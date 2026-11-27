import type {
  BootstrapPayload,
  CourseCategory,
  CourseLevel,
  PaymentStatus,
  SubmissionStatus,
} from '../types/platform.ts';

async function requestJson<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  });
  const payload = await res.json().catch(() => ({}));
  if (!res.ok) {
    throw new Error(payload?.error || `Ошибка сервера (${res.status})`);
  }
  return payload as T;
}

export const api = {
  bootstrap: () => requestJson<BootstrapPayload>('/api/bootstrap'),

  resetDemo: () =>
    requestJson<BootstrapPayload>('/api/reset', {
      method: 'POST',
    }),

  enroll: (payload: {
    studentId: string;
    courseId: string;
    payNow?: boolean;
    paymentMethod?: string;
  }) =>
    requestJson<{ pendingPayment: boolean; data: BootstrapPayload }>('/api/enrollments', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  toggleLesson: (enrollmentId: string, lessonId: string) =>
    requestJson<{ data: BootstrapPayload }>(
      `/api/enrollments/${encodeURIComponent(enrollmentId)}/lessons/${encodeURIComponent(lessonId)}/toggle`,
      { method: 'POST' },
    ),

  submitQuiz: (enrollmentId: string, quizId: string, answers: number[]) =>
    requestJson<{ data: BootstrapPayload }>(
      `/api/enrollments/${encodeURIComponent(enrollmentId)}/quizzes/${encodeURIComponent(quizId)}/submit`,
      {
        method: 'POST',
        body: JSON.stringify({ answers }),
      },
    ),

  submitHomework: (payload: {
    courseId: string;
    studentId: string;
    answerText: string;
    projectUrl?: string;
  }) =>
    requestJson<{ data: BootstrapPayload }>('/api/submissions', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  reviewHomework: (
    submissionId: string,
    payload: {
      status: SubmissionStatus;
      score?: number;
      instructorComment: string;
      reviewerId?: string;
    },
  ) =>
    requestJson<{ data: BootstrapPayload }>(`/api/submissions/${encodeURIComponent(submissionId)}/review`, {
      method: 'PATCH',
      body: JSON.stringify(payload),
    }),

  createCourse: (payload: {
    title: string;
    subtitle: string;
    category: CourseCategory;
    level: CourseLevel;
    durationHours: number;
    priceRub: number;
    instructorId: string;
    firstModuleTitle?: string;
    firstLessonTitle?: string;
    firstLessonSummary?: string;
    firstLessonContent?: string;
    homeworkTitle?: string;
    homeworkDescription?: string;
  }) =>
    requestJson<{ data: BootstrapPayload }>('/api/courses', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  addModule: (courseId: string, payload: { title: string; description?: string }) =>
    requestJson<{ data: BootstrapPayload }>(`/api/courses/${encodeURIComponent(courseId)}/modules`, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  addLesson: (
    courseId: string,
    moduleId: string,
    payload: {
      title: string;
      durationMinutes: number;
      summary: string;
      content: string;
      keyTakeaway?: string;
      codeExample?: string;
    },
  ) =>
    requestJson<{ data: BootstrapPayload }>(
      `/api/courses/${encodeURIComponent(courseId)}/modules/${encodeURIComponent(moduleId)}/lessons`,
      {
        method: 'POST',
        body: JSON.stringify(payload),
      },
    ),

  updateLesson: (
    courseId: string,
    lessonId: string,
    payload: {
      title: string;
      durationMinutes: number;
      summary: string;
      content: string;
      keyTakeaway: string;
    },
  ) =>
    requestJson<{ data: BootstrapPayload }>(
      `/api/courses/${encodeURIComponent(courseId)}/lessons/${encodeURIComponent(lessonId)}`,
      {
        method: 'PUT',
        body: JSON.stringify(payload),
      },
    ),

  addQuiz: (
    courseId: string,
    payload: {
      title: string;
      moduleId?: string;
      passingScore: number;
      pointsReward: number;
      questions: Array<{
        prompt: string;
        options: string[];
        correctIndex: number;
        explanation: string;
      }>;
    },
  ) =>
    requestJson<{ data: BootstrapPayload }>(`/api/courses/${encodeURIComponent(courseId)}/quizzes`, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  createWebinar: (payload: {
    title: string;
    courseId: string;
    instructorId: string;
    date: string;
    startTime: string;
    durationMinutes: number;
    meetingUrl?: string;
    description?: string;
  }) =>
    requestJson<{ data: BootstrapPayload }>('/api/webinars', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  deleteWebinar: (webinarId: string) =>
    requestJson<{ data: BootstrapPayload }>(`/api/webinars/${encodeURIComponent(webinarId)}`, {
      method: 'DELETE',
    }),

  createGroup: (payload: { name: string; curatorId: string; scheduleNote?: string }) =>
    requestJson<{ data: BootstrapPayload }>('/api/groups', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),

  assignStudentGroup: (studentId: string, groupId: string) =>
    requestJson<{ data: BootstrapPayload }>(`/api/users/${encodeURIComponent(studentId)}/group`, {
      method: 'PATCH',
      body: JSON.stringify({ groupId }),
    }),

  updatePaymentStatus: (paymentId: string, status: PaymentStatus) =>
    requestJson<{ data: BootstrapPayload }>(`/api/payments/${encodeURIComponent(paymentId)}`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
};
