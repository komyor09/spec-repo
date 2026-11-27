import { Award, ArrowRight, CheckCircle2, Clock, FileCheck2, AlertTriangle } from 'lucide-react';
import type { Course, Enrollment, HomeworkSubmission, User } from '../types/platform.ts';
import { SUBMISSION_STATUS_META, formatShortDate } from '../utils/formatters.ts';
import { MediaImage } from './MediaImage.tsx';

interface MyCoursesProps {
  courses: Course[];
  enrollments: Enrollment[];
  submissions: HomeworkSubmission[];
  instructors: User[];
  activeStudent: User;
  onOpenCourse: (courseId: string) => void;
  onOpenCertificate: (enrollment: Enrollment, course: Course) => void;
  onGoToCatalog: () => void;
}

export function MyCourses({
  courses,
  enrollments,
  submissions,
  instructors,
  activeStudent,
  onOpenCourse,
  onOpenCertificate,
  onGoToCatalog,
}: MyCoursesProps) {
  const courseById = new Map(courses.map((c) => [c.id, c]));
  const instructorById = new Map(instructors.map((i) => [i.id, i]));

  const myEnrollments = enrollments.filter((e) => e.studentId === activeStudent.id);
  const mySubmissions = submissions.filter((s) => s.studentId === activeStudent.id);

  const totalPoints = myEnrollments.reduce((sum, e) => sum + e.totalPoints, 0);
  const completedCount = myEnrollments.filter((e) => e.progressPercent === 100).length;

  return (
    <div className="space-y-10">
      {/* Student summary header */}
      <section className="flex flex-col md:flex-row md:items-end justify-between gap-6 border-b border-white/[0.08] pb-6">
        <div className="space-y-2">
          <p className="text-xs font-medium text-amber-400">Личный кабинет студента</p>
          <h1 className="font-display text-2xl md:text-3xl font-bold text-slate-50">
            Моё обучение — {activeStudent.name}
          </h1>
          <p className="text-sm text-slate-400">
            Отслеживайте прогресс по модулям, результаты тестов, статус домашних заданий и сертификаты.
          </p>
        </div>

        <div className="flex items-center gap-6 shrink-0">
          <div>
            <div className="font-mono text-2xl font-bold text-slate-50 tabular-nums">
              {myEnrollments.length}
            </div>
            <div className="text-xs text-slate-400">Активных курсов</div>
          </div>
          <div className="h-8 w-px bg-white/10" />
          <div>
            <div className="font-mono text-2xl font-bold text-amber-400 tabular-nums">
              {totalPoints}
            </div>
            <div className="text-xs text-slate-400">Набрано баллов</div>
          </div>
          <div className="h-8 w-px bg-white/10" />
          <div>
            <div className="font-mono text-2xl font-bold text-emerald-400 tabular-nums">
              {completedCount}
            </div>
            <div className="text-xs text-slate-400">Сертификатов</div>
          </div>
        </div>
      </section>

      {/* Active Enrolled Courses */}
      {myEnrollments.length === 0 ? (
        <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-12 text-center space-y-4">
          <p className="text-base font-medium text-slate-100">
            У вас пока нет активных записей на курсы
          </p>
          <p className="text-sm text-slate-400 max-w-md mx-auto">
            Выберите программу в каталоге, чтобы начать изучать уроки, проходить тесты и сдавать домашние задания.
          </p>
          <button
            type="button"
            onClick={onGoToCatalog}
            className="px-5 py-2.5 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer"
          >
            Перейти в каталог курсов
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {myEnrollments.map((enrollment) => {
            const course = courseById.get(enrollment.courseId);
            if (!course) return null;
            const instructor = instructorById.get(course.instructorId);
            const isCompleted = enrollment.progressPercent === 100;
            const totalLessons = course.modules.reduce((acc, m) => acc + m.lessons.length, 0);

            return (
              <div
                key={enrollment.id}
                className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-6 flex flex-col sm:flex-row gap-5"
              >
                <div className="w-full sm:w-40 aspect-[4/3] sm:aspect-auto sm:h-auto rounded-xl overflow-hidden shrink-0 bg-[#0B0F17]">
                  <MediaImage
                    src={course.coverImage}
                    alt={course.title}
                    className="w-full h-full object-cover"
                  />
                </div>

                <div className="flex-1 flex flex-col justify-between gap-4 min-w-0">
                  <div className="space-y-1.5">
                    <div className="flex items-center justify-between gap-2 text-xs text-slate-400">
                      <span className="truncate">
                        {course.category} · {instructor?.name ?? 'Преподаватель'}
                      </span>
                      {isCompleted && (
                        <span className="inline-flex items-center gap-1 text-emerald-400 font-medium shrink-0">
                          <CheckCircle2 className="w-3.5 h-3.5" />
                          Завершён
                        </span>
                      )}
                    </div>
                    <h2 className="font-display text-lg font-semibold text-slate-50 leading-snug">
                      {course.title}
                    </h2>
                    <p className="text-xs text-slate-400 font-mono tabular-nums">
                      Пройдено уроков: {enrollment.completedLessonIds.length} из {totalLessons} ·
                      Баллы: {enrollment.totalPoints}
                    </p>
                  </div>

                  {/* Progress bar */}
                  <div className="space-y-1.5">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-slate-400">Прогресс программы</span>
                      <span className="font-mono font-semibold text-amber-400 tabular-nums">
                        {enrollment.progressPercent}%
                      </span>
                    </div>
                    <div className="h-2 rounded-full bg-[#0B0F17] overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all duration-300 ${
                          isCompleted ? 'bg-emerald-500' : 'bg-amber-500'
                        }`}
                        style={{ width: `${enrollment.progressPercent}%` }}
                      />
                    </div>
                  </div>

                  <div className="flex flex-wrap items-center gap-3 pt-1">
                    <button
                      type="button"
                      onClick={() => onOpenCourse(course.id)}
                      className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer"
                    >
                      <span>{isCompleted ? 'Повторить материал' : 'Перейти к урокам'}</span>
                      <ArrowRight className="w-3.5 h-3.5" />
                    </button>

                    {isCompleted && (
                      <button
                        type="button"
                        onClick={() => onOpenCertificate(enrollment, course)}
                        className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-lg bg-emerald-500/15 text-emerald-300 border border-emerald-500/30 hover:bg-emerald-500/25 transition-colors cursor-pointer"
                      >
                        <Award className="w-3.5 h-3.5" />
                        <span>Открыть сертификат</span>
                      </button>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Homework Status Summary */}
      <section className="space-y-4">
        <h2 className="font-display text-xl font-semibold text-slate-50">
          Статусы домашних заданий
        </h2>

        {mySubmissions.length === 0 ? (
          <div className="rounded-xl bg-[#131A26] border border-white/[0.08] p-6 text-sm text-slate-400">
            Вы ещё не отправляли домашние задания. Откройте любой курс и перейдите на вкладку «Домашнее задание».
          </div>
        ) : (
          <div className="divide-y divide-white/[0.06] rounded-2xl bg-[#131A26] border border-white/[0.08]">
            {mySubmissions.map((sub) => {
              const course = courseById.get(sub.courseId);
              const meta = SUBMISSION_STATUS_META[sub.status];
              return (
                <div
                  key={sub.id}
                  className="p-5 flex flex-col md:flex-row md:items-center justify-between gap-4"
                >
                  <div className="space-y-1.5 min-w-0 flex-1">
                    <div className="flex items-center gap-3 text-xs text-slate-400">
                      <span className="font-medium text-slate-200 truncate">
                        {course?.title ?? 'Курс'}
                      </span>
                      <span aria-hidden="true">·</span>
                      <span className="font-mono tabular-nums">
                        {formatShortDate(sub.submittedAt)}
                      </span>
                    </div>
                    <p className="text-sm text-slate-300 line-clamp-2">{sub.answerText}</p>
                    {sub.instructorComment && (
                      <p className="text-xs text-slate-400 pt-1">
                        Комментарий преподавателя: <span className="text-slate-200">{sub.instructorComment}</span>
                      </p>
                    )}
                  </div>

                  <div className="flex items-center gap-4 shrink-0">
                    {sub.score !== undefined && (
                      <span className="font-mono text-sm font-semibold text-slate-100 tabular-nums">
                        {sub.score} / 100
                      </span>
                    )}
                    <span className={`inline-flex items-center gap-1.5 text-xs font-semibold ${meta.textClass}`}>
                      {sub.status === 'approved' && <FileCheck2 className="w-4 h-4" />}
                      {sub.status === 'pending' && <Clock className="w-4 h-4" />}
                      {sub.status === 'revision' && <AlertTriangle className="w-4 h-4" />}
                      {meta.label}
                    </span>
                    {course && (
                      <button
                        type="button"
                        onClick={() => onOpenCourse(course.id)}
                        className="px-3 py-1.5 text-xs font-medium text-slate-300 hover:text-amber-400 border border-white/10 rounded-md transition-colors cursor-pointer"
                      >
                        К заданию
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}
