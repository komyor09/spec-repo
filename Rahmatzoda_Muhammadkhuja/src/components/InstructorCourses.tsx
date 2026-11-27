import { useState } from 'react';
import { Plus, BookOpen, HelpCircle, Edit3, Check, AlertTriangle } from 'lucide-react';
import type {
  Course,
  CourseCategory,
  CourseLevel,
  Lesson,
  User,
} from '../types/platform.ts';
import { formatRub, pluralize } from '../utils/formatters.ts';

interface InstructorCoursesProps {
  courses: Course[];
  instructors: User[];
  openCreateByDefault?: boolean;
  onCreateCourse: (payload: {
    title: string;
    subtitle: string;
    category: CourseCategory;
    level: CourseLevel;
    durationHours: number;
    priceRub: number;
    instructorId: string;
    firstModuleTitle?: string;
    firstLessonTitle?: string;
    firstLessonContent?: string;
  }) => Promise<void>;
  onAddModule: (courseId: string, payload: { title: string; description?: string }) => Promise<void>;
  onAddLesson: (
    courseId: string,
    moduleId: string,
    payload: {
      title: string;
      durationMinutes: number;
      summary: string;
      content: string;
      keyTakeaway?: string;
    },
  ) => Promise<void>;
  onUpdateLesson: (
    courseId: string,
    lessonId: string,
    payload: {
      title: string;
      durationMinutes: number;
      summary: string;
      content: string;
      keyTakeaway: string;
    },
  ) => Promise<void>;
  onAddQuiz: (
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
  ) => Promise<void>;
}

const CATEGORIES: CourseCategory[] = [
  'IT и разработка',
  'Дизайн',
  'Маркетинг',
  'Аналитика',
  'Менеджмент',
];
const LEVELS: CourseLevel[] = ['Начальный', 'Средний', 'Продвинутый'];

