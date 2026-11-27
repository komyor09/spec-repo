import { useMemo, useState } from 'react';
import { Calendar, Clock, Trash2, AlertTriangle, CheckCircle2, Plus } from 'lucide-react';
import type { Course, User, Webinar } from '../types/platform.ts';
import { formatRuDate } from '../utils/formatters.ts';
import { MediaImage } from './MediaImage.tsx';

interface InstructorScheduleProps {
  webinars: Webinar[];
  courses: Course[];
  instructors: User[];
  onCreateWebinar: (payload: {
    title: string;
    courseId: string;
    instructorId: string;
    date: string;
    startTime: string;
    durationMinutes: number;
    meetingUrl?: string;
    description?: string;
  }) => Promise<void>;
  onDeleteWebinar: (id: string) => Promise<void>;
}

export function InstructorSchedule({
  webinars,
  courses,
  instructors,
  onCreateWebinar,
  onDeleteWebinar,
}: InstructorScheduleProps) {
  const [title, setTitle] = useState('');
  const [courseId, setCourseId] = useState(courses[0]?.id ?? '');
  const [instructorId, setInstructorId] = useState(instructors[0]?.id ?? 'inst-1');
  const [date, setDate] = useState('2026-12-03');
  const [startTime, setStartTime] = useState('19:30');
  const [durationMinutes, setDurationMinutes] = useState(60);
  const [meetingUrl, setMeetingUrl] = useState('https://meet.vector-academy.ru/session');
  const [description, setDescription] = useState('');
  const [conflictError, setConflictError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const courseById = useMemo(() => new Map(courses.map((c) => [c.id, c])), [courses]);
  const instructorById = useMemo(
    () => new Map(instructors.map((i) => [i.id, i])),
    [instructors],
  );

  const sortedWebinars = useMemo(
    () =>
      [...webinars].sort((a, b) =>
        `${a.date} ${a.startTime}`.localeCompare(`${b.date} ${b.startTime}`),
      ),
    [webinars],
  );

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setConflictError(null);
    setSuccessMsg(null);
    setBusy(true);
    try {
      await onCreateWebinar({
        title,
        courseId,
        instructorId,
        date,
        startTime,
        durationMinutes,
        meetingUrl,
        description,
      });
      setTitle('');
      setDescription('');
      setSuccessMsg('Занятие успешно добавлено в расписание.');
      setTimeout(() => setSuccessMsg(null), 3500);
    } catch (err) {
      setConflictError(
        err instanceof Error ? err.message : 'Не удалось запланировать занятие.',
      );
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-8">
      <div className="space-y-1.5 border-b border-white/[0.08] pb-6">
        <p className="text-xs font-medium text-amber-400">
          Контроль расписания без накладок
        </p>
        <h1 className="font-display text-2xl md:text-3xl font-bold text-slate-50">
          Вебинары и живые консультации
        </h1>
        <p className="text-sm text-slate-400">
          Сервер автоматически проверяет пересечение времени для выбранного преподавателя.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Create Webinar Form */}
        <form
          onSubmit={handleSubmit}
          className="lg:col-span-5 rounded-2xl bg-[#131A26] border border-white/[0.08] p-6 space-y-4"
        >
          <div className="flex items-center gap-2 font-display text-lg font-semibold text-slate-50">
            <Plus className="w-4 h-4 text-amber-400" />
            <span>Назначить занятие</span>
          </div>

          <div className="space-y-1.5">
            <label className="block text-xs text-slate-300">Тема вебинара или консультации</label>
            <input
              required
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Например: Разбор кейсов модуля 2"
              className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 focus:outline-none focus:border-amber-500"
            />
          </div>

          <div className="space-y-1.5">
            <label className="block text-xs text-slate-300">Преподаватель</label>
            <select
              value={instructorId}
              onChange={(e) => {
                setInstructorId(e.target.value);
                setConflictError(null);
              }}
              className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 focus:outline-none focus:border-amber-500"
            >
              {instructors.map((inst) => (
                <option key={inst.id} value={inst.id}>
                  {inst.name}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <label className="block text-xs text-slate-300">Курс</label>
            <select
              value={courseId}
              onChange={(e) => setCourseId(e.target.value)}
              className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 focus:outline-none focus:border-amber-500"
            >
              {courses.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.title}
                </option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div className="space-y-1">
              <label className="block text-[11px] text-slate-400">Дата</label>
              <input
                type="date"
                required
                value={date}
                onChange={(e) => {
                  setDate(e.target.value);
                  setConflictError(null);
                }}
                className="w-full px-2.5 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 font-mono tabular-nums"
              />
            </div>

            <div className="space-y-1">
              <label className="block text-[11px] text-slate-400">Начало (МСК)</label>
              <input
                type="time"
                required
                value={startTime}
                onChange={(e) => {
                  setStartTime(e.target.value);
                  setConflictError(null);
                }}
                className="w-full px-2.5 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 font-mono tabular-nums"
              />
            </div>

            <div className="space-y-1">
              <label className="block text-[11px] text-slate-400">Мин.</label>
              <input
                type="number"
                min={15}
                max={360}
                step={15}
                value={durationMinutes}
                onChange={(e) => {
                  setDurationMinutes(Number(e.target.value));
                  setConflictError(null);
                }}
                className="w-full px-2.5 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 font-mono tabular-nums"
              />
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="block text-xs text-slate-300">Ссылка на трансляцию</label>
            <input
              type="url"
              value={meetingUrl}
              onChange={(e) => setMeetingUrl(e.target.value)}
              className="w-full px-3.5 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
            />
          </div>

          <div className="space-y-1.5">
            <label className="block text-xs text-slate-300">Краткая программа занятия</label>
            <textarea
              rows={2}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Какие темы разберём на встрече..."
              className="w-full p-3 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
            />
          </div>

          {conflictError && (
            <div className="p-3.5 rounded-xl bg-rose-500/15 border border-rose-500/40 text-xs text-rose-200 flex items-start gap-2">
              <AlertTriangle className="w-4 h-4 text-rose-400 shrink-0 mt-0.5" />
              <span>{conflictError}</span>
            </div>
          )}

          {successMsg && (
            <div className="p-3.5 rounded-xl bg-emerald-500/15 border border-emerald-500/30 text-xs text-emerald-200 flex items-center gap-2">
              <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
              <span>{successMsg}</span>
            </div>
          )}

          <button
            type="submit"
            disabled={busy}
            className="w-full py-2.5 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer disabled:opacity-50"
          >
            {busy ? 'Проверка и сохранение...' : 'Запланировать занятие'}
          </button>
        </form>

        {/* Scheduled Webinars List */}
        <div className="lg:col-span-7 space-y-4">
          {sortedWebinars.map((web) => {
            const course = courseById.get(web.courseId);
            const instructor = instructorById.get(web.instructorId);

            return (
              <div
                key={web.id}
                className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4"
              >
                <div className="flex items-start gap-3.5 min-w-0">
                  <MediaImage
                    src={instructor?.avatar}
                    alt={instructor?.name ?? 'Спикер'}
                    kind="avatar"
                    fallbackName={instructor?.name}
                    className="w-10 h-10 rounded-full object-cover shrink-0 mt-0.5"
                  />
                  <div className="space-y-1 min-w-0">
                    <div className="flex flex-wrap items-center gap-2 text-xs text-amber-400 font-mono tabular-nums">
                      <span className="inline-flex items-center gap-1">
                        <Calendar className="w-3.5 h-3.5" />
                        {formatRuDate(web.date)}
                      </span>
                      <span aria-hidden="true">·</span>
                      <span className="inline-flex items-center gap-1">
                        <Clock className="w-3.5 h-3.5" />
                        {web.startTime} ({web.durationMinutes} мин)
                      </span>
                    </div>
                    <h2 className="font-display text-base font-semibold text-slate-50 leading-snug">
                      {web.title}
                    </h2>
                    <div className="text-xs text-slate-400">
                      {instructor?.name ?? 'Преподаватель'} · {course?.title ?? 'Курс'}
                    </div>
                  </div>
                </div>

                <button
                  type="button"
                  onClick={() => onDeleteWebinar(web.id)}
                  aria-label={`Удалить занятие ${web.title}`}
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs text-rose-300 hover:text-rose-200 border border-rose-500/20 hover:border-rose-500/40 rounded-lg transition-colors cursor-pointer shrink-0 self-start sm:self-center"
                >
                  <Trash2 className="w-3.5 h-3.5" />
                  <span>Отменить</span>
                </button>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
