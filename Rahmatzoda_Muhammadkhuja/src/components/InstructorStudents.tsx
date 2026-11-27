import { useMemo, useState } from 'react';
import { Plus, Users } from 'lucide-react';
import type { Course, Enrollment, Group, HomeworkSubmission, User } from '../types/platform.ts';

interface InstructorStudentsProps {
  students: User[];
  instructors: User[];
  groups: Group[];
  courses: Course[];
  enrollments: Enrollment[];
  submissions: HomeworkSubmission[];
  onCreateGroup: (payload: {
    name: string;
    curatorId: string;
    scheduleNote?: string;
  }) => Promise<void>;
  onAssignGroup: (studentId: string, groupId: string) => Promise<void>;
}

export function InstructorStudents({
  students,
  instructors,
  groups,
  courses,
  enrollments,
  submissions,
  onCreateGroup,
  onAssignGroup,
}: InstructorStudentsProps) {
  const [groupFilter, setGroupFilter] = useState<string>('all');
  const [groupName, setGroupName] = useState('');
  const [curatorId, setCuratorId] = useState(instructors[0]?.id ?? 'inst-1');
  const [scheduleNote, setScheduleNote] = useState('');
  const [busy, setBusy] = useState(false);

  const courseById = useMemo(() => new Map(courses.map((c) => [c.id, c])), [courses]);
  const instructorById = useMemo(
    () => new Map(instructors.map((i) => [i.id, i])),
    [instructors],
  );

  const filteredStudents = useMemo(() => {
    if (groupFilter === 'all') return students;
    return students.filter((s) => s.groupId === groupFilter);
  }, [students, groupFilter]);

  const handleCreateGroup = async (e: React.FormEvent) => {
    e.preventDefault();
    if (groupName.trim().length < 3) return;
    setBusy(true);
    try {
      await onCreateGroup({
        name: groupName.trim(),
        curatorId,
        scheduleNote: scheduleNote.trim() || undefined,
      });
      setGroupName('');
      setScheduleNote('');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-8">
      <div className="space-y-1.5 border-b border-white/[0.08] pb-6">
        <p className="text-xs font-medium text-amber-400">Учебные потоки и успеваемость</p>
        <h1 className="font-display text-2xl md:text-3xl font-bold text-slate-50">
          Студенты и группы
        </h1>
      </div>

      {/* Groups & Create Group */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        <div className="lg:col-span-8 grid grid-cols-1 sm:grid-cols-2 gap-4">
          {groups.map((grp) => {
            const membersCount = students.filter((s) => s.groupId === grp.id).length;
            const curator = instructorById.get(grp.curatorId);
            return (
              <button
                key={grp.id}
                type="button"
                onClick={() => setGroupFilter((prev) => (prev === grp.id ? 'all' : grp.id))}
                className={`text-left rounded-2xl p-5 border transition-colors cursor-pointer space-y-2 ${
                  groupFilter === grp.id
                    ? 'bg-amber-500/15 border-amber-500/50'
                    : 'bg-[#131A26] border-white/[0.08] hover:border-white/20'
                }`}
              >
                <div className="flex items-center justify-between gap-2 text-xs text-slate-400">
                  <span className="font-mono tabular-nums">{grp.scheduleNote}</span>
                  <span className="inline-flex items-center gap-1 font-mono tabular-nums text-amber-400">
                    <Users className="w-3.5 h-3.5" />
                    {membersCount}
                  </span>
                </div>
                <div className="font-display text-base font-semibold text-slate-50">
                  {grp.name}
                </div>
                <div className="text-xs text-slate-400">
                  Куратор: {curator?.name ?? 'Преподаватель'}
                </div>
              </button>
            );
          })}
        </div>

        <form
          onSubmit={handleCreateGroup}
          className="lg:col-span-4 rounded-2xl bg-[#131A26] border border-white/[0.08] p-5 space-y-3"
        >
          <div className="flex items-center gap-1.5 text-sm font-semibold text-slate-100">
            <Plus className="w-4 h-4 text-amber-400" />
            <span>Создать учебную группу</span>
          </div>
          <input
            required
            value={groupName}
            onChange={(e) => setGroupName(e.target.value)}
            placeholder="Название потока (например, Зима-2026)"
            className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
          />
          <select
            value={curatorId}
            onChange={(e) => setCuratorId(e.target.value)}
            className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
          >
            {instructors.map((inst) => (
              <option key={inst.id} value={inst.id}>
                Куратор: {inst.name}
              </option>
            ))}
          </select>
          <input
            value={scheduleNote}
            onChange={(e) => setScheduleNote(e.target.value)}
            placeholder="График (например, Пн / Ср · 19:00 МСК)"
            className="w-full px-3 py-2 text-xs bg-[#0B0F17] border border-white/10 rounded-lg text-slate-100"
          />
          <button
            type="submit"
            disabled={busy}
            className="w-full py-2 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors cursor-pointer"
          >
            {busy ? 'Создание...' : 'Добавить группу'}
          </button>
        </form>
      </div>

      {/* Student Roster Table */}
      <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] overflow-hidden">
        <div className="px-6 py-4 border-b border-white/[0.06] flex flex-wrap items-center justify-between gap-3">
          <h2 className="font-display text-lg font-semibold text-slate-50">
            Успеваемость студентов ({filteredStudents.length})
          </h2>
          {groupFilter !== 'all' && (
            <button
              type="button"
              onClick={() => setGroupFilter('all')}
              className="text-xs text-amber-400 hover:text-amber-300 cursor-pointer"
            >
              Показать все группы
            </button>
          )}
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-white/[0.06] text-[11px] uppercase tracking-wider text-slate-400">
                <th className="py-3 px-6 font-medium">Студент</th>
                <th className="py-3 px-4 font-medium">Группа</th>
                <th className="py-3 px-4 font-medium">Курсы и прогресс</th>
                <th className="py-3 px-4 font-medium text-right">Баллы</th>
                <th className="py-3 px-6 font-medium text-right">Ср. оценка ДЗ</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/[0.06] text-sm">
              {filteredStudents.map((st) => {
                const stEnrollments = enrollments.filter((e) => e.studentId === st.id);
                const totalPoints = stEnrollments.reduce((acc, e) => acc + e.totalPoints, 0);
                const gradedSubs = submissions.filter(
                  (s) => s.studentId === st.id && s.score !== undefined,
                );
                const avgScore =
                  gradedSubs.length > 0
                    ? Math.round(
                        gradedSubs.reduce((sum, s) => sum + (s.score ?? 0), 0) / gradedSubs.length,
                      )
                    : null;

                return (
                  <tr key={st.id} className="hover:bg-white/[0.02]">
                    <td className="py-4 px-6">
                      <div className="font-medium text-slate-100">{st.name}</div>
                      <div className="text-xs text-slate-400">{st.email}</div>
                    </td>

                    <td className="py-4 px-4">
                      <select
                        aria-label={`Группа для ${st.name}`}
                        value={st.groupId ?? ''}
                        onChange={(e) => onAssignGroup(st.id, e.target.value)}
                        className="bg-[#0B0F17] border border-white/10 rounded-md px-2.5 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-amber-500"
                      >
                        <option value="">Без группы</option>
                        {groups.map((g) => (
                          <option key={g.id} value={g.id}>
                            {g.name}
                          </option>
                        ))}
                      </select>
                    </td>

                    <td className="py-4 px-4">
                      {stEnrollments.length === 0 ? (
                        <span className="text-xs text-slate-500">Нет активных курсов</span>
                      ) : (
                        <div className="space-y-1.5 min-w-[220px]">
                          {stEnrollments.map((enr) => {
                            const c = courseById.get(enr.courseId);
                            return (
                              <div key={enr.id} className="flex items-center justify-between gap-3 text-xs">
                                <span className="text-slate-300 truncate max-w-[180px]">
                                  {c?.title ?? 'Курс'}
                                </span>
                                <span className="font-mono font-semibold text-amber-400 tabular-nums shrink-0">
                                  {enr.progressPercent}%
                                </span>
                              </div>
                            );
                          })}
                        </div>
                      )}
                    </td>

                    <td className="py-4 px-4 text-right font-mono font-semibold text-slate-100 tabular-nums">
                      {totalPoints}
                    </td>

                    <td className="py-4 px-6 text-right font-mono tabular-nums text-slate-200">
                      {avgScore !== null ? `${avgScore}/100` : '—'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
