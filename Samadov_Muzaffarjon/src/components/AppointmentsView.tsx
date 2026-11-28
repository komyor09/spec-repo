import {CalendarPlus, Search} from 'lucide-react';
import {useMemo, useState} from 'react';
import type {AppointmentStatus, ClinicState} from '../types/clinic.ts';
import {PetAvatar} from './PetAvatar.tsx';
import {AppointmentStatusLabel} from './StatusText.tsx';

export function AppointmentsView({
  state,
  onOpenNewAppointment,
  onUpdateStatus,
  onSelectPet,
}: {
  state: ClinicState;
  onOpenNewAppointment: () => void;
  onUpdateStatus: (id: string, status: AppointmentStatus) => void;
  onSelectPet: (petId: string) => void;
}) {
  const [statusFilter, setStatusFilter] = useState<'Все' | AppointmentStatus>(
    'Все',
  );
  const [vetFilter, setVetFilter] = useState<string>('all');
  const [search, setSearch] = useState('');

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return state.appointments.filter((apt) => {
      if (statusFilter !== 'Все' && apt.status !== statusFilter) return false;
      if (vetFilter !== 'all' && apt.veterinarianId !== vetFilter) return false;
      if (!q) return true;
      const pet = state.pets.find((p) => p.id === apt.petId);
      const owner = state.owners.find((o) => o.id === pet?.ownerId);
      return (
        (pet?.name.toLowerCase().includes(q) ?? false) ||
        (owner?.fullName.toLowerCase().includes(q) ?? false) ||
        apt.reason.toLowerCase().includes(q) ||
        apt.serviceType.toLowerCase().includes(q)
      );
    });
  }, [state, statusFilter, vetFilter, search]);

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex flex-wrap items-center gap-3">
          <div className="relative w-64">
            <Search
              size={15}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
            />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Питомец, услуга, повод..."
              className="w-full h-10 pl-9 pr-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>

          <select
            value={vetFilter}
            onChange={(e) => setVetFilter(e.target.value)}
            className="h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-800 focus:outline-none focus:border-teal-700"
          >
            <option value="all">Все врачи</option>
            {state.veterinarians.map((v) => (
              <option key={v.id} value={v.id}>
                {v.fullName}
              </option>
            ))}
          </select>

          <div className="flex items-center gap-1 p-1 bg-slate-200/70 rounded-lg overflow-x-auto">
            {(
              [
                'Все',
                'Запланирован',
                'На приеме',
                'Завершен',
                'Отменен',
              ] as const
            ).map((st) => (
              <button
                key={st}
                type="button"
                onClick={() => setStatusFilter(st)}
                className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors whitespace-nowrap shrink-0 ${
                  statusFilter === st
                    ? 'bg-white text-slate-900 shadow-sm'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                {st}
              </button>
            ))}
          </div>
        </div>

        <button
          type="button"
          onClick={onOpenNewAppointment}
          className="inline-flex items-center gap-2 px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 transition-colors whitespace-nowrap shrink-0"
        >
          <CalendarPlus size={15} />
          Записать на прием
        </button>
      </div>

      <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
        {filtered.length === 0 ? (
          <div className="p-10 text-center">
            <p className="text-sm text-slate-600 mb-4">
              Записей с выбранными параметрами не найдено.
            </p>
            <button
              type="button"
              onClick={onOpenNewAppointment}
              className="px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800"
            >
              Создать запись
            </button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                  <th className="py-3 px-4">Дата и слот</th>
                  <th className="py-3 px-4">Пациент</th>
                  <th className="py-3 px-4">Врач и кабинет</th>
                  <th className="py-3 px-4">Услуга и причина</th>
                  <th className="py-3 px-4">Статус</th>
                  <th className="py-3 px-4 text-right">Управление</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 text-sm">
                {filtered.map((apt) => {
                  const pet = state.pets.find((p) => p.id === apt.petId);
                  const owner = state.owners.find((o) => o.id === pet?.ownerId);
                  const vet = state.veterinarians.find(
                    (v) => v.id === apt.veterinarianId,
                  );
                  return (
                    <tr key={apt.id} className="hover:bg-slate-50">
                      <td className="py-3 px-4 font-mono text-xs tabular-nums whitespace-nowrap">
                        <div className="font-semibold text-slate-900">
                          {apt.timeSlot}
                        </div>
                        <div className="text-slate-500">{apt.date}</div>
                      </td>
                      <td className="py-3 px-4">
                        <div className="flex items-center gap-3">
                          <PetAvatar
                            src={pet?.photoUrl}
                            alt={pet?.name ?? 'Пациент'}
                            size="sm"
                          />
                          <div>
                            <button
                              type="button"
                              onClick={() => pet && onSelectPet(pet.id)}
                              className="font-medium text-slate-900 hover:text-teal-700 text-left"
                            >
                              {pet?.name ?? 'Неизвестно'}
                            </button>
                            <div className="text-xs text-slate-500">
                              {owner?.fullName}
                            </div>
                          </div>
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <div className="text-slate-900">{vet?.fullName}</div>
                        <div className="text-xs text-slate-500">
                          {vet?.cabinet} · {vet?.workDays}
                        </div>
                      </td>
                      <td className="py-3 px-4 text-slate-700 max-w-xs">
                        <div className="text-xs font-medium text-teal-800">
                          {apt.serviceType}
                        </div>
                        <div className="mt-0.5">{apt.reason}</div>
                        {apt.notes && (
                          <div className="text-xs text-slate-500 mt-0.5">
                            {apt.notes}
                          </div>
                        )}
                      </td>
                      <td className="py-3 px-4">
                        <AppointmentStatusLabel status={apt.status} />
                      </td>
                      <td className="py-3 px-4 text-right whitespace-nowrap">
                        <div className="inline-flex items-center gap-2">
                          {apt.status === 'Запланирован' && (
                            <>
                              <button
                                type="button"
                                onClick={() =>
                                  onUpdateStatus(apt.id, 'На приеме')
                                }
                                className="px-2.5 py-1 text-xs font-medium text-teal-700 border border-teal-200 bg-teal-50 rounded-md hover:bg-teal-100 whitespace-nowrap"
                              >
                                Принять
                              </button>
                              <button
                                type="button"
                                onClick={() =>
                                  onUpdateStatus(apt.id, 'Отменен')
                                }
                                className="px-2.5 py-1 text-xs font-medium text-slate-600 border border-slate-200 rounded-md hover:bg-slate-100 whitespace-nowrap"
                              >
                                Отменить
                              </button>
                            </>
                          )}
                          {apt.status === 'На приеме' && (
                            <button
                              type="button"
                              onClick={() =>
                                onUpdateStatus(apt.id, 'Завершен')
                              }
                              className="px-2.5 py-1 text-xs font-medium text-emerald-700 border border-emerald-200 bg-emerald-50 rounded-md hover:bg-emerald-100 whitespace-nowrap"
                            >
                              Завершить
                            </button>
                          )}
                          {pet && (
                            <button
                              type="button"
                              onClick={() => onSelectPet(pet.id)}
                              className="px-2.5 py-1 text-xs font-medium text-slate-700 hover:text-teal-700 whitespace-nowrap"
                            >
                              Карта
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
        )}
      </div>
    </div>
  );
}
