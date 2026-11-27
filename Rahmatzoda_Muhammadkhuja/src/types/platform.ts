export type CourseCategory =
  | 'IT и разработка'
  | 'Дизайн'
  | 'Маркетинг'
  | 'Аналитика'
  | 'Менеджмент';

export type CourseLevel = 'Начальный' | 'Средний' | 'Продвинутый';

export type UserRole = 'student' | 'instructor';

export type SubmissionStatus = 'pending' | 'approved' | 'revision';

export type PaymentStatus = 'paid' | 'pending' | 'cancelled';

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  avatar: string;
  specialty?: string;
  groupId?: string;
}

export interface Group {
  id: string;
  name: string;
  curatorId: string;
  startDate: string;
  scheduleNote: string;
}

export interface QuizQuestion {
  id: string;
  prompt: string;
  options: string[];
  correctIndex: number;
  explanation: string;
}

export interface Quiz {
  id: string;
  title: string;
  moduleId: string;
  passingScore: number;
  pointsReward: number;
  questions: QuizQuestion[];
}

export interface Lesson {
  id: string;
  title: string;
  durationMinutes: number;
  summary: string;
  content: string[];
  codeExample?: string;
  keyTakeaway: string;
}

export interface CourseModule {
  id: string;
  title: string;
  description: string;
  lessons: Lesson[];
}

export interface HomeworkTask {
  title: string;
  description: string;
  deliverableHint: string;
}

export interface Course {
  id: string;
  title: string;
  subtitle: string;
  category: CourseCategory;
  level: CourseLevel;
  durationHours: number;
  rating: number;
  studentsCount: number;
  priceRub: number;
  coverImage: string;
  instructorId: string;
  modules: CourseModule[];
  quizzes: Quiz[];
  homeworkTask: HomeworkTask;
  createdAt: string;
}

export interface QuizAttempt {
  quizId: string;
  scorePercent: number;
  passed: boolean;
  earnedPoints: number;
  selectedAnswers: number[];
  completedAt: string;
}

export interface Enrollment {
  id: string;
  studentId: string;
  courseId: string;
  enrolledAt: string;
  completedLessonIds: string[];
  quizAttempts: Record<string, QuizAttempt>;
  progressPercent: number;
  totalPoints: number;
  certificateId?: string;
  completedAt?: string;
}

export interface HomeworkSubmission {
  id: string;
  courseId: string;
  studentId: string;
  answerText: string;
  projectUrl?: string;
  status: SubmissionStatus;
  score?: number;
  instructorComment?: string;
  submittedAt: string;
  reviewedAt?: string;
  reviewerId?: string;
}

export interface Webinar {
  id: string;
  title: string;
  courseId: string;
  instructorId: string;
  date: string; // YYYY-MM-DD
  startTime: string; // HH:MM
  durationMinutes: number;
  meetingUrl: string;
  description: string;
}

export interface PaymentRequest {
  id: string;
  studentId: string;
  courseId: string;
  amountRub: number;
  status: PaymentStatus;
  paymentMethod: string;
  createdAt: string;
  updatedAt: string;
}

export interface EnrollmentTrendPoint {
  month: string;
  enrollments: number;
  completions: number;
}

export interface DatabaseSchema {
  users: User[];
  groups: Group[];
  courses: Course[];
  enrollments: Enrollment[];
  submissions: HomeworkSubmission[];
  webinars: Webinar[];
  payments: PaymentRequest[];
  enrollmentTrend: EnrollmentTrendPoint[];
}

export interface DashboardSummary {
  activeStudents: number;
  totalCourses: number;
  pendingHomework: number;
  confirmedRevenueRub: number;
  averageCompletionPercent: number;
  upcomingWebinarsCount: number;
}

export interface BootstrapPayload extends DatabaseSchema {
  summary: DashboardSummary;
}
