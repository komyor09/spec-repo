import {
  AlertTriangle,
  ArrowRight,
  BedDouble,
  Calendar,
  FlaskConical,
  Package,
} from 'lucide-react';
import type {AppointmentStatus, ClinicState} from '../types/clinic.ts';
import {PetAvatar} from './PetAvatar.tsx';
import {AppointmentStatusLabel} from './StatusText.tsx';

export type DoctorTab =
  | 'overview'
  | 'patients'
  | 'appointments'
  | 'inpatient'
  | 'lab'
  | 'pharmacy'
  | 'owners';

export function OverviewView({
  state,
  onSelectPet,
  onNavigate,
  onUpdateAppointmentStatus,
  onOpenNewAppointment,
}: {
  state: ClinicState;
  onSelectPet: (petId: string) => void;
  onNavigate: (tab: DoctorTab) => void;
  onUpdateAppointmentStatus: (id: string, status: AppointmentStatus) => void;
  onOpenNewAppointment: () => void;
}) {
  const activeAppointments = state.appointments.filter(
    (a) => a.status === 'Запланирован' || a.status === 'На приеме',
  );
  const lowStockMeds = state.medications.filter(
    (m) => m.stockQty <= m.minThreshold,
  );
  const inpatientPets = state.pets.filter(
    (p) => p.status === 'Стационар' || p.status === 'Наблюдение',
  );
  const pendingLabs = state.labTests.filter((l) => l.status !== 'Готово');

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <button
          type="button"
          onClick={() => onNavigate('appointments')}
          className="text-left bg-white border border-slate-200 rounded-xl p-5 hover:border-teal-300 transition-colors"
        >
          <div className="text-xs font-medium text-slate-500">
            Активные записи
          </div>
          <div className="mt-2 flex items-baseline justify-between">
            <span className="text-2xl font-semibold text-slate-900 tabular-nums">
              {activeAppointments.length}
            </span>
            <span className="text-xs text-slate-500 tabular-nums">
              всего {state.appointments.length}
            </span>
          </div>
        </button>

        <button
          type="button"
          onClick={() => onNavigate('inpatient')}
          className="text-left bg-white border border-slate-200 rounded-xl p-5 hover:border-teal-300 transition-colors"
        >
          <div className="text-xs font-medium text-slate-500">
            Стационар и наблюдение
          </div>
          <div className="mt-2 flex items-baseline justify-between">
            <span className="text-2xl font-semibold text-slate-900 tabular-nums">
              {inpatientPets.length}
            </span>
            <span className="text-xs text-slate-500 tabular-nums">
              всего пациентов: {state.pets.length}
            </span>
          </div>
        </button>

        <button
          type="button"
          onClick={() => onNavigate('lab')}
          className="text-left bg-white border border-slate-200 rounded-xl p-5 hover:border-teal-300 transition-colors"
        >
          <div className="text-xs font-medium text-slate-500">
            Анализы в работе
          </div>
          <div className="mt-2 flex items-baseline justify-between">
            <span className="text-2xl font-semibold text-slate-900 tabular-nums">
              {pendingLabs.length}
            </span>
            <span className="text-xs text-slate-500 tabular-nums">
              выполнено: {state.labTests.length - pendingLabs.length}
            </span>
          </div>
        </button>

        <button
          type="button"
          onClick={() => onNavigate('pharmacy')}
          className="text-left bg-white border border-slate-200 rounded-xl p-5 hover:border-teal-300 transition-colors"
        >
          <div className="text-xs font-medium text-slate-500">
            Препараты ниже порога
          </div>
          <div className="mt-2 flex items-baseline justify-between">
            <span
              className={`text-2xl font-semibold tabular-nums ${
                lowStockMeds.length > 0 ? 'text-amber-700' : 'text-slate-900'
              }`}
            >
              {lowStockMeds.length}
            </span>
            <span className="text-xs text-slate-500 tabular-nums">
              в аптеке: {state.medications.length}
            </span>
          </div>
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
        <div className="lg:col-span-2 space-y-6">
          <section className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-200 flex items-center justify-between gap-4">
              <div>
                <h2 className="text-base font-semibold text-slate-900">
                  Текущая очередь приемов
                </h2>
                <p className="text-xs text-slate-500 mt-0.5">
                  Оперативное управление визитами и переход к электронной карте
                </p>
              </div>
              <button
                type="button"
                onClick={() => onNavigate('appointments')}
                className="inline-flex items-center gap-1.5 text-xs font-medium text-teal-700 hover:text-teal-800 whitespace-nowrap shrink-0"
              >
                Все записи
                <ArrowRight size={14} />
              </button>
            </div>

            {state.appointments.length === 0 ? (
              <div className="p-8 text-center">
                <Calendar className="mx-auto text-slate-400 mb-3" size={28} />
                <p className="text-sm text-slate-600 mb-4">
                  На ближайшее время записей пока нет.
                </p>
                <button
                  type="button"
                  onClick={onOpenNewAppointment}
                  className="px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800"
                >
                  Создать первую запись
                </button>
              </div>
            ) : (
              <div className="divide-y divide-slate-200">
                {state.appointments.slice(0, 6).map((apt) => {
                  const pet = state.pets.find((p) => p.id === apt.petId);
                  const owner = state.owners.find(
                    (o) => o.id === pet?.ownerId,
                  );
                  const vet = state.veterinarians.find(
                    (v) => v.id === apt.veterinarianId,
                  );
                  return (
                    <div
                      key={apt.id}
                      className="px-6 py-3.5 flex flex-wrap sm:flex-nowrap items-center justify-between gap-4 hover:bg-slate-50 transition-colors"
                    >
                      <div className="flex items-center gap-3.5 min-w-0">
                        <div className="text-xs font-mono text-slate-600 tabular-nums w-24 shrink-0">
                          <div>{apt.date}</div>
                          <div className="font-semibold text-slate-900">
                            {apt.timeSlot}
                          </div>
                        </div>
                        <PetAvatar
                          src={pet?.photoUrl}
                          alt={pet?.name ?? 'Пациент'}
                          size="sm"
                        />
                        <div className="min-w-0">
                          <button
                            type="button"
                            onClick={() => pet && onSelectPet(pet.id)}
                            className="text-sm font-medium text-slate-900 hover:text-teal-700 truncate block text-left"
                          >
                            {pet?.name ?? 'Пациент'} · {apt.serviceType}
                          </button>
                          <div className="text-xs text-slate-500 truncate">
                            {owner?.fullName} · {vet?.fullName} · {apt.reason}
                          </div>
                        </div>
                      </div>

                      <div className="flex items-center gap-3 shrink-0">
                        <AppointmentStatusLabel status={apt.status} />
                        {apt.status === 'Запланирован' && (
                          <button
                            type="button"
                            onClick={() =>
                              onUpdateAppointmentStatus(apt.id, 'На приеме')
                            }
                            className="px-3 py-1.5 text-xs font-medium text-teal-700 border border-teal-200 bg-teal-50 rounded-lg hover:bg-teal-100 transition-colors whitespace-nowrap"
                          >
                            Начать прием
                          </button>
                        )}
                        {apt.status === 'На приеме' && (
                          <button
                            type="button"
                            onClick={() =>
                              onUpdateAppointmentStatus(apt.id, 'Завершен')
                            }
                            className="px-3 py-1.5 text-xs font-medium text-emerald-700 border border-emerald-200 bg-emerald-50 rounded-lg hover:bg-emerald-100 transition-colors whitespace-nowrap"
                          >
                            Завершить
                          </button>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </section>

          <section className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-slate-900">
                Специалисты на смене
              </h2>
              <span className="text-xs text-slate-500 tabular-nums">
                {state.veterinarians.length} врача
              </span>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 divide-y sm:divide-y-0 sm:gap-px bg-slate-200">
              {state.veterinarians.map((vet) => (
                <div
                  key={vet.id}
                  className="bg-white p-4 flex items-center gap-3.5"
                >
                  <PetAvatar
                    src={vet.avatarUrl}
                    alt={vet.fullName}
                    size="md"
                    variant="doctor"
                  />
                  <div className="min-w-0">
                    <div className="text-sm font-medium text-slate-900 truncate">
                      {vet.fullName}
                    </div>
                    <div className="text-xs text-slate-500 truncate">
                      {vet.specialization}
                    </div>
                    <div className="text-xs text-slate-500 mt-0.5 tabular-nums">
                      {vet.cabinet} · стаж {vet.experienceYears} лет
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </section>
        </div>

        <div className="space-y-6">
          <section className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            <div className="px-5 py-4 border-b border-slate-200 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <BedDouble size={16} className="text-slate-600" />
                <h2 className="text-sm font-semibold text-slate-900">
                  Лист стационара
                </h2>
              </div>
              <button
                type="button"
                onClick={() => onNavigate('inpatient')}
                className="text-xs font-medium text-teal-700 hover:text-teal-800 whitespace-nowrap"
              >
                Открыть лист
              </button>
            </div>
            <div className="divide-y divide-slate-200">
              {inpatientPets.slice(0, 4).map((pet) => (
                <div
                  key={pet.id}
                  className="px-5 py-3 flex items-center justify-between gap-3"
                >
                  <div className="min-w-0">
                    <button
                      type="button"
                      onClick={() => onSelectPet(pet.id)}
                      className="text-sm font-medium text-slate-900 hover:text-teal-700 truncate block text-left"
                    >
                      {pet.name}
                    </button>
                    <div className="text-xs text-slate-500 truncate">
                      {pet.species} · {pet.breed}
                    </div>
                  </div>
                  <span className="text-xs font-mono text-slate-700 shrink-0">
                    {pet.cageNumber || pet.status}
                  </span>
                </div>
              ))}
            </div>
          </section>

          <section className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            <div className="px-5 py-4 border-b border-slate-200 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <FlaskConical size={16} className="text-slate-600" />
                <h2 className="text-sm font-semibold text-slate-900">
                  Лаборатория
                </h2>
              </div>
              <button
                type="button"
                onClick={() => onNavigate('lab')}
                className="text-xs font-medium text-teal-700 hover:text-teal-800 whitespace-nowrap"
              >
                Все анализы
              </button>
            </div>
            <div className="divide-y divide-slate-200">
              {state.labTests.slice(0, 3).map((lab) => {
                const pet = state.pets.find((p) => p.id === lab.petId);
                return (
                  <div key={lab.id} className="px-5 py-3 space-y-1">
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-xs font-semibold text-slate-900">
                        {pet?.name}
                      </span>
                      <span className="text-xs text-slate-500">
                        {lab.status}
                      </span>
                    </div>
                    <div className="text-xs text-slate-600 truncate">
                      {lab.testType}
                    </div>
                  </div>
                );
              })}
            </div>
          </section>

          <section className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            <div className="px-5 py-4 border-b border-slate-200 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Package size={16} className="text-slate-600" />
                <h2 className="text-sm font-semibold text-slate-900">
                  Контроль аптеки
                </h2>
              </div>
              <button
                type="button"
                onClick={() => onNavigate('pharmacy')}
                className="text-xs font-medium text-teal-700 hover:text-teal-800 whitespace-nowrap"
              >
                Склад
              </button>
            </div>
            {lowStockMeds.length === 0 ? (
              <div className="p-5 text-xs text-slate-500">
                Все препараты в достаточном количестве.
              </div>
            ) : (
              <div className="divide-y divide-slate-200">
                {lowStockMeds.map((med) => (
                  <div
                    key={med.id}
                    className="px-5 py-3 flex items-center justify-between gap-3"
                  >
                    <div className="min-w-0">
                      <div className="text-sm font-medium text-slate-900 truncate">
                        {med.name}
                      </div>
                      <div className="text-xs text-slate-500 truncate">
                        {med.category} · {med.manufacturer}
                      </div>
                    </div>
                    <div className="text-right shrink-0">
                      <div className="inline-flex items-center gap-1 text-xs font-semibold text-amber-700 tabular-nums">
                        <AlertTriangle size={12} />
                        {med.stockQty} {med.unit}
                      </div>
                      <div className="text-xs text-slate-400 tabular-nums">
                        мин. {med.minThreshold}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </section>
        </div>
      </div>
    </div>
  );
}
