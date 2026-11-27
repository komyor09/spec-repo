import type { PaymentStatus, SubmissionStatus } from '../types/platform.ts';

export function formatRub(amount: number): string {
  if (amount <= 0) return 'Бесплатно';
  return `${new Intl.NumberFormat('ru-RU').format(amount)} ₽`;
}

export function formatRuDate(dateStr?: string): string {
  if (!dateStr) return '—';
  const parsed = new Date(dateStr);
  if (Number.isNaN(parsed.getTime())) return dateStr;
  return new Intl.DateTimeFormat('ru-RU', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(parsed);
}

export function formatShortDate(dateStr?: string): string {
  if (!dateStr) return '—';
  const parsed = new Date(dateStr);
  if (Number.isNaN(parsed.getTime())) return dateStr;
  return new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(parsed);
}

export function pluralize(count: number, one: string, few: string, many: string): string {
  const mod10 = count % 10;
  const mod100 = count % 100;
  if (mod10 === 1 && mod100 !== 11) return `${count} ${one}`;
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return `${count} ${few}`;
  return `${count} ${many}`;
}

export function getInitials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  if (parts.length === 0) return 'АВ';
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}

export const SUBMISSION_STATUS_META: Record<
  SubmissionStatus,
  { label: string; textClass: string }
> = {
  pending: {
    label: 'На проверке',
    textClass: 'text-amber-400',
  },
  approved: {
    label: 'Принято',
    textClass: 'text-emerald-400',
  },
  revision: {
    label: 'Нужна доработка',
    textClass: 'text-rose-400',
  },
};

export const PAYMENT_STATUS_META: Record<
  PaymentStatus,
  { label: string; textClass: string }
> = {
  paid: {
    label: 'Оплачено',
    textClass: 'text-emerald-400',
  },
  pending: {
    label: 'Ожидает оплаты',
    textClass: 'text-amber-400',
  },
  cancelled: {
    label: 'Отменено',
    textClass: 'text-rose-400',
  },
};
