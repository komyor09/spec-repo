import { useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  ArrowLeft,
  ArrowRight,
  Award,
  BookOpen,
  CheckCircle2,
  Circle,
  ClipboardCheck,
  Clock,
  ExternalLink,
  FileCheck2,
  HelpCircle,
  Send,
} from 'lucide-react';
import type {
  Course,
  Enrollment,
  HomeworkSubmission,
  User,
} from '../types/platform.ts';
import { SUBMISSION_STATUS_META, formatShortDate } from '../utils/formatters.ts';
import { MediaImage } from './MediaImage.tsx';

interface CoursePlayerProps {
  courses: Course[];
  enrollments: Enrollment[];
  submissions: HomeworkSubmission[];
  instructors: User[];
  activeStudentId: string;
  selectedCourseId: string | null;
  onSelectCourse: (courseId: string) => void;
  onBackToMyCourses: () => void;
  onToggleLesson: (enrollmentId: string, lessonId: string) => Promise<void>;
  onSubmitQuiz: (enrollmentId: string, quizId: string, answers: number[]) => Promise<void>;
  onSubmitHomework: (payload: {
    courseId: string;
    studentId: string;
    answerText: string;
    projectUrl?: string;
  }) => Promise<void>;
  onOpenCertificate: (enrollment: Enrollment, course: Course) => void;
}

type StageSelection =
  | { type: 'lesson'; lessonId: string }
  | { type: 'quiz'; quizId: string }
  | { type: 'homework' };

