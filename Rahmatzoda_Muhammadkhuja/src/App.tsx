import { useCallback, useEffect, useMemo, useState } from 'react';
import { api } from './api/client.ts';
import { CertificateModal } from './components/CertificateModal.tsx';
import { CourseCatalog } from './components/CourseCatalog.tsx';
import { CoursePlayer } from './components/CoursePlayer.tsx';
import { Header, type InstructorTab, type StudentTab } from './components/Header.tsx';
import { InstructorCourses } from './components/InstructorCourses.tsx';
import { InstructorDashboard } from './components/InstructorDashboard.tsx';
import { InstructorHomework } from './components/InstructorHomework.tsx';
import { InstructorPayments } from './components/InstructorPayments.tsx';
import { InstructorSchedule } from './components/InstructorSchedule.tsx';
import { InstructorStudents } from './components/InstructorStudents.tsx';
import { MyCourses } from './components/MyCourses.tsx';
import { WebinarSchedule } from './components/WebinarSchedule.tsx';
import type { BootstrapPayload, Course, Enrollment, UserRole } from './types/platform.ts';

export default function App() {
  const [data, setData] = useState<BootstrapPayload | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [role, setRole] = useState<UserRole>('student');
  const [studentTab, setStudentTab] = useState<StudentTab>('catalog');
  const [instructorTab, setInstructorTab] = useState<InstructorTab>('overview');
  const [activeStudentId, setActiveStudentId] = useState<string>('stud-1');
  const [selectedCourseId, setSelectedCourseId] = useState<string | null>('crs-fullstack');
  const [openCreateCourse, setOpenCreateCourse] = useState(false);

  const [certificateTarget, setCertificateTarget] = useState<{
    enrollment: Enrollment;
    course: Course;
  } | null>(null);

  const [toast, setToast] = useState<string | null>(null);
  const [resetting, setResetting] = useState(false);

  const showToast = useCallback((msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 3500);
  }, []);

  const fetchInitialData = useCallback(async () => {
    setLoading(true);
    setLoadError(null);
    try {
      const payload = await api.bootstrap();
      setData(payload);
    } catch (err) {
      setLoadError(err instanceof Error ? err.message : 'Ошибка загрузки платформы');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchInitialData();
  }, [fetchInitialData]);

  const students = useMemo(
    () => (data ? data.users.filter((u) => u.role === 'student') : []),
    [data],
  );
  const instructors = useMemo(
    () => (data ? data.users.filter((u) => u.role === 'instructor') : []),
    [data],
  );

  const activeStudent = useMemo(
    () => students.find((s) => s.id === activeStudentId) ?? students[0] ?? null,
    [students, activeStudentId],
  );

  const handleResetDemo = async () => {
    setResetting(true);
    try {
      const payload = await api.resetDemo();
      setData(payload);
      setActiveStudentId('stud-1');
      setSelectedCourseId('crs-fullstack');
      setCertificateTarget(null);
      showToast('Демонстрационные данные восстановлены.');
    } finally {
      setResetting(false);
    }
  };

  const handleEnroll = async (courseId: string, payNow: boolean) => {
    if (!activeStudent) return;
    const res = await api.enroll({
      studentId: activeStudent.id,
      courseId,
      payNow,
    });
    setData(res.data);
    if (res.pendingPayment) {
      showToast('Заявка на оплату создана и передана преподавателю.');
    } else {
      setSelectedCourseId(courseId);
      setStudentTab('player');
      showToast('Вы успешно записались на курс!');
    }
  };

  const handleOpenCourse = (courseId: string) => {
    setSelectedCourseId(courseId);
    setStudentTab('player');
  };

  const handlePrimaryAction = () => {
    if (role === 'student') {
      const firstEnrollment = data?.enrollments.find((e) => e.studentId === activeStudent?.id);
      if (firstEnrollment) {
        setSelectedCourseId(firstEnrollment.courseId);
        setStudentTab('player');
      } else {
        setStudentTab('catalog');
      }
    } else {
      setOpenCreateCourse(true);
      setInstructorTab('courses');
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-[#0B0F17] text-slate-100 flex items-center justify-center p-6">
        <div className="w-full max-w-md rounded-2xl bg-[#131A26] border border-white/[0.08] p-8 text-center space-y-3">
          <div className="font-display text-lg font-bold text-amber-400">Академия Вектор</div>
          <p className="text-sm text-slate-400">Загрузка учебных программ и данных...</p>
        </div>
      </div>
    );
  }

  if (loadError || !data || !activeStudent) {
    return (
      <div className="min-h-screen bg-[#0B0F17] text-slate-100 flex items-center justify-center p-6">
        <div className="w-full max-w-md rounded-2xl bg-[#131A26] border border-rose-500/30 p-8 text-center space-y-4">
          <div className="font-display text-lg font-bold text-slate-50">
            Не удалось подключиться к серверу
          </div>
          <p className="text-sm text-slate-400">{loadError}</p>
          <button
            type="button"
            onClick={fetchInitialData}
            className="px-5 py-2.5 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 cursor-pointer"
          >
            Повторить попытку
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#0B0F17] text-[#F8FAFC] flex flex-col">
      <Header
        role={role}
        onRoleChange={(nextRole) => {
          setRole(nextRole);
          setOpenCreateCourse(false);
        }}
        studentTab={studentTab}
        onStudentTabChange={setStudentTab}
        instructorTab={instructorTab}
        onInstructorTabChange={(tab) => {
          setOpenCreateCourse(false);
          setInstructorTab(tab);
        }}
        students={students}
        activeStudentId={activeStudent.id}
        onSelectStudent={(id) => {
          setActiveStudentId(id);
          setCertificateTarget(null);
        }}
        onPrimaryAction={handlePrimaryAction}
        onResetDemo={handleResetDemo}
        resetting={resetting}
        pendingHomeworkCount={data.summary.pendingHomework}
      />

      <main className="flex-1 w-full max-w-[1320px] mx-auto px-6 py-8 md:py-10">
        {role === 'student' && (
          <>
            {studentTab === 'catalog' && (
              <CourseCatalog
                courses={data.courses}
                instructors={instructors}
                enrollments={data.enrollments}
                payments={data.payments}
                activeStudentId={activeStudent.id}
                onEnroll={handleEnroll}
                onOpenCourse={handleOpenCourse}
              />
            )}

            {studentTab === 'my-courses' && (
              <MyCourses
                courses={data.courses}
                enrollments={data.enrollments}
                submissions={data.submissions}
                instructors={instructors}
                activeStudent={activeStudent}
                onOpenCourse={handleOpenCourse}
                onOpenCertificate={(enrollment, course) =>
                  setCertificateTarget({ enrollment, course })
                }
                onGoToCatalog={() => setStudentTab('catalog')}
              />
            )}

            {studentTab === 'player' && (
              <CoursePlayer
                courses={data.courses}
                enrollments={data.enrollments}
                submissions={data.submissions}
                instructors={instructors}
                activeStudentId={activeStudent.id}
                selectedCourseId={selectedCourseId}
                onSelectCourse={setSelectedCourseId}
                onBackToMyCourses={() => setStudentTab('my-courses')}
                onToggleLesson={async (enrollmentId, lessonId) => {
                  const res = await api.toggleLesson(enrollmentId, lessonId);
                  setData(res.data);
                  showToast('Прогресс по уроку обновлён.');
                }}
                onSubmitQuiz={async (enrollmentId, quizId, answers) => {
                  const res = await api.submitQuiz(enrollmentId, quizId, answers);
                  setData(res.data);
                  showToast('Тест проверен, результаты сохранены.');
                }}
                onSubmitHomework={async (payload) => {
                  const res = await api.submitHomework(payload);
                  setData(res.data);
                  showToast('Домашнее задание отправлено на проверку.');
                }}
                onOpenCertificate={(enrollment, course) =>
                  setCertificateTarget({ enrollment, course })
                }
              />
            )}

            {studentTab === 'webinars' && (
              <WebinarSchedule
                webinars={data.webinars}
                courses={data.courses}
                instructors={instructors}
              />
            )}
          </>
        )}

        {role === 'instructor' && (
          <>
            {instructorTab === 'overview' && (
              <InstructorDashboard
                summary={data.summary}
                trend={data.enrollmentTrend}
                submissions={data.submissions}
                courses={data.courses}
                students={students}
                webinars={data.webinars}
                onNavigate={(tab) => setInstructorTab(tab)}
              />
            )}

            {instructorTab === 'courses' && (
              <InstructorCourses
                courses={data.courses}
                instructors={instructors}
                openCreateByDefault={openCreateCourse}
                onCreateCourse={async (payload) => {
                  const res = await api.createCourse(payload);
                  setData(res.data);
                }}
                onAddModule={async (courseId, payload) => {
                  const res = await api.addModule(courseId, payload);
                  setData(res.data);
                }}
                onAddLesson={async (courseId, moduleId, payload) => {
                  const res = await api.addLesson(courseId, moduleId, payload);
                  setData(res.data);
                }}
                onUpdateLesson={async (courseId, lessonId, payload) => {
                  const res = await api.updateLesson(courseId, lessonId, payload);
                  setData(res.data);
                }}
                onAddQuiz={async (courseId, payload) => {
                  const res = await api.addQuiz(courseId, payload);
                  setData(res.data);
                }}
              />
            )}

            {instructorTab === 'homework' && (
              <InstructorHomework
                submissions={data.submissions}
                courses={data.courses}
                students={students}
                onReview={async (submissionId, payload) => {
                  const res = await api.reviewHomework(submissionId, {
                    ...payload,
                    reviewerId: instructors[0]?.id,
                  });
                  setData(res.data);
                }}
              />
            )}

            {instructorTab === 'students' && (
              <InstructorStudents
                students={students}
                instructors={instructors}
                groups={data.groups}
                courses={data.courses}
                enrollments={data.enrollments}
                submissions={data.submissions}
                onCreateGroup={async (payload) => {
                  const res = await api.createGroup(payload);
                  setData(res.data);
                  showToast('Учебная группа создана.');
                }}
                onAssignGroup={async (studentId, groupId) => {
                  const res = await api.assignStudentGroup(studentId, groupId);
                  setData(res.data);
                  showToast('Группа студента обновлена.');
                }}
              />
            )}

            {instructorTab === 'schedule' && (
              <InstructorSchedule
                webinars={data.webinars}
                courses={data.courses}
                instructors={instructors}
                onCreateWebinar={async (payload) => {
                  const res = await api.createWebinar(payload);
                  setData(res.data);
                }}
                onDeleteWebinar={async (id) => {
                  const res = await api.deleteWebinar(id);
                  setData(res.data);
                  showToast('Вебинар удалён из расписания.');
                }}
              />
            )}

            {instructorTab === 'payments' && (
              <InstructorPayments
                payments={data.payments}
                students={students}
                courses={data.courses}
                onUpdateStatus={async (paymentId, status) => {
                  const res = await api.updatePaymentStatus(paymentId, status);
                  setData(res.data);
                  showToast(
                    status === 'paid'
                      ? 'Оплата подтверждена, доступ к курсу открыт.'
                      : 'Статус заявки обновлён.',
                  );
                }}
              />
            )}
          </>
        )}
      </main>

      <footer className="border-t border-white/[0.06] py-6 px-6 text-xs text-slate-500">
        <div className="max-w-[1320px] mx-auto flex flex-wrap items-center justify-between gap-4">
          <span>© 2026 Академия Вектор — Платформа онлайн-обучения</span>
          <span>Локальное хранилище данных: JSON-база сервера</span>
        </div>
      </footer>

      {certificateTarget && (
        <CertificateModal
          enrollment={certificateTarget.enrollment}
          course={certificateTarget.course}
          student={activeStudent}
          instructor={instructors.find((i) => i.id === certificateTarget.course.instructorId)}
          onClose={() => setCertificateTarget(null)}
        />
      )}

      {toast && (
        <div
          role="status"
          className="fixed bottom-6 right-6 z-50 px-4 py-3 rounded-xl bg-[#1A2332] border border-amber-500/40 text-xs font-medium text-slate-100 shadow-xl"
        >
          {toast}
        </div>
      )}
    </div>
  );
}
