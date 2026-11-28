import {
  AlertTriangle,
  CheckCircle2,
  Clock,
  Stethoscope,
  XCircle,
} from 'lucide-react';
import type {AppointmentStatus, Pet} from '../types/clinic.ts';

export function AppointmentStatusLabel({status}: {status: AppointmentStatus}) {
  switch (status) {
    case 'Запланирован':
      return (
        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-amber-700 whitespace-nowrap">
          <Clock size={13} aria-hidden="true" />
          {status}
        </span>
      );
    case 'На приеме':
      return (
        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-teal-700 whitespace-nowrap">
          <Stethoscope size={13} aria-hidden="true" />
          {status}
        </span>
      );
    case 'Завершен':
      return (
        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-700 whitespace-nowrap">
          <CheckCircle2 size={13} aria-hidden="true" />
          {status}
        </span>
      );
    case 'Отменен':
      return (
        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-slate-500 whitespace-nowrap">
          <XCircle size={13} aria-hidden="true" />
          {status}
        </span>
      );
  }
}

export function PetHealthLabel({status}: {status: Pet['status']}) {
  switch (status) {
    case 'Здоров':
      return (
        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-emerald-700 whitespace-nowrap">
          <CheckCircle2 size={13} aria-hidden="true" />
          {status}
        </span>
      );
    case 'Наблюдение':
      return (
        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-amber-700 whitespace-nowrap">
          <Clock size={13} aria-hidden="true" />
          {status}
        </span>
      );
    case 'Стационар':
      return (
        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-red-700 whitespace-nowrap">
          <AlertTriangle size={13} aria-hidden="true" />
          {status}
        </span>
      );
  }
}
