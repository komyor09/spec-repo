import { Users, BookOpen, ClipboardList, Wallet, ArrowRight, Calendar } from 'lucide-react';
import type {
  Course,
  DashboardSummary,
  EnrollmentTrendPoint,
  HomeworkSubmission,
  User,
  Webinar,
} from '../types/platform.ts';
import type { InstructorTab } from './Header.tsx';
import { formatRub, formatShortDate } from '../utils/formatters.ts';

interface InstructorDashboardProps {
  summary: DashboardSummary;
  trend: EnrollmentTrendPoint[];
  submissions: HomeworkSubmission[];
  courses: Course[];
  students: User[];
  webinars: Webinar[];
  onNavigate: (tab: InstructorTab) => void;
}

export function InstructorDashboard({
  summary,
  trend,
  submissions,
  courses,
  students,
  webinars,
  onNavigate,
}: InstructorDashboardProps) {
  const courseById = new Map(courses.map((c) => [c.id, c]));
  const studentById = new Map(students.map((s) => [s.id, s]));

  const pendingQueue = submissions.filter((s) => s.status === 'pending').slice(0, 4);
  const nextWebinars = [...webinars]
    .sort((a, b) => `${a.date} ${a.startTime}`.localeCompare(`${b.date} ${b.startTime}`))
    .slice(0, 3);

  const maxTrendValue = Math.max(
    1,
    ...trend.map((t) => Math.max(t.enrollments, t.completions)),
  );

  return (
    <div className="space-y-8">
      {/* Header */}
      <div className="space-y-1.5 border-b border-white/[0.08] pb-6">
        <p className="text-xs font-medium text-amber-400">Аналитика и управление академией</p>
        <h1 className="font-display text-2xl md:text-3xl font-bold text-slate-50">
          Кабинет преподавателя и администратора
        </h1>
        <p className="text-sm text-slate-400">
          Сводка по учебным группам, проверке заданий, динамике записей и поступлениям.
        </p>
      </div>

      {/* 4 KPI Metrics */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-5 space-y-2">
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>Активные студенты</span>
            <Users className="w-4 h-4 text-amber-400" />
          </div>
          <div className="font-mono text-3xl font-bold text-slate-50 tabular-nums">
            {summary.activeStudents}
          </div>
          <div className="text-xs text-slate-400 font-mono tabular-nums">
            Средний прогресс: {summary.averageCompletionPercent}%
          </div>
        </div>

        <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-5 space-y-2">
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>Учебных программ</span>
            <BookOpen className="w-4 h-4 text-amber-400" />
          </div>
          <div className="font-mono text-3xl font-bold text-slate-50 tabular-nums">
            {summary.totalCourses}
          </div>
          <div className="text-xs text-slate-400">5 направлений обучения</div>
        </div>

        <button
          type="button"
          onClick={() => onNavigate('homework')}
          className="text-left rounded-2xl bg-[#131A26] border border-white/[0.08] hover:border-amber-500/40 p-5 space-y-2 transition-colors cursor-pointer"
        >
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>ДЗ на проверке</span>
            <ClipboardList className="w-4 h-4 text-amber-400" />
          </div>
          <div className="font-mono text-3xl font-bold text-amber-400 tabular-nums">
            {summary.pendingHomework}
          </div>
          <div className="text-xs text-slate-300 inline-flex items-center gap-1">
            <span>Перейти к проверке</span>
            <ArrowRight className="w-3 h-3" />
          </div>
        </button>

        <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-5 space-y-2">
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>Подтверждённые оплаты</span>
            <Wallet className="w-4 h-4 text-emerald-400" />
          </div>
          <div className="font-mono text-2xl font-bold text-emerald-400 tabular-nums">
            {formatRub(summary.confirmedRevenueRub)}
          </div>
          <div className="text-xs text-slate-400">Учёт подтверждённых заявок</div>
        </div>
      </div>

      {/* Dynamics Chart + Pending Queue */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-stretch">
        {/* Enrollment Dynamics */}
        <section className="lg:col-span-7 rounded-2xl bg-[#131A26] border border-white/[0.08] p-6 flex flex-col justify-between gap-6">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="font-display text-lg font-semibold text-slate-50">
                Динамика записей и выпусков
              </h2>
              <p className="text-xs text-slate-400">
                Количество новых записей и завершённых курсов за последние 6 месяцев
              </p>
            </div>
            <div className="flex items-center gap-4 text-xs text-slate-300">
              <span className="inline-flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-sm bg-amber-500" />
                Записи
              </span>
              <span className="inline-flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-sm bg-emerald-500" />
                Выпуски
              </span>
            </div>
          </div>

          <div className="grid grid-cols-6 gap-4 items-end h-52 pt-4">
            {trend.map((point) => {
              const enrollH = Math.max(10, Math.round((point.enrollments / maxTrendValue) * 100));
              const compH = Math.max(8, Math.round((point.completions / maxTrendValue) * 100));
              return (
                <div key={point.month} className="flex flex-col items-center gap-2 h-full justify-end">
                  <div className="flex items-end gap-1.5 w-full justify-center h-40">
                    <div
                      title={`Записи: ${point.enrollments}`}
                      className="w-4 sm:w-5 rounded-t bg-amber-500/85 transition-all"
                      style={{ height: `${enrollH}%` }}
                    />
                    <div
                      title={`Завершения: ${point.completions}`}
                      className="w-4 sm:w-5 rounded-t bg-emerald-500/80 transition-all"
                      style={{ height: `${compH}%` }}
                    />
                  </div>
                  <div className="text-xs text-slate-400 font-mono tabular-nums">
                    {point.month}
                  </div>
                  <div className="text-[11px] text-slate-300 font-mono tabular-nums">
                    {point.enrollments}/{point.completions}
                  </div>
                </div>
              );
            })}
          </div>
        </section>

        {/* Pending Homework Queue */}
        <section className="lg:col-span-5 rounded-2xl bg-[#131A26] border border-white/[0.08] p-6 flex flex-col justify-between gap-4">
          <div className="flex items-center justify-between gap-2">
            <h2 className="font-display text-lg font-semibold text-slate-50">
              Очередь проверки ДЗ
            </h2>
            <button
              type="button"
              onClick={() => onNavigate('homework')}
              className="text-xs font-medium text-amber-400 hover:text-amber-300 cursor-pointer"
            >
              Все работы
            </button>
          </div>

          {pendingQueue.length === 0 ? (
            <div className="flex-1 flex items-center justify-center text-sm text-slate-400 py-8">
              Все присланные задания уже проверены.
            </div>
          ) : (
            <div className="divide-y divide-white/[0.06]">
              {pendingQueue.map((sub) => {
                const student = studentById.get(sub.studentId);
                const course = courseById.get(sub.courseId);
                return (
                  <div key={sub.id} className="py-3.5 first:pt-0 last:pb-0 space-y-1">
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-sm font-medium text-slate-100 truncate">
                        {student?.name ?? 'Студент'}
                      </span>
                      <span className="text-xs font-mono text-slate-400 tabular-nums shrink-0">
                        {formatShortDate(sub.submittedAt)}
                      </span>
                    </div>
                    <div className="text-xs text-amber-400 truncate">
                      {course?.title ?? 'Курс'}
                    </div>
                    <p className="text-xs text-slate-400 line-clamp-1">{sub.answerText}</p>
                  </div>
                );
              })}
            </div>
          )}

          <button
            type="button"
            onClick={() => onNavigate('homework')}
            className="w-full py-2.5 text-xs font-semibold rounded-lg bg-[#1A2332] hover:bg-[#232F42] text-slate-100 border border-white/10 transition-colors cursor-pointer"
          >
            Открыть журнал проверки
          </button>
        </section>
      </div>

      {/* Upcoming Schedule Strip */}
      <section className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-6 space-y-4">
        <div className="flex items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Calendar className="w-4 h-4 text-amber-400" />
            <h2 className="font-display text-lg font-semibold text-slate-50">
              Ближайшие занятия в расписании
            </h2>
          </div>
          <button
            type="button"
            onClick={() => onNavigate('schedule')}
            className="text-xs font-medium text-amber-400 hover:text-amber-300 cursor-pointer"
          >
            Управлять расписанием
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {nextWebinars.map((web) => {
            const course = courseById.get(web.courseId);
            return (
              <div
                key={web.id}
                className="p-4 rounded-xl bg-[#0B0F17] border border-white/[0.06] space-y-1.5"
              >
                <div className="font-mono text-xs text-amber-400 tabular-nums">
                  {web.date} · {web.startTime} ({web.durationMinutes} мин)
                </div>
                <div className="text-sm font-semibold text-slate-100 leading-snug">
                  {web.title}
                </div>
                <div className="text-xs text-slate-400 truncate">
                  {course?.title ?? 'Курс'}
                </div>
              </div>
            );
          })}
        </div>
      </section>
    </div>
  );
}