export function InstructorCourses({
  courses,
  instructors,
  openCreateByDefault = false,
  onCreateCourse,
  onAddModule,
  onAddLesson,
  onUpdateLesson,
  onAddQuiz,
}: InstructorCoursesProps) {
  const [selectedCourseId, setSelectedCourseId] = useState<string>(courses[0]?.id ?? '');
  const [showCreateCourse, setShowCreateCourse] = useState(openCreateByDefault);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // Create Course Form State
  const [title, setTitle] = useState('');
  const [subtitle, setSubtitle] = useState('');
  const [category, setCategory] = useState<CourseCategory>('IT и разработка');
  const [level, setLevel] = useState<CourseLevel>('Начальный');
  const [durationHours, setDurationHours] = useState(24);
  const [priceRub, setPriceRub] = useState(19900);
  const [instructorId, setInstructorId] = useState(instructors[0]?.id ?? 'inst-1');
  const [firstLessonTitle, setFirstLessonTitle] = useState('');

  // Add Module State
  const [newModuleTitle, setNewModuleTitle] = useState('');

  // Add Lesson State
  const [targetModuleId, setTargetModuleId] = useState('');
  const [lessonTitle, setLessonTitle] = useState('');
  const [lessonDuration, setLessonDuration] = useState(30);
  const [lessonSummary, setLessonSummary] = useState('');
  const [lessonContent, setLessonContent] = useState('');

  // Edit Lesson State
  const [editingLesson, setEditingLesson] = useState<Lesson | null>(null);
  const [editTitle, setEditTitle] = useState('');
  const [editDuration, setEditDuration] = useState(30);
  const [editSummary, setEditSummary] = useState('');
  const [editContent, setEditContent] = useState('');
  const [editTakeaway, setEditTakeaway] = useState('');

  // Add Quiz State
  const [quizTitle, setQuizTitle] = useState('');
  const [quizPrompt, setQuizPrompt] = useState('');
  const [quizOptA, setQuizOptA] = useState('');
  const [quizOptB, setQuizOptB] = useState('');
  const [quizOptC, setQuizOptC] = useState('');
  const [quizCorrectIdx, setQuizCorrectIdx] = useState(0);

  const selectedCourse = courses.find((c) => c.id === selectedCourseId) ?? courses[0] ?? null;

  const showSuccess = (msg: string) => {
    setSuccessMsg(msg);
    setErrorMsg(null);
    setTimeout(() => setSuccessMsg(null), 3500);
  };

  const handleCreateCourseSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);
    setBusy(true);
    try {
      await onCreateCourse({
        title,
        subtitle,
        category,
        level,
        durationHours,
        priceRub,
        instructorId,
        firstLessonTitle: firstLessonTitle || undefined,
      });
      setTitle('');
      setSubtitle('');
      setFirstLessonTitle('');
      setShowCreateCourse(false);
      showSuccess('Новый курс создан и добавлен в каталог.');
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : 'Ошибка создания курса');
    } finally {
      setBusy(false);
    }
  };

  const handleAddModuleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCourse) return;
    setErrorMsg(null);
    setBusy(true);
    try {
      await onAddModule(selectedCourse.id, { title: newModuleTitle });
      setNewModuleTitle('');
      showSuccess('Модуль добавлен в программу.');
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : 'Ошибка добавления модуля');
    } finally {
      setBusy(false);
    }
  };

  const handleAddLessonSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCourse) return;
    const modId = targetModuleId || selectedCourse.modules[0]?.id;
    if (!modId) {
      setErrorMsg('Сначала создайте хотя бы один модуль.');
      return;
    }
    setErrorMsg(null);
    setBusy(true);
    try {
      await onAddLesson(selectedCourse.id, modId, {
        title: lessonTitle,
        durationMinutes: lessonDuration,
        summary: lessonSummary,
        content: lessonContent,
      });
      setLessonTitle('');
      setLessonSummary('');
      setLessonContent('');
      showSuccess('Урок добавлен в модуль.');
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : 'Ошибка добавления урока');
    } finally {
      setBusy(false);
    }
  };

  const startEditingLesson = (lesson: Lesson) => {
    setEditingLesson(lesson);
    setEditTitle(lesson.title);
    setEditDuration(lesson.durationMinutes);
    setEditSummary(lesson.summary);
    setEditContent(lesson.content.join('\n'));
    setEditTakeaway(lesson.keyTakeaway);
  };

  const handleSaveLessonEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCourse || !editingLesson) return;
    setErrorMsg(null);
    setBusy(true);
    try {
      await onUpdateLesson(selectedCourse.id, editingLesson.id, {
        title: editTitle,
        durationMinutes: editDuration,
        summary: editSummary,
        content: editContent,
        keyTakeaway: editTakeaway,
      });
      setEditingLesson(null);
      showSuccess('Материалы урока обновлены.');
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : 'Ошибка сохранения урока');
    } finally {
      setBusy(false);
    }
  };

  const handleAddQuizSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCourse) return;
    setErrorMsg(null);
    const options = [quizOptA, quizOptB, quizOptC].map((o) => o.trim()).filter(Boolean);
    if (options.length < 2) {
      setErrorMsg('Укажите минимум 2 варианта ответа для вопроса теста.');
      return;
    }
    setBusy(true);
    try {
      await onAddQuiz(selectedCourse.id, {
        title: quizTitle,
        moduleId: selectedCourse.modules[0]?.id,
        passingScore: 70,
        pointsReward: 45,
        questions: [
          {
            prompt: quizPrompt,
            options,
            correctIndex: Math.min(quizCorrectIdx, options.length - 1),
            explanation: 'Ответ подтверждён преподавателем курса.',
          },
        ],
      });
      setQuizTitle('');
      setQuizPrompt('');
      setQuizOptA('');
      setQuizOptB('');
      setQuizOptC('');
      showSuccess('Интерактивный тест добавлен в курс.');
    } catch (err) {
      setErrorMsg(err instanceof Error ? err.message : 'Ошибка создания теста');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-8">
      <div className="flex flex-wrap items-end justify-between gap-4 border-b border-white/[0.08] pb-6">
        <div className="space-y-1.5">
          <p className="text-xs font-medium text-amber-400">Конструктор образовательных программ</p>
          <h1 className="font-display text-2xl md:text-3xl font-bold text-slate-50">
            Управление курсами, уроками и тестами
          </h1>
        </div>

        <button
          type="button"
          onClick={() => setShowCreateCourse((prev) => !prev)}
          className="inline-flex items-center gap-1.5 px-4 py-2.5 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer"
        >
          <Plus className="w-4 h-4" />
          <span>{showCreateCourse ? 'Скрыть форму' : 'Создать новый курс'}</span>
        </button>
      </div>

      {errorMsg && (
        <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/30 text-sm text-rose-200 flex items-center gap-2">
          <AlertTriangle className="w-4 h-4 text-rose-400 shrink-0" />
          <span>{errorMsg}</span>
        </div>
      )}

      {successMsg && (
        <div className="p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-sm text-emerald-200 flex items-center gap-2">
          <Check className="w-4 h-4 text-emerald-400 shrink-0" />
          <span>{successMsg}</span>
        </div>
      )}

      {/* New Course Creation Form */}
      {showCreateCourse && (
        <form
          onSubmit={handleCreateCourseSubmit}
          className="rounded-2xl bg-[#131A26] border border-amber-500/30 p-6 space-y-5"
        >
          <h2 className="font-display text-lg font-semibold text-slate-50">
            Новый учебный курс
          </h2>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <label className="block text-xs text-slate-300">Название курса</label>
              <input
                required
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Например: Системный анализ и проектирование БД"
                className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 focus:outline-none focus:border-amber-500"
              />
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs text-slate-300">Преподаватель</label>
              <select
                value={instructorId}
                onChange={(e) => setInstructorId(e.target.value)}
                className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 focus:outline-none focus:border-amber-500"
              >
                {instructors.map((inst) => (
                  <option key={inst.id} value={inst.id}>
                    {inst.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="md:col-span-2 space-y-1.5">
              <label className="block text-xs text-slate-300">Краткое описание программы</label>
              <input
                required
                value={subtitle}
                onChange={(e) => setSubtitle(e.target.value)}
                placeholder="Чему научатся студенты и какие практические навыки освоят..."
                className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 focus:outline-none focus:border-amber-500"
              />
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs text-slate-300">Категория</label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value as CourseCategory)}
                className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 focus:outline-none focus:border-amber-500"
              >
                {CATEGORIES.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs text-slate-300">Уровень сложности</label>
              <select
                value={level}
                onChange={(e) => setLevel(e.target.value as CourseLevel)}
                className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 focus:outline-none focus:border-amber-500"
              >
                {LEVELS.map((l) => (
                  <option key={l} value={l}>
                    {l}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs text-slate-300">Длительность (часов)</label>
              <input
                type="number"
                min={1}
                max={500}
                value={durationHours}
                onChange={(e) => setDurationHours(Number(e.target.value))}
                className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 font-mono tabular-nums focus:outline-none focus:border-amber-500"
              />
            </div>

            <div className="space-y-1.5">
              <label className="block text-xs text-slate-300">Стоимость (₽, 0 = бесплатно)</label>
              <input
                type="number"
                min={0}
                step={100}
                value={priceRub}
                onChange={(e) => setPriceRub(Number(e.target.value))}
                className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 font-mono tabular-nums focus:outline-none focus:border-amber-500"
              />
            </div>

            <div className="md:col-span-2 space-y-1.5">
              <label className="block text-xs text-slate-300">
                Название первого вводного урока (необязательно)
              </label>
              <input
                value={firstLessonTitle}
                onChange={(e) => setFirstLessonTitle(e.target.value)}
                placeholder="Урок 1. Введение и архитектура проекта"
                className="w-full px-3.5 py-2 text-sm bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 focus:outline-none focus:border-amber-500"
              />
            </div>
          </div>

          <div className="flex items-center gap-3">
            <button
              type="submit"
              disabled={busy}
              className="px-5 py-2.5 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer disabled:opacity-50"
            >
              {busy ? 'Сохранение...' : 'Опубликовать курс'}
            </button>
            <button
              type="button"
              onClick={() => setShowCreateCourse(false)}
              className="px-4 py-2.5 text-xs text-slate-400 hover:text-slate-200 cursor-pointer"
            >
              Отмена
            </button>
          </div>
        </form>
      )}

      {/* Course Selector + Content Editor */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Course List */}
        <aside className="lg:col-span-4 rounded-2xl bg-[#131A26] border border-white/[0.08] p-4 space-y-2">
          <div className="px-2 py-1 text-xs font-semibold uppercase tracking-wider text-slate-400">
            Программы ({courses.length})
          </div>
          {courses.map((c) => {
            const active = selectedCourse?.id === c.id;
            const lessonsCount = c.modules.reduce((acc, m) => acc + m.lessons.length, 0);
            return (
              <button
                key={c.id}
                type="button"
                onClick={() => {
                  setSelectedCourseId(c.id);
                  setEditingLesson(null);
                }}
                className={`w-full text-left p-3.5 rounded-xl border transition-colors cursor-pointer space-y-1 ${
                  active
                    ? 'bg-amber-500/15 border-amber-500/40'
                    : 'bg-[#0B0F17] border-white/[0.06] hover:border-white/20'
                }`}
              >
                <div className="text-xs text-slate-400">
                  {c.category} · {formatRub(c.priceRub)}
                </div>
                <div className="text-sm font-semibold text-slate-100 leading-snug">{c.title}</div>
                <div className="text-[11px] text-slate-400 font-mono tabular-nums">
                  {pluralize(lessonsCount, 'урок', 'урока', 'уроков')} · {c.quizzes.length} тест(а)
                </div>
              </button>
            );
          })}
        </aside>

        {/* Selected Course Structure & Editors */}
        {selectedCourse && (
          <div className="lg:col-span-8 space-y-6">
            <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-6 space-y-5">
              <div className="border-b border-white/[0.06] pb-4 space-y-1">
                <div className="text-xs text-amber-400">
                  {selectedCourse.category} · {selectedCourse.level} · {selectedCourse.durationHours} ч
                </div>
                <h2 className="font-display text-xl font-bold text-slate-50">
                  {selectedCourse.title}
                </h2>
                <p className="text-sm text-slate-400">{selectedCourse.subtitle}</p>
              </div>

              {/* Modules & Lessons List */}
              <div className="space-y-4">
                {selectedCourse.modules.map((mod) => (
                  <div
                    key={mod.id}
                    className="p-4 rounded-xl bg-[#0B0F17] border border-white/[0.06] space-y-3"
                  >
                    <div className="text-sm font-semibold text-slate-100">{mod.title}</div>
                    {mod.lessons.length === 0 ? (
                      <p className="text-xs text-slate-500">В модуле пока нет уроков.</p>
                    ) : (
                      <div className="divide-y divide-white/[0.06]">
                        {mod.lessons.map((les) => (
                          <div
                            key={les.id}
                            className="py-2.5 flex items-center justify-between gap-3"
                          >
                            <div className="flex items-center gap-2.5 min-w-0">
                              <BookOpen className="w-4 h-4 text-amber-400 shrink-0" />
                              <div className="min-w-0">
                                <div className="text-xs font-medium text-slate-200 truncate">
                                  {les.title}
                                </div>
                                <div className="text-[11px] text-slate-400 font-mono tabular-nums">
                                  {les.durationMinutes} мин
                                </div>
                              </div>
                            </div>
                            <button
                              type="button"
                              onClick={() => startEditingLesson(les)}
                              className="inline-flex items-center gap-1 px-2.5 py-1 text-xs text-slate-300 hover:text-amber-400 border border-white/10 rounded-md transition-colors cursor-pointer shrink-0"
                            >
                              <Edit3 className="w-3 h-3" />
                              <span>Изменить</span>
                            </button>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>

              {/* Lesson Edit Form */}
              {editingLesson && (
                <form
                  onSubmit={handleSaveLessonEdit}
                  className="p-5 rounded-xl bg-[#1A2332] border border-amber-500/30 space-y-4"
                >
                  <div className="text-sm font-semibold text-amber-300">
                    Редактирование урока: {editingLesson.title}
                  </div>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                    <div className="md:col-span-2">
                      <input
                        required
                        value={editTitle}
                        onChange={(e) => setEditTitle(e.target.value)}
                        placeholder="Название урока"
                        className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                      />
                    </div>
                    <div>
                      <input
                        type="number"
                        min={1}
                        max={300}
                        value={editDuration}
                        onChange={(e) => setEditDuration(Number(e.target.value))}
                        className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 font-mono tabular-nums"
                      />
                    </div>
                  </div>
                  <input
                    value={editSummary}
                    onChange={(e) => setEditSummary(e.target.value)}
                    placeholder="Краткое резюме"
                    className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                  />
                  <textarea
                    rows={3}
                    value={editContent}
                    onChange={(e) => setEditContent(e.target.value)}
                    placeholder="Текст урока (каждый абзац с новой строки)"
                    className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                  />
                  <input
                    value={editTakeaway}
                    onChange={(e) => setEditTakeaway(e.target.value)}
                    placeholder="Ключевой вывод урока"
                    className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                  />
                  <div className="flex items-center gap-3">
                    <button
                      type="submit"
                      disabled={busy}
                      className="px-4 py-2 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 cursor-pointer"
                    >
                      Сохранить изменения
                    </button>
                    <button
                      type="button"
                      onClick={() => setEditingLesson(null)}
                      className="px-3 py-2 text-xs text-slate-300 hover:text-white cursor-pointer"
                    >
                      Отмена
                    </button>
                  </div>
                </form>
              )}

              {/* Quizzes List */}
              <div className="pt-2 space-y-2">
                <div className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                  Тесты курса ({selectedCourse.quizzes.length})
                </div>
                {selectedCourse.quizzes.map((q) => (
                  <div
                    key={q.id}
                    className="px-4 py-3 rounded-xl bg-[#0B0F17] border border-white/[0.06] flex items-center justify-between gap-3"
                  >
                    <div className="flex items-center gap-2 text-xs font-medium text-slate-200">
                      <HelpCircle className="w-4 h-4 text-amber-400" />
                      <span>{q.title}</span>
                    </div>
                    <span className="text-xs font-mono text-slate-400 tabular-nums">
                      {q.questions.length} вопр. · +{q.pointsReward} б.
                    </span>
                  </div>
                ))}
              </div>
            </div>

            {/* Add Module, Lesson, Quiz Panels */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Add Lesson */}
              <form
                onSubmit={handleAddLessonSubmit}
                className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-5 space-y-3"
              >
                <h3 className="font-display text-sm font-semibold text-slate-50">
                  Добавить урок в курс
                </h3>
                <select
                  value={targetModuleId || selectedCourse.modules[0]?.id || ''}
                  onChange={(e) => setTargetModuleId(e.target.value)}
                  className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                >
                  {selectedCourse.modules.map((m) => (
                    <option key={m.id} value={m.id}>
                      {m.title}
                    </option>
                  ))}
                </select>
                <input
                  required
                  value={lessonTitle}
                  onChange={(e) => setLessonTitle(e.target.value)}
                  placeholder="Название урока"
                  className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                />
                <div className="grid grid-cols-2 gap-2">
                  <input
                    type="number"
                    min={5}
                    max={300}
                    value={lessonDuration}
                    onChange={(e) => setLessonDuration(Number(e.target.value))}
                    placeholder="Мин."
                    className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100 font-mono tabular-nums"
                  />
                  <input
                    value={lessonSummary}
                    onChange={(e) => setLessonSummary(e.target.value)}
                    placeholder="Краткое описание"
                    className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                  />
                </div>
                <textarea
                  rows={3}
                  required
                  value={lessonContent}
                  onChange={(e) => setLessonContent(e.target.value)}
                  placeholder="Основной текст урока..."
                  className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                />
                <button
                  type="submit"
                  disabled={busy}
                  className="w-full py-2 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer"
                >
                  Добавить урок
                </button>
              </form>

              {/* Add Module + Add Quiz */}
              <div className="space-y-6">
                <form
                  onSubmit={handleAddModuleSubmit}
                  className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-5 space-y-3"
                >
                  <h3 className="font-display text-sm font-semibold text-slate-50">
                    Новый модуль
                  </h3>
                  <input
                    required
                    value={newModuleTitle}
                    onChange={(e) => setNewModuleTitle(e.target.value)}
                    placeholder="Например: Модуль 3. Оптимизация и тестирование"
                    className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                  />
                  <button
                    type="submit"
                    disabled={busy}
                    className="w-full py-2 text-xs font-semibold rounded-lg bg-[#1A2332] hover:bg-[#232F42] text-slate-100 border border-white/10 transition-colors cursor-pointer"
                  >
                    Добавить модуль
                  </button>
                </form>

                <form
                  onSubmit={handleAddQuizSubmit}
                  className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-5 space-y-3"
                >
                  <h3 className="font-display text-sm font-semibold text-slate-50">
                    Добавить тест по теме
                  </h3>
                  <input
                    required
                    value={quizTitle}
                    onChange={(e) => setQuizTitle(e.target.value)}
                    placeholder="Название теста"
                    className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                  />
                  <input
                    required
                    value={quizPrompt}
                    onChange={(e) => setQuizPrompt(e.target.value)}
                    placeholder="Текст вопроса"
                    className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                  />
                  <div className="space-y-1.5">
                    <input
                      required
                      value={quizOptA}
                      onChange={(e) => setQuizOptA(e.target.value)}
                      placeholder="Вариант 1"
                      className="w-full px-3 py-1.5 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                    />
                    <input
                      required
                      value={quizOptB}
                      onChange={(e) => setQuizOptB(e.target.value)}
                      placeholder="Вариант 2"
                      className="w-full px-3 py-1.5 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                    />
                    <input
                      value={quizOptC}
                      onChange={(e) => setQuizOptC(e.target.value)}
                      placeholder="Вариант 3 (необязательно)"
                      className="w-full px-3 py-1.5 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                    />
                  </div>
                  <select
                    value={quizCorrectIdx}
                    onChange={(e) => setQuizCorrectIdx(Number(e.target.value))}
                    className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
                  >
                    <option value={0}>Правильный ответ: Вариант 1</option>
                    <option value={1}>Правильный ответ: Вариант 2</option>
                    <option value={2}>Правильный ответ: Вариант 3</option>
                  </select>
                  <button
                    type="submit"
                    disabled={busy}
                    className="w-full py-2 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer"
                  >
                    Создать тест
                  </button>
                </form>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
