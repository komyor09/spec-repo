import { useMemo, useState } from 'react';
import { CheckCircle2, AlertTriangle, Clock, ExternalLink, Send } from 'lucide-react';
import type { Course, HomeworkSubmission, SubmissionStatus, User } from '../types/platform.ts';
import { SUBMISSION_STATUS_META, formatShortDate } from '../utils/formatters.ts';

interface InstructorHomeworkProps {
  submissions: HomeworkSubmission[];
  courses: Course[];
  students: User[];
  onReview: (
    submissionId: string,
    payload: {
      status: SubmissionStatus;
      score?: number;
      instructorComment: string;
    },
  ) => Promise<void>;
}

export function InstructorHomework({
  submissions,
  courses,
  students,
  onReview,
}: InstructorHomeworkProps) {
  const [filter, setFilter] = useState<'all' | SubmissionStatus>('all');
  const [drafts, setDrafts] = useState<
    Record<string, { status: SubmissionStatus; score: number; comment: string }>
  >({});
  const [busyId, setBusyId] = useState<string | null>(null);
  const [feedbackBanner, setFeedbackBanner] = useState<string | null>(null);

  const courseById = useMemo(() => new Map(courses.map((c) => [c.id, c])), [courses]);
  const studentById = useMemo(() => new Map(students.map((s) => [s.id, s])), [students]);

  const filtered = useMemo(() => {
    if (filter === 'all') return submissions;
    return submissions.filter((s) => s.status === filter);
  }, [submissions, filter]);

  const getDraft = (sub: HomeworkSubmission) =>
    drafts[sub.id] ?? {
      status: sub.status === 'pending' ? 'approved' : sub.status,
      score: sub.score ?? 90,
      comment: sub.instructorComment ?? '',
    };

  const updateDraft = (
    sub: HomeworkSubmission,
    patch: Partial<{ status: SubmissionStatus; score: number; comment: string }>,
  ) => {
    const base = getDraft(sub);
    setDrafts((prev) => ({
      ...prev,
      [sub.id]: { ...base, ...patch },
    }));
  };

  const handleSaveReview = async (sub: HomeworkSubmission) => {
    const draft = getDraft(sub);
    setBusyId(sub.id);
    try {
      await onReview(sub.id, {
        status: draft.status,
        score: draft.score,
        instructorComment: draft.comment,
      });
      setFeedbackBanner('Оценка и комментарий сохранены.');
      setTimeout(() => setFeedbackBanner(null), 3000);
    } finally {
      setBusyId(null);
    }
  };

  const filterTabs: Array<{ id: 'all' | SubmissionStatus; label: string }> = [
    { id: 'all', label: `Все (${submissions.length})` },
    {
      id: 'pending',
      label: `На проверке (${submissions.filter((s) => s.status === 'pending').length})`,
    },
    {
      id: 'approved',
      label: `Принято (${submissions.filter((s) => s.status === 'approved').length})`,
    },
    {
      id: 'revision',
      label: `Доработка (${submissions.filter((s) => s.status === 'revision').length})`,
    },
  ];

  return (
    <div className="space-y-8">
      <div className="flex flex-wrap items-end justify-between gap-4 border-b border-white/[0.08] pb-6">
        <div className="space-y-1.5">
          <p className="text-xs font-medium text-amber-400">Обратная связь студентам</p>
          <h1 className="font-display text-2xl md:text-3xl font-bold text-slate-50">
            Проверка домашних заданий
          </h1>
        </div>

        <div className="flex items-center gap-1 p-1 bg-[#131A26] border border-white/[0.08] rounded-lg overflow-x-auto">
          {filterTabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setFilter(tab.id)}
              className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors whitespace-nowrap shrink-0 cursor-pointer ${
                filter === tab.id
                  ? 'bg-amber-500 text-slate-950'
                  : 'text-slate-300 hover:text-white'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {feedbackBanner && (
        <div className="p-3.5 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-xs font-medium text-emerald-200 flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4 text-emerald-400" />
          <span>{feedbackBanner}</span>
        </div>
      )}

      {filtered.length === 0 ? (
        <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-12 text-center text-sm text-slate-400">
          В выбранной категории пока нет домашних заданий.
        </div>
      ) : (
        <div className="space-y-5">
          {filtered.map((sub) => {
            const student = studentById.get(sub.studentId);
            const course = courseById.get(sub.courseId);
            const meta = SUBMISSION_STATUS_META[sub.status];
            const draft = getDraft(sub);

            return (
              <article
                key={sub.id}
                className="rounded-2xl bg-[#131A26] border border-white/[0.08] p-6 grid grid-cols-1 lg:grid-cols-12 gap-6"
              >
                {/* Submission Details */}
                <div className="lg:col-span-7 space-y-3">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="text-xs text-slate-400">
                      <span className="font-semibold text-slate-100">
                        {student?.name ?? 'Студент'}
                      </span>
                      <span aria-hidden="true"> · </span>
                      <span className="text-amber-400">{course?.title ?? 'Курс'}</span>
                    </div>
                    <span className="font-mono text-xs text-slate-400 tabular-nums">
                      Отправлено: {formatShortDate(sub.submittedAt)}
                    </span>
                  </div>

                  <div className="p-4 rounded-xl bg-[#0B0F17] border border-white/[0.06] space-y-2">
                    <div className="text-xs font-medium text-slate-400">
                      Ответ студента:
                    </div>
                    <p className="text-sm text-slate-200 leading-relaxed whitespace-pre-line">
                      {sub.answerText}
                    </p>
                    {sub.projectUrl && (
                      <a
                        href={sub.projectUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex items-center gap-1.5 text-xs text-amber-400 hover:underline pt-1"
                      >
                        <span>{sub.projectUrl}</span>
                        <ExternalLink className="w-3 h-3" />
                      </a>
                    )}
                  </div>

                  <div className="flex items-center gap-3 text-xs">
                    <span className="text-slate-400">Текущий статус:</span>
                    <span className={`inline-flex items-center gap-1.5 font-semibold ${meta.textClass}`}>
                      {sub.status === 'approved' && <CheckCircle2 className="w-3.5 h-3.5" />}
                      {sub.status === 'pending' && <Clock className="w-3.5 h-3.5" />}
                      {sub.status === 'revision' && <AlertTriangle className="w-3.5 h-3.5" />}
                      {meta.label}
                    </span>
                    {sub.score !== undefined && (
                      <span className="font-mono text-slate-200 tabular-nums">
                        ({sub.score} / 100)
                      </span>
                    )}
                  </div>
                </div>

                {/* Review Controls */}
                <div className="lg:col-span-5 rounded-xl bg-[#0B0F17] border border-white/[0.06] p-4 space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <div className="space-y-1">
                      <label className="block text-[11px] text-slate-400">Решение</label>
                      <select
                        value={draft.status}
                        onChange={(e) =>
                          updateDraft(sub, { status: e.target.value as SubmissionStatus })
                        }
                        className="w-full px-2.5 py-2 text-xs bg-[#131A26] border border-white/10 rounded-lg text-slate-100 focus:outline-none focus:border-amber-500"
                      >
                        <option value="approved">Принято</option>
                        <option value="revision">Нужна доработка</option>
                        <option value="pending">На проверке</option>
                      </select>
                    </div>

                    <div className="space-y-1">
                      <label className="block text-[11px] text-slate-400">Оценка (0–100)</label>
                      <input
                        type="number"
                        min={0}
                        max={100}
                        value={draft.score}
                        onChange={(e) =>
                          updateDraft(sub, { score: Number(e.target.value) })
                        }
                        className="w-full px-2.5 py-2 text-xs bg-[#131A26] border border-white/10 rounded-lg text-slate-100 font-mono tabular-nums focus:outline-none focus:border-amber-500"
                      />
                    </div>
                  </div>

                  <div className="space-y-1">
                    <label className="block text-[11px] text-slate-400">
                      Комментарий преподавателя
                    </label>
                    <textarea
                      rows={3}
                      value={draft.comment}
                      onChange={(e) => updateDraft(sub, { comment: e.target.value })}
                      placeholder="Напишите разбор сильных сторон и рекомендации..."
                      className="w-full p-2.5 text-xs bg-[#131A26] border border-white/10 rounded-lg text-slate-100 placeholder:text-slate-500 focus:outline-none focus:border-amber-500"
                    />
                  </div>

                  <button
                    type="button"
                    disabled={busyId === sub.id}
                    onClick={() => handleSaveReview(sub)}
                    className="w-full inline-flex items-center justify-center gap-1.5 py-2.5 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer disabled:opacity-50"
                  >
                    <Send className="w-3.5 h-3.5" />
                    <span>{busyId === sub.id ? 'Сохранение...' : 'Сохранить проверку'}</span>
                  </button>
                </div>
              </article>
            );
          })}
        </div>
      )}
    </div>
  );
}