export function CoursePlayer({
  courses,
  enrollments,
  submissions,
  instructors,
  activeStudentId,
  selectedCourseId,
  onSelectCourse,
  onBackToMyCourses,
  onToggleLesson,
  onSubmitQuiz,
  onSubmitHomework,
  onOpenCertificate,
}: CoursePlayerProps) {
  const myEnrollments = useMemo(
    () => enrollments.filter((e) => e.studentId === activeStudentId),
    [enrollments, activeStudentId],
  );

  const activeEnrollment = useMemo(() => {
    if (selectedCourseId) {
      const found = myEnrollments.find((e) => e.courseId === selectedCourseId);
      if (found) return found;
    }
    return myEnrollments[0] ?? null;
  }, [myEnrollments, selectedCourseId]);

  const course = useMemo(
    () => courses.find((c) => c.id === activeEnrollment?.courseId) ?? null,
    [courses, activeEnrollment],
  );

  const instructor = useMemo(
    () => instructors.find((i) => i.id === course?.instructorId),
    [instructors, course],
  );

  const allLessons = useMemo(
    () => (course ? course.modules.flatMap((m) => m.lessons) : []),
    [course],
  );

  const [selection, setSelection] = useState<StageSelection>(() => {
    if (allLessons[0]) return { type: 'lesson', lessonId: allLessons[0].id };
    return { type: 'homework' };
  });

  const [quizAnswers, setQuizAnswers] = useState<Record<string, number[]>>({});
  const [hwText, setHwText] = useState('');
  const [hwUrl, setHwUrl] = useState('');
  const [hwError, setHwError] = useState<string | null>(null);
  const [busyAction, setBusyAction] = useState(false);

  // Sync stage selection when switching courses
  useEffect(() => {
    if (!course) return;
    const firstUnfinished = allLessons.find(
      (l) => !activeEnrollment?.completedLessonIds.includes(l.id),
    );
    const targetLesson = firstUnfinished ?? allLessons[0];
    if (targetLesson) {
      setSelection({ type: 'lesson', lessonId: targetLesson.id });
    } else if (course.quizzes[0]) {
      setSelection({ type: 'quiz', quizId: course.quizzes[0].id });
    } else {
      setSelection({ type: 'homework' });
    }
  }, [course?.id]);

  if (!activeEnrollment || !course) {
    return (
      <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-12 text-center space-y-4">
        <p className="text-base font-medium text-slate-100">
          Вы ещё не записаны ни на один курс
        </p>
        <p className="text-sm text-slate-400">
          Перейдите в каталог и выберите программу обучения, чтобы открыть плеер уроков.
        </p>
        <button
          type="button"
          onClick={onBackToMyCourses}
          className="px-5 py-2.5 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer"
        >
          К разделу «Мои курсы»
        </button>
      </div>
    );
  }

  const courseSubmissions = submissions.filter(
    (s) => s.courseId === course.id && s.studentId === activeStudentId,
  );

  const activeLesson =
    selection.type === 'lesson'
      ? allLessons.find((l) => l.id === selection.lessonId) ?? allLessons[0]
      : null;

  const activeLessonIndex = activeLesson
    ? allLessons.findIndex((l) => l.id === activeLesson.id)
    : -1;
  const nextLesson =
    activeLessonIndex >= 0 && activeLessonIndex + 1 < allLessons.length
      ? allLessons[activeLessonIndex + 1]
      : null;

  const activeQuiz =
    selection.type === 'quiz'
      ? course.quizzes.find((q) => q.id === selection.quizId) ?? course.quizzes[0]
      : null;

  const activeQuizAttempt = activeQuiz ? activeEnrollment.quizAttempts[activeQuiz.id] : undefined;
  const currentSelectedAnswers = activeQuiz
    ? quizAnswers[activeQuiz.id] ??
      activeQuizAttempt?.selectedAnswers ??
      new Array(activeQuiz.questions.length).fill(-1)
    : [];

  const handleSelectOption = (quizId: string, qIndex: number, optIndex: number, total: number) => {
    setQuizAnswers((prev) => {
      const base = prev[quizId] ? [...prev[quizId]] : new Array(total).fill(-1);
      base[qIndex] = optIndex;
      return { ...prev, [quizId]: base };
    });
  };

  const handleToggleCurrentLesson = async (lessonId: string) => {
    setBusyAction(true);
    try {
      await onToggleLesson(activeEnrollment.id, lessonId);
    } finally {
      setBusyAction(false);
    }
  };

  const handleQuizSubmit = async () => {
    if (!activeQuiz) return;
    setBusyAction(true);
    try {
      await onSubmitQuiz(activeEnrollment.id, activeQuiz.id, currentSelectedAnswers);
    } finally {
      setBusyAction(false);
    }
  };

  const handleHomeworkSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setHwError(null);
    if (hwText.trim().length < 10) {
      setHwError('Опишите решение задания подробнее (минимум 10 символов).');
      return;
    }
    setBusyAction(true);
    try {
      await onSubmitHomework({
        courseId: course.id,
        studentId: activeStudentId,
        answerText: hwText.trim(),
        projectUrl: hwUrl.trim() || undefined,
      });
      setHwText('');
      setHwUrl('');
    } catch (err) {
      setHwError(err instanceof Error ? err.message : 'Не удалось отправить задание');
    } finally {
      setBusyAction(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Player Control Strip */}
      <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-5 flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-4 min-w-0">
          <button
            type="button"
            onClick={onBackToMyCourses}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-slate-300 hover:text-white bg-[#0B0F17] border border-white/10 rounded-lg transition-colors cursor-pointer shrink-0"
          >
            <ArrowLeft className="w-3.5 h-3.5" />
            <span>Мои курсы</span>
          </button>

          <div className="min-w-0">
            <div className="text-xs text-slate-400 truncate">
              {course.category} · Преподаватель: {instructor?.name ?? 'Академия Вектор'}
            </div>
            <h1 className="font-display text-lg font-bold text-slate-50 truncate">
              {course.title}
            </h1>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-4">
          {myEnrollments.length > 1 && (
            <select
              aria-label="Выбрать другой курс"
              value={course.id}
              onChange={(e) => onSelectCourse(e.target.value)}
              className="bg-[#0B0F17] border border-white/10 rounded-lg px-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-amber-500 max-w-[240px]"
            >
              {myEnrollments.map((enr) => {
                const c = courses.find((item) => item.id === enr.courseId);
                if (!c) return null;
                return (
                  <option key={c.id} value={c.id}>
                    {c.title} ({enr.progressPercent}%)
                  </option>
                );
              })}
            </select>
          )}

          <div className="flex items-center gap-3">
            <div className="text-right">
              <div className="font-mono text-sm font-bold text-amber-400 tabular-nums">
                {activeEnrollment.progressPercent}%
              </div>
              <div className="text-[11px] text-slate-400 font-mono tabular-nums">
                {activeEnrollment.totalPoints} баллов
              </div>
            </div>
            <div className="w-24 h-2 rounded-full bg-[#0B0F17] overflow-hidden">
              <div
                className={`h-full transition-all duration-300 ${
                  activeEnrollment.progressPercent === 100 ? 'bg-emerald-500' : 'bg-amber-500'
                }`}
                style={{ width: `${activeEnrollment.progressPercent}%` }}
              />
            </div>
          </div>

          {activeEnrollment.progressPercent === 100 && (
            <button
              type="button"
              onClick={() => onOpenCertificate(activeEnrollment, course)}
              className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold rounded-lg bg-emerald-500/15 text-emerald-300 border border-emerald-500/30 hover:bg-emerald-500/25 transition-colors cursor-pointer whitespace-nowrap shrink-0"
            >
              <Award className="w-3.5 h-3.5" />
              <span>Сертификат</span>
            </button>
          )}
        </div>
      </div>

      {/* Main Split Workspace */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Left Curriculum Navigation */}
        <aside className="lg:col-span-4 rounded-2xl bg-[#131A26] border border-white/[0.08] p-5 space-y-6">
          <div className="space-y-4">
            <h2 className="text-xs font-semibold uppercase tracking-wider text-slate-400">
              Программа курса
            </h2>

            {course.modules.map((mod) => (
              <div key={mod.id} className="space-y-2">
                <div className="text-xs font-semibold text-slate-200 leading-snug">
                  {mod.title}
                </div>
                <div className="space-y-1">
                  {mod.lessons.map((les) => {
                    const completed = activeEnrollment.completedLessonIds.includes(les.id);
                    const isSelected =
                      selection.type === 'lesson' && selection.lessonId === les.id;
                    return (
                      <button
                        key={les.id}
                        type="button"
                        onClick={() => setSelection({ type: 'lesson', lessonId: les.id })}
                        className={`w-full text-left px-3 py-2.5 rounded-lg flex items-start gap-2.5 transition-colors cursor-pointer ${
                          isSelected
                            ? 'bg-amber-500/15 text-amber-200 border border-amber-500/30'
                            : 'text-slate-300 hover:bg-white/[0.04] border border-transparent'
                        }`}
                      >
                        {completed ? (
                          <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0 mt-0.5" />
                        ) : (
                          <Circle className="w-4 h-4 text-slate-500 shrink-0 mt-0.5" />
                        )}
                        <div className="min-w-0 flex-1">
                          <div className="text-xs font-medium leading-snug">{les.title}</div>
                          <div className="text-[11px] text-slate-400 font-mono tabular-nums mt-0.5">
                            {les.durationMinutes} мин
                          </div>
                        </div>
                      </button>
                    );
                  })}
                </div>
              </div>
            ))}
          </div>

          {/* Quizzes list */}
          {course.quizzes.length > 0 && (
            <div className="pt-4 border-t border-white/[0.06] space-y-2">
              <h2 className="text-xs font-semibold uppercase tracking-wider text-slate-400">
                Интерактивные тесты
              </h2>
              {course.quizzes.map((quiz) => {
                const attempt = activeEnrollment.quizAttempts[quiz.id];
                const isSelected = selection.type === 'quiz' && selection.quizId === quiz.id;
                return (
                  <button
                    key={quiz.id}
                    type="button"
                    onClick={() => setSelection({ type: 'quiz', quizId: quiz.id })}
                    className={`w-full text-left px-3 py-2.5 rounded-lg flex items-start gap-2.5 transition-colors cursor-pointer ${
                      isSelected
                        ? 'bg-amber-500/15 text-amber-200 border border-amber-500/30'
                        : 'text-slate-300 hover:bg-white/[0.04] border border-transparent'
                    }`}
                  >
                    <HelpCircle
                      className={`w-4 h-4 shrink-0 mt-0.5 ${
                        attempt?.passed ? 'text-emerald-400' : 'text-amber-400'
                      }`}
                    />
                    <div className="min-w-0 flex-1">
                      <div className="text-xs font-medium leading-snug">{quiz.title}</div>
                      <div className="text-[11px] text-slate-400 font-mono tabular-nums mt-0.5">
                        {attempt
                          ? `Результат: ${attempt.scorePercent}% (${attempt.passed ? 'Зачтён' : 'Повтор'})`
                          : `${quiz.questions.length} вопр. · +${quiz.pointsReward} б.`}
                      </div>
                    </div>
                  </button>
                );
              })}
            </div>
          )}

          {/* Homework section link */}
          <div className="pt-4 border-t border-white/[0.06] space-y-2">
            <h2 className="text-xs font-semibold uppercase tracking-wider text-slate-400">
              Практика
            </h2>
            <button
              type="button"
              onClick={() => setSelection({ type: 'homework' })}
              className={`w-full text-left px-3 py-2.5 rounded-lg flex items-start gap-2.5 transition-colors cursor-pointer ${
                selection.type === 'homework'
                  ? 'bg-amber-500/15 text-amber-200 border border-amber-500/30'
                  : 'text-slate-300 hover:bg-white/[0.04] border border-transparent'
              }`}
            >
              <ClipboardCheck className="w-4 h-4 text-amber-400 shrink-0 mt-0.5" />
              <div className="min-w-0 flex-1">
                <div className="text-xs font-medium leading-snug">Домашнее задание</div>
                <div className="text-[11px] text-slate-400 mt-0.5">
                  {courseSubmissions.length > 0
                    ? SUBMISSION_STATUS_META[courseSubmissions[0].status].label
                    : 'Ожидает отправки'}
                </div>
              </div>
            </button>
          </div>
        </aside>

        {/* Right Stage */}
        <section className="lg:col-span-8 rounded-2xl bg-[#131A26] border border-white/[0.08] p-6 md:p-8">
          {/* LESSON STAGE */}
          {selection.type === 'lesson' && activeLesson && (
            <div className="space-y-6">
              <div className="flex flex-wrap items-center justify-between gap-4 pb-5 border-b border-white/[0.06]">
                <div className="space-y-1.5">
                  <div className="flex items-center gap-2 text-xs text-slate-400">
                    <BookOpen className="w-3.5 h-3.5 text-amber-400" />
                    <span>Учебный материал</span>
                    <span aria-hidden="true">·</span>
                    <span className="font-mono tabular-nums">
                      {activeLesson.durationMinutes} минут
                    </span>
                  </div>
                  <h2 className="font-display text-2xl font-bold text-slate-50">
                    {activeLesson.title}
                  </h2>
                </div>
              </div>

              <p className="text-base text-amber-200/90 font-medium leading-relaxed">
                {activeLesson.summary}
              </p>

              <div className="space-y-4 text-[15px] text-slate-300 leading-relaxed max-w-[72ch]">
                {activeLesson.content.map((paragraph, i) => (
                  <p key={i}>{paragraph}</p>
                ))}
              </div>

              {activeLesson.codeExample && (
                <div className="rounded-xl bg-[#0B0F17] border border-white/[0.08] p-4 overflow-x-auto">
                  <div className="text-[11px] text-slate-400 mb-2 font-mono">Пример кода</div>
                  <pre className="font-mono text-xs text-amber-200 leading-relaxed">
                    <code>{activeLesson.codeExample}</code>
                  </pre>
                </div>
              )}

              <div className="rounded-xl bg-[#0B0F17] border border-white/[0.08] p-5 space-y-1.5">
                <div className="text-xs font-semibold text-amber-400">Ключевой вывод урока</div>
                <p className="text-sm text-slate-200 leading-relaxed">
                  {activeLesson.keyTakeaway}
                </p>
              </div>

              <div className="pt-4 border-t border-white/[0.06] flex flex-wrap items-center justify-between gap-4">
                <button
                  type="button"
                  disabled={busyAction}
                  onClick={() => handleToggleCurrentLesson(activeLesson.id)}
                  className={`inline-flex items-center gap-2 px-5 py-2.5 text-xs font-semibold rounded-lg transition-colors cursor-pointer disabled:opacity-50 ${
                    activeEnrollment.completedLessonIds.includes(activeLesson.id)
                      ? 'bg-emerald-500/15 text-emerald-300 border border-emerald-500/30 hover:bg-emerald-500/25'
                      : 'bg-amber-500 text-slate-950 hover:bg-amber-400'
                  }`}
                >
                  <CheckCircle2 className="w-4 h-4" />
                  <span>
                    {activeEnrollment.completedLessonIds.includes(activeLesson.id)
                      ? 'Урок завершён (отменить)'
                      : 'Завершить урок и обновить прогресс'}
                  </span>
                </button>

                {nextLesson ? (
                  <button
                    type="button"
                    onClick={() => setSelection({ type: 'lesson', lessonId: nextLesson.id })}
                    className="inline-flex items-center gap-1.5 px-4 py-2.5 text-xs font-medium rounded-lg bg-[#1A2332] text-slate-200 hover:text-white border border-white/10 transition-colors cursor-pointer"
                  >
                    <span>Следующий урок</span>
                    <ArrowRight className="w-3.5 h-3.5" />
                  </button>
                ) : course.quizzes[0] ? (
                  <button
                    type="button"
                    onClick={() =>
                      setSelection({ type: 'quiz', quizId: course.quizzes[0].id })
                    }
                    className="inline-flex items-center gap-1.5 px-4 py-2.5 text-xs font-medium rounded-lg bg-[#1A2332] text-slate-200 hover:text-white border border-white/10 transition-colors cursor-pointer"
                  >
                    <span>Перейти к тесту</span>
                    <ArrowRight className="w-3.5 h-3.5" />
                  </button>
                ) : null}
              </div>
            </div>
          )}

          {/* QUIZ STAGE */}
          {selection.type === 'quiz' && activeQuiz && (
            <div className="space-y-6">
              <div className="pb-5 border-b border-white/[0.06] flex flex-wrap items-center justify-between gap-4">
                <div className="space-y-1">
                  <div className="text-xs text-amber-400 font-medium">
                    Интерактивное тестирование · Порог зачёта {activeQuiz.passingScore}%
                  </div>
                  <h2 className="font-display text-2xl font-bold text-slate-50">
                    {activeQuiz.title}
                  </h2>
                </div>
                <div className="font-mono text-xs text-slate-300 tabular-nums">
                  Награда: +{activeQuiz.pointsReward} баллов
                </div>
              </div>

              {activeQuizAttempt && (
                <div
                  className={`p-4 rounded-xl border flex flex-wrap items-center justify-between gap-3 ${
                    activeQuizAttempt.passed
                      ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-200'
                      : 'bg-rose-500/10 border-rose-500/30 text-rose-200'
                  }`}
                >
                  <div className="flex items-center gap-2 text-sm font-medium">
                    {activeQuizAttempt.passed ? (
                      <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                    ) : (
                      <AlertTriangle className="w-4 h-4 text-rose-400" />
                    )}
                    <span>
                      {activeQuizAttempt.passed
                        ? `Тест успешно сдан! Начислено ${activeQuizAttempt.earnedPoints} баллов.`
                        : `Набрано ${activeQuizAttempt.scorePercent}%. Для зачёта нужно минимум ${activeQuiz.passingScore}% — попробуйте снова.`}
                    </span>
                  </div>
                  <span className="font-mono text-base font-bold tabular-nums">
                    {activeQuizAttempt.scorePercent}%
                  </span>
                </div>
              )}

              <div className="space-y-6">
                {activeQuiz.questions.map((question, qIndex) => {
                  const chosen = currentSelectedAnswers[qIndex] ?? -1;
                  const evaluated = Boolean(activeQuizAttempt);
                  const isCorrect = evaluated && activeQuizAttempt?.selectedAnswers[qIndex] === question.correctIndex;

                  return (
                    <div
                      key={question.id}
                      className="p-5 rounded-xl bg-[#0B0F17] border border-white/[0.08] space-y-3"
                    >
                      <div className="text-sm font-semibold text-slate-100 leading-snug">
                        {qIndex + 1}. {question.prompt}
                      </div>

                      <div className="space-y-2">
                        {question.options.map((opt, optIdx) => {
                          const selected = chosen === optIdx;
                          return (
                            <label
                              key={optIdx}
                              className={`flex items-center gap-3 p-3 rounded-lg border text-sm transition-colors cursor-pointer ${
                                selected
                                  ? 'bg-amber-500/15 border-amber-500/50 text-slate-50'
                                  : 'bg-[#131A26] border-white/[0.06] text-slate-300 hover:border-white/20'
                              }`}
                            >
                              <input
                                type="radio"
                                name={`quiz-${activeQuiz.id}-q-${qIndex}`}
                                checked={selected}
                                onChange={() =>
                                  handleSelectOption(
                                    activeQuiz.id,
                                    qIndex,
                                    optIdx,
                                    activeQuiz.questions.length,
                                  )
                                }
                                className="accent-amber-500"
                              />
                              <span>{opt}</span>
                            </label>
                          );
                        })}
                      </div>

                      {evaluated && (
                        <div
                          className={`text-xs pt-2 ${
                            isCorrect ? 'text-emerald-400' : 'text-amber-300'
                          }`}
                        >
                          {isCorrect ? 'Верно: ' : 'Пояснение: '}
                          {question.explanation}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>

              <div className="flex items-center justify-between gap-4 pt-2">
                <button
                  type="button"
                  disabled={busyAction || currentSelectedAnswers.some((v) => v < 0)}
                  onClick={handleQuizSubmit}
                  className="px-5 py-2.5 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer disabled:opacity-50"
                >
                  {busyAction
                    ? 'Проверка...'
                    : activeQuizAttempt
                      ? 'Отправить повторно'
                      : 'Проверить ответы'}
                </button>
                <span className="text-xs text-slate-400">
                  Выберите вариант ответа для каждого вопроса
                </span>
              </div>
            </div>
          )}

          {/* HOMEWORK STAGE */}
          {selection.type === 'homework' && (
            <div className="space-y-8">
              <div className="pb-5 border-b border-white/[0.06] space-y-2">
                <div className="text-xs text-amber-400 font-medium">
                  Практическое задание курса
                </div>
                <h2 className="font-display text-2xl font-bold text-slate-50">
                  {course.homeworkTask.title}
                </h2>
                <p className="text-sm text-slate-300 leading-relaxed">
                  {course.homeworkTask.description}
                </p>
                <p className="text-xs text-slate-400">{course.homeworkTask.deliverableHint}</p>
              </div>

              {/* Submission Form */}
              <form onSubmit={handleHomeworkSubmit} className="space-y-4">
                <div className="space-y-1.5">
                  <label className="block text-xs font-medium text-slate-300">
                    Текст решения и комментарии для преподавателя
                  </label>
                  <textarea
                    rows={4}
                    value={hwText}
                    onChange={(e) => setHwText(e.target.value)}
                    placeholder="Опишите ход решения, выводы или приложите ключевые фрагменты работы..."
                    className="w-full p-3.5 text-sm bg-[#0B0F17] border border-white/10 rounded-xl text-slate-100 placeholder:text-slate-500 focus:outline-none focus:border-amber-500"
                  />
                </div>

                <div className="space-y-1.5">
                  <label className="block text-xs font-medium text-slate-300">
                    Ссылка на проект, макет или репозиторий (необязательно)
                  </label>
                  <input
                    type="url"
                    value={hwUrl}
                    onChange={(e) => setHwUrl(e.target.value)}
                    placeholder="https://github.com/..."
                    className="w-full px-3.5 py-2.5 text-sm bg-[#0B0F17] border border-white/10 rounded-xl text-slate-100 placeholder:text-slate-500 focus:outline-none focus:border-amber-500"
                  />
                </div>

                {hwError && (
                  <div className="text-xs text-rose-400 flex items-center gap-1.5">
                    <AlertTriangle className="w-3.5 h-3.5 shrink-0" />
                    <span>{hwError}</span>
                  </div>
                )}

                <button
                  type="submit"
                  disabled={busyAction}
                  className="inline-flex items-center gap-2 px-5 py-2.5 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer disabled:opacity-50"
                >
                  <Send className="w-3.5 h-3.5" />
                  <span>{busyAction ? 'Отправка...' : 'Отправить на проверку'}</span>
                </button>
              </form>

              {/* Submission History */}
              <div className="space-y-3 pt-4 border-t border-white/[0.06]">
                <h3 className="text-sm font-semibold text-slate-200">
                  История отправок по этому курсу
                </h3>

                {courseSubmissions.length === 0 ? (
                  <p className="text-xs text-slate-400">
                    Пока нет отправленных решений.
                  </p>
                ) : (
                  <div className="space-y-3">
                    {courseSubmissions.map((sub) => {
                      const meta = SUBMISSION_STATUS_META[sub.status];
                      return (
                        <div
                          key={sub.id}
                          className="p-4 rounded-xl bg-[#0B0F17] border border-white/[0.08] space-y-2.5"
                        >
                          <div className="flex flex-wrap items-center justify-between gap-2">
                            <span className={`inline-flex items-center gap-1.5 text-xs font-semibold ${meta.textClass}`}>
                              {sub.status === 'approved' && <FileCheck2 className="w-4 h-4" />}
                              {sub.status === 'pending' && <Clock className="w-4 h-4" />}
                              {sub.status === 'revision' && <AlertTriangle className="w-4 h-4" />}
                              {meta.label}
                            </span>

                            <div className="flex items-center gap-3 text-xs text-slate-400 font-mono tabular-nums">
                              {sub.score !== undefined && (
                                <span className="text-slate-100 font-semibold">
                                  Оценка: {sub.score}/100
                                </span>
                              )}
                              <span>{formatShortDate(sub.submittedAt)}</span>
                            </div>
                          </div>

                          <p className="text-sm text-slate-300 leading-relaxed">{sub.answerText}</p>

                          {sub.projectUrl && (
                            <a
                              href={sub.projectUrl}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="inline-flex items-center gap-1 text-xs text-amber-400 hover:underline"
                            >
                              <span>Открыть прикреплённую ссылку</span>
                              <ExternalLink className="w-3 h-3" />
                            </a>
                          )}

                          {sub.instructorComment && (
                            <div className="pt-2 border-t border-white/[0.06] text-xs text-slate-300">
                              <span className="text-slate-400">Комментарий преподавателя: </span>
                              {sub.instructorComment}
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Instructor Signature Card */}
          <div className="mt-8 pt-6 border-t border-white/[0.06] flex items-center gap-3">
            <MediaImage
              src={instructor?.avatar}
              alt={instructor?.name ?? 'Преподаватель'}
              kind="avatar"
              fallbackName={instructor?.name}
              className="w-10 h-10 rounded-full object-cover shrink-0"
            />
            <div>
              <div className="text-xs font-semibold text-slate-200">
                {instructor?.name ?? 'Преподаватель курса'}
              </div>
              <div className="text-xs text-slate-400">
                {instructor?.specialty ?? 'Куратор учебной программы'}
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}
