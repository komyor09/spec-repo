import { RotateCcw, GraduationCap, Briefcase } from 'lucide-react';
import type { User, UserRole } from '../types/platform.ts';

export type StudentTab = 'catalog' | 'my-courses' | 'player' | 'webinars';
export type InstructorTab =
  | 'overview'
  | 'courses'
  | 'homework'
  | 'students'
  | 'schedule'
  | 'payments';

interface HeaderProps {
  role: UserRole;
  onRoleChange: (role: UserRole) => void;
  studentTab: StudentTab;
  onStudentTabChange: (tab: StudentTab) => void;
  instructorTab: InstructorTab;
  onInstructorTabChange: (tab: InstructorTab) => void;
  students: User[];
  activeStudentId: string;
  onSelectStudent: (id: string) => void;
  onPrimaryAction: () => void;
  onResetDemo: () => void;
  resetting: boolean;
  pendingHomeworkCount: number;
}

export function Header({
  role,
  onRoleChange,
  studentTab,
  onStudentTabChange,
  instructorTab,
  onInstructorTabChange,
  students,
  activeStudentId,
  onSelectStudent,
  onPrimaryAction,
  onResetDemo,
  resetting,
  pendingHomeworkCount,
}: HeaderProps) {
  const studentLinks: Array<{ id: StudentTab; label: string }> = [
    { id: 'catalog', label: 'Каталог' },
    { id: 'my-courses', label: 'Мои курсы' },
    { id: 'player', label: 'Обучение' },
    { id: 'webinars', label: 'Вебинары' },
  ];

  const instructorLinks: Array<{ id: InstructorTab; label: string }> = [
    { id: 'overview', label: 'Дашборд' },
    { id: 'courses', label: 'Курсы' },
    {
      id: 'homework',
      label: pendingHomeworkCount > 0 ? `Проверка ДЗ (${pendingHomeworkCount})` : 'Проверка ДЗ',
    },
    { id: 'students', label: 'Студенты' },
    { id: 'schedule', label: 'Расписание' },
    { id: 'payments', label: 'Оплаты' },
  ];

  return (
    <div className="sticky top-0 z-40 bg-[#0B0F17]/95 backdrop-blur-md">
      {/* Strict 3-zone top bar contract */}
      <header className="max-w-[1320px] mx-auto px-6 h-16 flex items-center justify-between gap-8 border-b border-white/[0.08]">
        {/* Zone 1: Single-element brand wordmark */}
        <button
          type="button"
          onClick={() =>
            role === 'student' ? onStudentTabChange('catalog') : onInstructorTabChange('overview')
          }
          className="font-display text-lg font-bold tracking-tight text-slate-50 hover:text-amber-400 transition-colors whitespace-nowrap shrink-0 cursor-pointer"
        >
          Академия Вектор
        </button>

        {/* Zone 2: Clean single-line navigation links */}
        <nav className="hidden lg:flex items-center gap-6 overflow-x-auto">
          {role === 'student'
            ? studentLinks.map((item) => {
                const active = studentTab === item.id;
                return (
                  <button
                    key={item.id}
                    type="button"
                    onClick={() => onStudentTabChange(item.id)}
                    className={`py-1.5 text-sm font-medium transition-colors whitespace-nowrap shrink-0 cursor-pointer border-b-2 ${
                      active
                        ? 'text-amber-400 border-amber-400'
                        : 'text-slate-300 border-transparent hover:text-slate-50 hover:border-white/20'
                    }`}
                  >
                    {item.label}
                  </button>
                );
              })
            : instructorLinks.map((item) => {
                const active = instructorTab === item.id;
                return (
                  <button
                    key={item.id}
                    type="button"
                    onClick={() => onInstructorTabChange(item.id)}
                    className={`py-1.5 text-sm font-medium transition-colors whitespace-nowrap shrink-0 cursor-pointer border-b-2 ${
                      active
                        ? 'text-amber-400 border-amber-400'
                        : 'text-slate-300 border-transparent hover:text-slate-50 hover:border-white/20'
                    }`}
                  >
                    {item.label}
                  </button>
                );
              })}
        </nav>

        {/* Zone 3: Single primary action */}
        <div className="flex items-center shrink-0">
          <button
            type="button"
            onClick={onPrimaryAction}
            className="px-4 py-2 text-xs font-semibold rounded-lg bg-amber-500 text-slate-950 hover:bg-amber-400 transition-colors whitespace-nowrap shrink-0 cursor-pointer"
          >
            {role === 'student' ? 'Продолжить обучение' : 'Создать курс'}
          </button>
        </div>
      </header>

      {/* Mobile navigation strip */}
      <div className="lg:hidden border-b border-white/[0.08] px-4 py-2 flex items-center gap-4 overflow-x-auto bg-[#0F1520]">
        {(role === 'student' ? studentLinks : instructorLinks).map((item) => {
          const active =
            role === 'student' ? studentTab === item.id : instructorTab === item.id;
          return (
            <button
              key={item.id}
              type="button"
              onClick={() =>
                role === 'student'
                  ? onStudentTabChange(item.id as StudentTab)
                  : onInstructorTabChange(item.id as InstructorTab)
              }
              className={`px-2.5 py-1.5 text-xs font-medium rounded-md whitespace-nowrap shrink-0 transition-colors cursor-pointer ${
                active
                  ? 'bg-amber-500/15 text-amber-400'
                  : 'text-slate-400 hover:text-slate-100'
              }`}
            >
              {item.label}
            </button>
          );
        })}
      </div>

      {/* Workspace role switcher & demo controls below header */}
      <div className="border-b border-white/[0.06] bg-[#101622]">
        <div className="max-w-[1320px] mx-auto px-6 py-2.5 flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3 flex-wrap">
            <span className="text-xs text-slate-400 whitespace-nowrap">Режим работы:</span>
            <div className="inline-flex items-center p-1 rounded-lg bg-[#0B0F17] border border-white/[0.08]">
              <button
                type="button"
                onClick={() => onRoleChange('student')}
                className={`inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md transition-colors whitespace-nowrap shrink-0 cursor-pointer ${
                  role === 'student'
                    ? 'bg-amber-500 text-slate-950'
                    : 'text-slate-300 hover:text-white'
                }`}
              >
                <GraduationCap className="w-3.5 h-3.5" />
                <span>Кабинет студента</span>
              </button>
              <button
                type="button"
                onClick={() => onRoleChange('instructor')}
                className={`inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md transition-colors whitespace-nowrap shrink-0 cursor-pointer ${
                  role === 'instructor'
                    ? 'bg-amber-500 text-slate-950'
                    : 'text-slate-300 hover:text-white'
                }`}
              >
                <Briefcase className="w-3.5 h-3.5" />
                <span>Кабинет преподавателя</span>
              </button>
            </div>
          </div>

          <div className="flex items-center gap-4 flex-wrap">
            {role === 'student' && (
              <label className="flex items-center gap-2 text-xs text-slate-400">
                <span className="whitespace-nowrap">Профиль студента:</span>
                <select
                  value={activeStudentId}
                  onChange={(e) => onSelectStudent(e.target.value)}
                  className="bg-[#131A26] border border-white/10 rounded-md px-2.5 py-1 text-xs text-slate-100 focus:outline-none focus:border-amber-500"
                >
                  {students.map((st) => (
                    <option key={st.id} value={st.id}>
                      {st.name}
                    </option>
                  ))}
                </select>
              </label>
            )}

            <button
              type="button"
              onClick={onResetDemo}
              disabled={resetting}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-slate-300 hover:text-amber-400 border border-white/10 hover:border-amber-500/40 rounded-md transition-colors whitespace-nowrap shrink-0 cursor-pointer disabled:opacity-50"
            >
              <RotateCcw className={`w-3.5 h-3.5 ${resetting ? 'animate-spin' : ''}`} />
              <span>{resetting ? 'Сброс...' : 'Сбросить демо'}</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
