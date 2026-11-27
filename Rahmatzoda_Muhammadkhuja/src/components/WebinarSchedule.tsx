import { Calendar, Clock, Video, ExternalLink } from 'lucide-react';
import type { Course, User, Webinar } from '../types/platform.ts';
import { formatRuDate } from '../utils/formatters.ts';
import { MediaImage } from './MediaImage.tsx';

interface WebinarScheduleProps {
  webinars: Webinar[];
  courses: Course[];
  instructors: User[];
}

export function WebinarSchedule({ webinars, courses, instructors }: WebinarScheduleProps) {
  const courseById = new Map(courses.map((c) => [c.id, c]));
  const instructorById = new Map(instructors.map((i) => [i.id, i]));

  const sortedWebinars = [...webinars].sort((a, b) =>
    `${a.date} ${a.startTime}`.localeCompare(`${b.date} ${b.startTime}`),
  );

  return (
    <div className="space-y-8">
      <div className="space-y-2 border-b border-white/[0.08] pb-6">
        <p className="text-xs font-medium text-amber-400">Живые онлайн-занятия</p>
        <h1 className="font-display text-2xl md:text-3xl font-bold text-slate-50">
          Расписание ближайших вебинаров и консультаций
        </h1>
        <p className="text-sm text-slate-400 max-w-2xl">
          Разборы практических заданий, ответы на вопросы с преподавателями и воркшопы в прямом эфире.
        </p>
      </div>

      {sortedWebinars.length === 0 ? (
        <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-12 text-center text-sm text-slate-400">
          Ближайших запланированных вебинаров пока нет.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {sortedWebinars.map((webinar) => {
            const course = courseById.get(webinar.courseId);
            const instructor = instructorById.get(webinar.instructorId);

            return (
              <article
                key={webinar.id}
                className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-6 flex flex-col justify-between gap-5"
              >
                <div className="space-y-3">
                  <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-slate-400">
                    <span className="font-medium text-amber-400">
                      {course?.title ?? 'Открытое занятие'}
                    </span>
                    <span className="inline-flex items-center gap-1 font-mono tabular-nums text-slate-300">
                      <Calendar className="w-3.5 h-3.5 text-slate-500" />
                      {formatRuDate(webinar.date)}
                    </span>
                  </div>

                  <h2 className="font-display text-lg font-semibold text-slate-50 leading-snug">
                    {webinar.title}
                  </h2>

                  <p className="text-sm text-slate-400 leading-relaxed">{webinar.description}</p>
                </div>

                <div className="pt-4 border-t border-white/[0.06] flex flex-wrap items-center justify-between gap-4">
                  <div className="flex items-center gap-3">
                    <MediaImage
                      src={instructor?.avatar}
                      alt={instructor?.name ?? 'Спикер'}
                      kind="avatar"
                      fallbackName={instructor?.name}
                      className="w-9 h-9 rounded-full object-cover shrink-0"
                    />
                    <div>
                      <div className="text-xs font-medium text-slate-200">
                        {instructor?.name ?? 'Преподаватель'}
                      </div>
                      <div className="flex items-center gap-2 text-xs text-slate-400 font-mono tabular-nums">
                        <Clock className="w-3 h-3 text-amber-400" />
                        <span>
                          {webinar.startTime} МСК · {webinar.durationMinutes} мин
                        </span>
                      </div>
                    </div>
                  </div>

                  <a
                    href={webinar.meetingUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors whitespace-nowrap shrink-0"
                  >
                    <Video className="w-3.5 h-3.5" />
                    <span>Подключиться</span>
                    <ExternalLink className="w-3 h-3" />
                  </a>
                </div>
              </article>
            );
          })}
        </div>
      )}
    </div>
  );
}
