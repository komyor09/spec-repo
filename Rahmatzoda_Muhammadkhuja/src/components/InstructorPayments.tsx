import { useMemo, useState } from 'react';
import { CheckCircle2, Clock, XCircle } from 'lucide-react';
import type { Course, PaymentRequest, PaymentStatus, User } from '../types/platform.ts';
import { PAYMENT_STATUS_META, formatRub, formatShortDate } from '../utils/formatters.ts';

interface InstructorPaymentsProps {
  payments: PaymentRequest[];
  students: User[];
  courses: Course[];
  onUpdateStatus: (paymentId: string, status: PaymentStatus) => Promise<void>;
}

export function InstructorPayments({
  payments,
  students,
  courses,
  onUpdateStatus,
}: InstructorPaymentsProps) {
  const [filter, setFilter] = useState<'all' | PaymentStatus>('all');
  const [busyId, setBusyId] = useState<string | null>(null);

  const studentById = useMemo(() => new Map(students.map((s) => [s.id, s])), [students]);
  const courseById = useMemo(() => new Map(courses.map((c) => [c.id, c])), [courses]);

  const filtered = useMemo(() => {
    if (filter === 'all') return payments;
    return payments.filter((p) => p.status === filter);
  }, [payments, filter]);

  const totalPaid = payments
    .filter((p) => p.status === 'paid')
    .reduce((sum, p) => sum + p.amountRub, 0);
  const totalPending = payments
    .filter((p) => p.status === 'pending')
    .reduce((sum, p) => sum + p.amountRub, 0);

  const handleAction = async (id: string, status: PaymentStatus) => {
    setBusyId(id);
    try {
      await onUpdateStatus(id, status);
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div className="space-y-8">
      <div className="flex flex-wrap items-end justify-between gap-4 border-b border-white/[0.08] pb-6">
        <div className="space-y-1.5">
          <p className="text-xs font-medium text-amber-400">Финансы и доступы</p>
          <h1 className="font-display text-2xl md:text-3xl font-bold text-slate-50">
            Учёт оплат и заявок на обучение
          </h1>
          <p className="text-sm text-slate-400">
            Подтверждение заявки автоматически открывает студенту доступ к курсу.
          </p>
        </div>

        <div className="flex items-center gap-6 shrink-0">
          <div>
            <div className="font-mono text-xl font-bold text-emerald-400 tabular-nums">
              {formatRub(totalPaid)}
            </div>
            <div className="text-xs text-slate-400">Оплачено</div>
          </div>
          <div className="h-8 w-px bg-white/10" />
          <div>
            <div className="font-mono text-xl font-bold text-amber-400 tabular-nums">
              {formatRub(totalPending)}
            </div>
            <div className="text-xs text-slate-400">В ожидании</div>
          </div>
        </div>
      </div>

      {/* Filter Tabs */}
      <div className="inline-flex items-center gap-1 p-1 bg-[#131A26] border border-white/[0.08] rounded-lg overflow-x-auto">
        {(
          [
            { id: 'all', label: 'Все заявки' },
            { id: 'pending', label: 'Ожидают оплаты' },
            { id: 'paid', label: 'Оплачено' },
            { id: 'cancelled', label: 'Отменено' },
          ] as const
        ).map((tab) => (
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

      {/* Table */}
      <div className="rounded-2xl bg-[#131A26] border border-white/[0.08] overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-white/[0.06] text-[11px] uppercase tracking-wider text-slate-400">
                <th className="py-3 px-6 font-medium">Дата</th>
                <th className="py-3 px-4 font-medium">Студент</th>
                <th className="py-3 px-4 font-medium">Курс</th>
                <th className="py-3 px-4 font-medium">Способ</th>
                <th className="py-3 px-4 font-medium text-right">Сумма</th>
                <th className="py-3 px-4 font-medium">Статус</th>
                <th className="py-3 px-6 font-medium text-right">Действия</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/[0.06] text-sm">
              {filtered.map((pay) => {
                const student = studentById.get(pay.studentId);
                const course = courseById.get(pay.courseId);
                const meta = PAYMENT_STATUS_META[pay.status];

                return (
                  <tr key={pay.id} className="hover:bg-white/[0.02]">
                    <td className="py-4 px-6 font-mono text-xs text-slate-400 tabular-nums whitespace-nowrap">
                      {formatShortDate(pay.createdAt)}
                    </td>
                    <td className="py-4 px-4">
                      <div className="font-medium text-slate-100">
                        {student?.name ?? 'Студент'}
                      </div>
                      <div className="text-xs text-slate-400">{student?.email}</div>
                    </td>
                    <td className="py-4 px-4 text-xs text-slate-200 max-w-[220px]">
                      {course?.title ?? 'Курс'}
                    </td>
                    <td className="py-4 px-4 text-xs text-slate-400">{pay.paymentMethod}</td>
                    <td className="py-4 px-4 text-right font-mono font-semibold text-slate-100 tabular-nums whitespace-nowrap">
                      {formatRub(pay.amountRub)}
                    </td>
                    <td className="py-4 px-4 whitespace-nowrap">
                      <span className={`inline-flex items-center gap-1.5 text-xs font-semibold ${meta.textClass}`}>
                        {pay.status === 'paid' && <CheckCircle2 className="w-3.5 h-3.5" />}
                        {pay.status === 'pending' && <Clock className="w-3.5 h-3.5" />}
                        {pay.status === 'cancelled' && <XCircle className="w-3.5 h-3.5" />}
                        {meta.label}
                      </span>
                    </td>
                    <td className="py-4 px-6 text-right whitespace-nowrap">
                      <div className="inline-flex items-center gap-2">
                        {pay.status !== 'paid' && (
                          <button
                            type="button"
                            disabled={busyId === pay.id}
                            onClick={() => handleAction(pay.id, 'paid')}
                            className="px-3 py-1.5 text-xs font-semibold rounded-md bg-emerald-500/15 text-emerald-300 border border-emerald-500/30 hover:bg-emerald-500/25 transition-colors cursor-pointer disabled:opacity-50"
                          >
                            Подтвердить
                          </button>
                        )}
                        {pay.status === 'pending' && (
                          <button
                            type="button"
                            disabled={busyId === pay.id}
                            onClick={() => handleAction(pay.id, 'cancelled')}
                            className="px-3 py-1.5 text-xs font-medium rounded-md text-rose-300 border border-rose-500/20 hover:border-rose-500/40 transition-colors cursor-pointer disabled:opacity-50"
                          >
                            Отклонить
                          </button>
                        )}
                      </div>
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
