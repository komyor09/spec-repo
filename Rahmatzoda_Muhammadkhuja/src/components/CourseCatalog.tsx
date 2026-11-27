import { useMemo, useState } from 'react';
import { Search, Star, Clock, ArrowRight, CheckCircle2, Hourglass } from 'lucide-react';
import type {
  Course,
  CourseCategory,
  CourseLevel,
  Enrollment,
  PaymentRequest,
  User,
} from '../types/platform.ts';
import { formatRub, pluralize } from '../utils/formatters.ts';
import { MediaImage } from './MediaImage.tsx';

interface CourseCatalogProps {
  courses: Course[];
  instructors: User[];
  enrollments: Enrollment[];
  payments: PaymentRequest[];
  activeStudentId: string;
  onEnroll: (courseId: string, payNow: boolean) => Promise<void>;
  onOpenCourse: (courseId: string) => void;
}

const CATEGORIES: Array<'Все' | CourseCategory> = [
  'Все',
  'IT и разработка',
  'Дизайн',
  'Маркетинг',
  'Аналитика',
  'Менеджмент',
];

const LEVELS: Array<'Все' | CourseLevel> = ['Все', 'Начальный', 'Средний', 'Продвинутый'];

export function CourseCatalog({
  courses,
  instructors,
  enrollments,
  payments,
  activeStudentId,
  onEnroll,
  onOpenCourse,
}: CourseCatalogProps) {
  const [search, setSearch] = useState('');
  const [categoryFilter, setCategoryFilter] = useState<'Все' | CourseCategory>('Все');
  const [levelFilter, setLevelFilter] = useState<'Все' | CourseLevel>('Все');
  const [busyCourseId, setBusyCourseId] = useState<string | null>(null);
  const [checkoutCourse, setCheckoutCourse] = useState<Course | null>(null);

  const instructorById = useMemo(
    () => new Map(instructors.map((inst) => [inst.id, inst])),
    [instructors],
  );

  const studentEnrollmentsByCourse = useMemo(() => {
    const map = new Map<string, Enrollment>();
    for (const enr of enrollments) {
      if (enr.studentId === activeStudentId) {
        map.set(enr.courseId, enr);
      }
    }
    return map;
  }, [enrollments, activeStudentId]);

  const pendingPaymentsByCourse = useMemo(() => {
    const set = new Set<string>();
    for (const p of payments) {
      if (p.studentId === activeStudentId && p.status === 'pending') {
        set.add(p.courseId);
      }
    }
    return set;
  }, [payments, activeStudentId]);

  const filteredCourses = useMemo(() => {
    const q = search.trim().toLowerCase();
    return courses.filter((course) => {
      if (categoryFilter !== 'Все' && course.category !== categoryFilter) return false;
      if (levelFilter !== 'Все' && course.level !== levelFilter) return false;
      if (!q) return true;
      return (
        course.title.toLowerCase().includes(q) ||
        course.subtitle.toLowerCase().includes(q) ||
        course.category.toLowerCase().includes(q)
      );
    });
  }, [courses, search, categoryFilter, levelFilter]);

  const handleEnrollAction = async (course: Course, payNow: boolean) => {
    setBusyCourseId(course.id);
    try {
      await onEnroll(course.id, payNow);
      setCheckoutCourse(null);
    } finally {
      setBusyCourseId(null);
    }
  };

  return (
    <div className="space-y-10">
      {/* Editorial Hero Section */}
      <section className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-8 md:p-10 grid grid-cols-1 lg:grid-cols-12 gap-8 items-center">
        <div className="lg:col-span-8 space-y-4">
          <p className="text-xs font-medium tracking-wide text-amber-400">
            Многопрофильная онлайн-академия · Набор осень–зима 2026
          </p>
          <h1 className="font-display text-3xl md:text-4xl font-bold text-slate-50 leading-tight">
            Прикладные профессии в IT, дизайне, маркетинге и аналитике
          </h1>
          <p className="text-base text-slate-300 max-w-2xl leading-relaxed">
            Практические модули с проверкой домашних заданий преподавателями, интерактивными тестами,
            живыми вебинарами и именным сертификатом после завершения программы.
          </p>
        </div>

        <div className="lg:col-span-4 grid grid-cols-2 gap-4 pt-2 lg:pt-0">
          <div className="p-4 rounded-xl bg-[#0B0F17] border border-white/[0.06]">
            <div className="font-mono text-2xl font-semibold text-amber-400 tabular-nums">
              {courses.length}
            </div>
            <div className="text-xs text-slate-400 mt-1">Актуальных программ</div>
          </div>
          <div className="p-4 rounded-xl bg-[#0B0F17] border border-white/[0.06]">
            <div className="font-mono text-2xl font-semibold text-slate-50 tabular-nums">
              100%
            </div>
            <div className="text-xs text-slate-400 mt-1">Проверка ДЗ наставником</div>
          </div>
        </div>
      </section>

      {/* Search & Filters Toolbar */}
      <section className="space-y-4">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="relative flex-1 max-w-md">
            <Search className="w-4 h-4 text-slate-400 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
            <input
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Поиск по названию, теме или направлению..."
              className="w-full pl-10 pr-4 py-2.5 text-sm bg-[#131A26] border border-white/10 rounded-lg text-slate-100 placeholder:text-slate-500 focus:outline-none focus:border-amber-500"
            />
          </div>

          {/* Segmented Level Filter */}
          <div className="flex items-center gap-1 p-1 bg-[#131A26] border border-white/[0.08] rounded-lg overflow-x-auto">
            {LEVELS.map((lvl) => (
              <button
                key={lvl}
                type="button"
                onClick={() => setLevelFilter(lvl)}
                className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors whitespace-nowrap shrink-0 cursor-pointer ${
                  levelFilter === lvl
                    ? 'bg-amber-500 text-slate-950'
                    : 'text-slate-300 hover:text-white'
                }`}
              >
                {lvl === 'Все' ? 'Любой уровень' : lvl}
              </button>
            ))}
          </div>
        </div>

        {/* Category Filter Tabs */}
        <div className="flex items-center gap-2 overflow-x-auto pb-1">
          {CATEGORIES.map((cat) => (
            <button
              key={cat}
              type="button"
              onClick={() => setCategoryFilter(cat)}
              className={`px-3.5 py-2 text-xs font-medium rounded-lg border transition-colors whitespace-nowrap shrink-0 cursor-pointer ${
                categoryFilter === cat
                  ? 'bg-amber-500/15 border-amber-500/60 text-amber-300'
                  : 'bg-[#131A26] border-white/[0.08] text-slate-300 hover:border-white/20 hover:text-white'
              }`}
            >
              {cat}
            </button>
          ))}
        </div>
      </section>

      {/* Course Grid */}
      {filteredCourses.length === 0 ? (
        <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-12 text-center space-y-3">
          <p className="text-base font-medium text-slate-200">
            По заданным фильтрам курсы не найдены
          </p>
          <p className="text-sm text-slate-400">
            Попробуйте сбросить категорию или изменить поисковый запрос.
          </p>
          <button
            type="button"
            onClick={() => {
              setSearch('');
              setCategoryFilter('Все');
              setLevelFilter('Все');
            }}
            className="mt-2 px-4 py-2 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer"
          >
            Сбросить фильтры
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredCourses.map((course) => {
            const instructor = instructorById.get(course.instructorId);
            const enrollment = studentEnrollmentsByCourse.get(course.id);
            const isPendingPayment = pendingPaymentsByCourse.has(course.id);
            const lessonsCount = course.modules.reduce((acc, m) => acc + m.lessons.length, 0);

            return (
              <article
                key={course.id}
                className="group flex flex-col rounded-2xl bg-[#131A26] border border-white/[0.08] hover:border-amber-500/40 transition-colors overflow-hidden"
              >
                <div className="relative aspect-[4/3] overflow-hidden bg-[#0B0F17]">
                  <MediaImage
                    src={course.coverImage}
                    alt={course.title}
                    className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-[1.03]"
                  />
                  <div className="absolute inset-x-0 bottom-0 h-20 bg-gradient-to-t from-[#131A26] via-[#131A26]/60 to-transparent" />
                </div>

                <div className="p-6 flex-1 flex flex-col gap-4">
                  {/* Unboxed zero-pill metadata line */}
                  <div className="flex items-center justify-between gap-2 text-xs text-slate-400">
                    <span className="truncate">
                      {course.category} · {course.level}
                    </span>
                    <span className="inline-flex items-center gap-1 font-mono tabular-nums text-amber-400 shrink-0">
                      <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />
                      {course.rating.toFixed(1)}
                    </span>
                  </div>

                  <div className="space-y-2">
                    <h2 className="font-display text-lg font-semibold text-slate-50 leading-snug">
                      {course.title}
                    </h2>
                    <p className="text-sm text-slate-400 line-clamp-2 leading-relaxed">
                      {course.subtitle}
                    </p>
                  </div>

                  {/* Duration & Curriculum summary */}
                  <div className="flex items-center gap-3 text-xs text-slate-400 font-mono tabular-nums">
                    <span className="inline-flex items-center gap-1">
                      <Clock className="w-3.5 h-3.5 text-slate-500" />
                      {course.durationHours} ч
                    </span>
                    <span aria-hidden="true">·</span>
                    <span>{pluralize(lessonsCount, 'урок', 'урока', 'уроков')}</span>
                    <span aria-hidden="true">·</span>
                    <span>{course.studentsCount} студ.</span>
                  </div>

                  {/* Instructor line */}
                  <div className="flex items-center gap-2.5 pt-2 border-t border-white/[0.06]">
                    <MediaImage
                      src={instructor?.avatar}
                      alt={instructor?.name ?? 'Преподаватель'}
                      kind="avatar"
                      fallbackName={instructor?.name}
                      className="w-7 h-7 rounded-full object-cover shrink-0"
                    />
                    <span className="text-xs text-slate-300 truncate">
                      {instructor?.name ?? 'Преподаватель академии'}
                    </span>
                  </div>

                  {/* Footer CTA */}
                  <div className="mt-auto pt-2 flex items-center justify-between gap-3">
                    <div className="font-mono text-base font-semibold text-slate-50 tabular-nums">
                      {formatRub(course.priceRub)}
                    </div>

                    {enrollment ? (
                      <button
                        type="button"
                        onClick={() => onOpenCourse(course.id)}
                        className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-lg bg-emerald-500/15 text-emerald-300 border border-emerald-500/30 hover:bg-emerald-500/25 transition-colors whitespace-nowrap shrink-0 cursor-pointer"
                      >
                        <CheckCircle2 className="w-3.5 h-3.5" />
                        <span>Открыть ({enrollment.progressPercent}%)</span>
                      </button>
                    ) : isPendingPayment ? (
                      <span className="inline-flex items-center gap-1.5 px-3 py-2 text-xs font-medium text-amber-300">
                        <Hourglass className="w-3.5 h-3.5" />
                        Заявка на оплате
                      </span>
                    ) : (
                      <button
                        type="button"
                        disabled={busyCourseId === course.id}
                        onClick={() => {
                          if (course.priceRub === 0) {
                            handleEnrollAction(course, true);
                          } else {
                            setCheckoutCourse(course);
                          }
                        }}
                        className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors whitespace-nowrap shrink-0 cursor-pointer disabled:opacity-50"
                      >
                        <span>
                          {busyCourseId === course.id ? 'Запись...' : 'Записаться'}
                        </span>
                        <ArrowRight className="w-3.5 h-3.5" />
                      </button>
                    )}
                  </div>
                </div>
              </article>
            );
          })}
        </div>
      )}

      {/* Enrollment / Payment Option Modal for Paid Courses */}
      {checkoutCourse && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/75 backdrop-blur-sm p-4">
          <div className="w-full max-w-md rounded-2xl bg-[#131A26] border border-white/10 p-6 space-y-5">
            <div className="space-y-1">
              <p className="text-xs text-amber-400 font-medium">Запись на программу</p>
              <h3 className="font-display text-lg font-bold text-slate-50">
                {checkoutCourse.title}
              </h3>
              <p className="text-xs text-slate-400">
                {checkoutCourse.category} · {checkoutCourse.durationHours} часов обучения
              </p>
            </div>

            <div className="p-4 rounded-xl bg-[#0B0F17] border border-white/[0.06] flex items-center justify-between">
              <span className="text-sm text-slate-300">Стоимость обучения</span>
              <span className="font-mono text-lg font-bold text-amber-400 tabular-nums">
                {formatRub(checkoutCourse.priceRub)}
              </span>
            </div>

            <div className="space-y-2.5">
              <button
                type="button"
                disabled={busyCourseId === checkoutCourse.id}
                onClick={() => handleEnrollAction(checkoutCourse, true)}
                className="w-full py-2.5 px-4 text-sm font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer disabled:opacity-50"
              >
                Оплатить сейчас и начать обучение
              </button>
              <button
                type="button"
                disabled={busyCourseId === checkoutCourse.id}
                onClick={() => handleEnrollAction(checkoutCourse, false)}
                className="w-full py-2.5 px-4 text-sm font-medium rounded-lg bg-[#1A2332] text-slate-200 hover:bg-[#232F42] border border-white/10 transition-colors cursor-pointer disabled:opacity-50"
              >
                Оставить заявку на оплату (подтвердит преподаватель)
              </button>
              <button
                type="button"
                onClick={() => setCheckoutCourse(null)}
                className="w-full py-2 text-xs text-slate-400 hover:text-slate-200 transition-colors cursor-pointer"
              >
                Отмена
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
