import {
  AlertTriangle,
  CalendarPlus,
  CheckCircle2,
  Clock,
  FlaskConical,
  Pill,
  Receipt,
  Syringe,
} from 'lucide-react';
import {useMemo, useState} from 'react';
import type {
  AppointmentStatus,
  ClinicState,
  InvoiceStatus,
} from '../types/clinic.ts';
import {PetAvatar} from './PetAvatar.tsx';
import {AppointmentStatusLabel, PetHealthLabel} from './StatusText.tsx';

export type OwnerSection =
  | 'pets'
  | 'history'
  | 'preventive'
  | 'appointments'
  | 'billing';

export function OwnerPortalView({
  state,
  activeOwnerId,
  activeSection,
  onChangeOwner,
  onBookAppointment,
  onCancelAppointment,
  onPayInvoice,
}: {
  state: ClinicState;
  activeOwnerId: string;
  activeSection: OwnerSection;
  onChangeOwner: (ownerId: string) => void;
  onBookAppointment: (petId?: string) => void;
  onCancelAppointment: (id: string, status: AppointmentStatus) => void;
  onPayInvoice: (invoiceId: string, status: InvoiceStatus) => Promise<void>;
}) {
  const [petFilter, setPetFilter] = useState<string>('all');

  const owner = useMemo(
    () =>
      state.owners.find((o) => o.id === activeOwnerId) ?? state.owners[0],
    [state.owners, activeOwnerId],
  );

  const myPets = useMemo(
    () => state.pets.filter((p) => p.ownerId === owner?.id),
    [state.pets, owner],
  );

  const myPetIds = useMemo(() => new Set(myPets.map((p) => p.id)), [myPets]);

  const myAppointments = useMemo(
    () => state.appointments.filter((a) => myPetIds.has(a.petId)),
    [state.appointments, myPetIds],
  );

  const myRecords = useMemo(
    () =>
      state.medicalRecords.filter(
        (r) =>
          myPetIds.has(r.petId) &&
          (petFilter === 'all' || r.petId === petFilter),
      ),
    [state.medicalRecords, myPetIds, petFilter],
  );

  const myLabs = useMemo(
    () =>
      state.labTests.filter(
        (l) =>
          myPetIds.has(l.petId) &&
          (petFilter === 'all' || l.petId === petFilter),
      ),
    [state.labTests, myPetIds, petFilter],
  );

  const myVaccinations = useMemo(
    () => state.vaccinations.filter((v) => myPetIds.has(v.petId)),
    [state.vaccinations, myPetIds],
  );

  const myInvoices = useMemo(
    () => state.invoices.filter((i) => i.ownerId === owner?.id),
    [state.invoices, owner],
  );

  if (!owner) {
    return (
      <div className="bg-white border border-slate-200 rounded-xl p-10 text-center text-sm text-slate-600">
        В базе пока нет зарегистрированных владельцев.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <section className="bg-white border border-slate-200 rounded-xl p-6 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="text-xs font-medium text-teal-700">
            Личный кабинет владельца · {owner.loyaltyTier}
          </div>
          <h2 className="text-xl font-semibold text-slate-900 mt-1">
            {owner.fullName}
          </h2>
          <div className="text-xs text-slate-600 mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 tabular-nums">
            <span>{owner.phone}</span>
            <span aria-hidden="true">·</span>
            <span>{owner.email}</span>
            {owner.address && (
              <>
                <span aria-hidden="true">·</span>
                <span>{owner.address}</span>
              </>
            )}
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <div className="flex items-center gap-2">
            <label
              htmlFor="owner-switcher"
              className="text-xs font-medium text-slate-500 whitespace-nowrap"
            >
              Профиль:
            </label>
            <select
              id="owner-switcher"
              value={owner.id}
              onChange={(e) => {
                setPetFilter('all');
                onChangeOwner(e.target.value);
              }}
              className="h-9 px-3 rounded-lg border border-slate-200 bg-white text-xs font-medium text-slate-900 focus:outline-none focus:border-teal-700"
            >
              {state.owners.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.fullName}
                </option>
              ))}
            </select>
          </div>

          <button
            type="button"
            onClick={() => onBookAppointment(myPets[0]?.id)}
            disabled={myPets.length === 0}
            className="inline-flex items-center gap-2 px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 disabled:opacity-50 transition-colors whitespace-nowrap"
          >
            <CalendarPlus size={15} />
            Записаться на прием
          </button>
        </div>
      </section>

      {activeSection === 'pets' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {myPets.length === 0 ? (
            <div className="lg:col-span-2 bg-white border border-slate-200 rounded-xl p-10 text-center text-sm text-slate-600">
              К этому профилю пока не привязаны питомцы. Обратитесь в
              регистратуру клиники.
            </div>
          ) : (
            myPets.map((pet) => (
              <article
                key={pet.id}
                className="bg-white border border-slate-200 rounded-xl p-6 space-y-5"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-center gap-4">
                    <PetAvatar
                      src={pet.photoUrl}
                      alt={pet.name}
                      size="lg"
                    />
                    <div>
                      <div className="flex items-center gap-2.5">
                        <h3 className="text-lg font-semibold text-slate-900">
                          {pet.name}
                        </h3>
                        <PetHealthLabel status={pet.status} />
                      </div>
                      <div className="text-sm text-slate-600 mt-1">
                        {pet.species} · {pet.breed}
                      </div>
                      <div className="text-xs text-slate-500 mt-1">
                        {pet.color} · {pet.gender}
                        {pet.sterilized ? ' · стерилизован' : ''}
                      </div>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={() => onBookAppointment(pet.id)}
                    className="px-3 py-1.5 text-xs font-medium text-teal-700 border border-teal-200 bg-teal-50 rounded-lg hover:bg-teal-100 whitespace-nowrap"
                  >
                    Записать
                  </button>
                </div>

                <div className="grid grid-cols-2 sm:grid-cols-3 gap-4 pt-4 border-t border-slate-100 text-xs">
                  <div>
                    <div className="text-slate-500">Вес</div>
                    <div className="font-mono font-semibold text-slate-900 mt-0.5 tabular-nums">
                      {pet.weightKg} кг
                    </div>
                  </div>
                  <div>
                    <div className="text-slate-500">Группа крови</div>
                    <div className="font-mono text-slate-900 mt-0.5">
                      {pet.bloodType}
                    </div>
                  </div>
                  <div>
                    <div className="text-slate-500">Микрочип</div>
                    <div className="font-mono text-slate-900 mt-0.5 tabular-nums truncate">
                      {pet.microchipNumber}
                    </div>
                  </div>
                  <div className="col-span-2 sm:col-span-3">
                    <div className="text-slate-500">Аллергии</div>
                    <div className="text-slate-900 font-medium mt-0.5">
                      {pet.allergies}
                    </div>
                  </div>
                  <div className="col-span-2 sm:col-span-3">
                    <div className="text-slate-500">Рекомендованный рацион</div>
                    <div className="text-slate-900 mt-0.5">{pet.diet}</div>
                  </div>
                </div>
              </article>
            ))
          )}
        </div>
      )}

      {activeSection === 'history' && (
        <div className="space-y-5">
          {myPets.length > 1 && (
            <div className="flex items-center gap-1 p-1 bg-slate-200/70 rounded-lg w-fit">
              <button
                type="button"
                onClick={() => setPetFilter('all')}
                className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                  petFilter === 'all'
                    ? 'bg-white text-slate-900 shadow-sm'
                    : 'text-slate-600'
                }`}
              >
                Все питомцы
              </button>
              {myPets.map((p) => (
                <button
                  key={p.id}
                  type="button"
                  onClick={() => setPetFilter(p.id)}
                  className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors ${
                    petFilter === p.id
                      ? 'bg-white text-slate-900 shadow-sm'
                      : 'text-slate-600'
                  }`}
                >
                  {p.name}
                </button>
              ))}
            </div>
          )}

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 items-start">
            <div className="lg:col-span-2 space-y-4">
              <h3 className="text-sm font-semibold text-slate-900">
                Протоколы осмотров ({myRecords.length})
              </h3>
              {myRecords.length === 0 ? (
                <div className="bg-white border border-slate-200 rounded-xl p-8 text-center text-sm text-slate-500">
                  Обращений и записей в истории болезни пока нет.
                </div>
              ) : (
                myRecords.map((rec) => {
                  const pet = myPets.find((p) => p.id === rec.petId);
                  const vet = state.veterinarians.find(
                    (v) => v.id === rec.veterinarianId,
                  );
                  return (
                    <article
                      key={rec.id}
                      className="bg-white border border-slate-200 rounded-xl p-5 space-y-3"
                    >
                      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-3">
                        <div>
                          <div className="text-xs text-slate-500 tabular-nums">
                            {rec.visitDate} · {pet?.name} · {vet?.fullName}
                          </div>
                          <h4 className="text-base font-semibold text-slate-900 mt-0.5">
                            {rec.diagnosis}
                          </h4>
                        </div>
                        <div className="text-xs font-mono text-slate-600 tabular-nums">
                          T {rec.temperatureC.toFixed(1)} °C · Вес {rec.weightKg} кг
                        </div>
                      </div>
                      <div className="text-xs text-slate-600 leading-relaxed">
                        <strong className="text-slate-800">Рекомендации: </strong>
                        {rec.treatmentPlan}
                      </div>
                      {rec.prescriptions.length > 0 && (
                        <div className="pt-2 border-t border-slate-100 space-y-1">
                          {rec.prescriptions.map((rx, idx) => (
                            <div
                              key={`${rec.id}-rx-${idx}`}
                              className="text-xs text-slate-700"
                            >
                              • <strong>{rx.medicationName}</strong>:{' '}
                              {rx.dosage}, {rx.frequency} ({rx.durationDays} дн.)
                            </div>
                          ))}
                        </div>
                      )}
                    </article>
                  );
                })
              )}
            </div>

            <div className="space-y-4">
              <div className="flex items-center gap-2 text-sm font-semibold text-slate-900">
                <FlaskConical size={16} className="text-teal-700" />
                Результаты анализов ({myLabs.length})
              </div>
              {myLabs.length === 0 ? (
                <div className="bg-white border border-slate-200 rounded-xl p-6 text-xs text-slate-500">
                  Лабораторные исследования не назначались.
                </div>
              ) : (
                <div className="space-y-3">
                  {myLabs.map((lab) => {
                    const pet = myPets.find((p) => p.id === lab.petId);
                    return (
                      <div
                        key={lab.id}
                        className="bg-white border border-slate-200 rounded-xl p-4 space-y-2"
                      >
                        <div className="flex items-center justify-between text-xs">
                          <span className="font-semibold text-slate-900">
                            {pet?.name}
                          </span>
                          <span className="font-mono text-slate-500 tabular-nums">
                            {lab.orderedDate} · {lab.status}
                          </span>
                        </div>
                        <div className="text-xs font-medium text-slate-800">
                          {lab.testType}
                        </div>
                        <div className="text-xs text-slate-600 leading-relaxed">
                          {lab.resultSummary}
                        </div>
                        {lab.isAbnormal && (
                          <div className="inline-flex items-center gap-1 text-xs font-medium text-red-700">
                            <AlertTriangle size={12} />
                            Требуется консультация врача
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {activeSection === 'preventive' && (
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Syringe size={16} className="text-teal-700" />
              <h3 className="text-base font-semibold text-slate-900">
                Календарь прививок и обработок
              </h3>
            </div>
            <button
              type="button"
              onClick={() => onBookAppointment(myPets[0]?.id)}
              className="text-xs font-medium text-teal-700 hover:underline"
            >
              Записаться на плановую вакцинацию
            </button>
          </div>
          {myVaccinations.length === 0 ? (
            <div className="p-8 text-center text-sm text-slate-500">
              Записи о прививках и профилактических обработках отсутствуют.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                    <th className="py-3 px-4">Питомец</th>
                    <th className="py-3 px-4">Процедура</th>
                    <th className="py-3 px-4">Препарат и серия</th>
                    <th className="py-3 px-4">Выполнено</th>
                    <th className="py-3 px-4">Следующая дата</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200 text-sm">
                  {myVaccinations.map((vac) => {
                    const pet = myPets.find((p) => p.id === vac.petId);
                    const daysLeft = Math.ceil(
                      (new Date(vac.nextDueDate).getTime() -
                        new Date('2026-11-22').getTime()) /
                        86400000,
                    );
                    const dueLabel =
                      daysLeft <= 0
                        ? 'Требуется сейчас'
                        : daysLeft <= 30
                          ? `Через ${daysLeft} дн.`
                          : 'Актуально';
                    return (
                      <tr key={vac.id} className="hover:bg-slate-50">
                        <td className="py-3 px-4 font-medium text-slate-900">
                          {pet?.name}
                        </td>
                        <td className="py-3 px-4 text-xs text-slate-600">
                          {vac.kind}
                        </td>
                        <td className="py-3 px-4">
                          <div className="text-slate-900">
                            {vac.vaccineName}
                          </div>
                          <div className="text-xs font-mono text-slate-500">
                            {vac.batchNumber}
                          </div>
                        </td>
                        <td className="py-3 px-4 font-mono text-xs text-slate-600 tabular-nums">
                          {vac.administeredDate}
                        </td>
                        <td className="py-3 px-4">
                          <div className="font-mono text-xs font-semibold text-slate-900 tabular-nums">
                            {vac.nextDueDate}
                          </div>
                          <div
                            className={`text-xs font-medium mt-0.5 ${
                              daysLeft <= 0
                                ? 'text-red-700'
                                : daysLeft <= 30
                                  ? 'text-amber-700'
                                  : 'text-emerald-700'
                            }`}
                          >
                            {dueLabel}
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
      )}

      {activeSection === 'appointments' && (
        <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
            <h3 className="text-base font-semibold text-slate-900">
              Мои записи в клинику
            </h3>
            <button
              type="button"
              onClick={() => onBookAppointment(myPets[0]?.id)}
              disabled={myPets.length === 0}
              className="text-xs font-medium text-teal-700 hover:underline"
            >
              + Новая запись
            </button>
          </div>
          {myAppointments.length === 0 ? (
            <div className="p-10 text-center">
              <p className="text-sm text-slate-600 mb-4">
                У вас пока нет запланированных визитов.
              </p>
              <button
                type="button"
                onClick={() => onBookAppointment(myPets[0]?.id)}
                className="px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800"
              >
                Записаться онлайн
              </button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                    <th className="py-3 px-4">Дата и время</th>
                    <th className="py-3 px-4">Питомец</th>
                    <th className="py-3 px-4">Врач и кабинет</th>
                    <th className="py-3 px-4">Услуга и повод</th>
                    <th className="py-3 px-4">Статус</th>
                    <th className="py-3 px-4 text-right">Действие</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200 text-sm">
                  {myAppointments.map((apt) => {
                    const pet = myPets.find((p) => p.id === apt.petId);
                    const vet = state.veterinarians.find(
                      (v) => v.id === apt.veterinarianId,
                    );
                    return (
                      <tr key={apt.id} className="hover:bg-slate-50">
                        <td className="py-3 px-4 font-mono text-xs tabular-nums whitespace-nowrap">
                          {apt.date} · {apt.timeSlot}
                        </td>
                        <td className="py-3 px-4 font-medium text-slate-900">
                          {pet?.name}
                        </td>
                        <td className="py-3 px-4">
                          <div className="text-slate-900">{vet?.fullName}</div>
                          <div className="text-xs text-slate-500">
                            {vet?.cabinet}
                          </div>
                        </td>
                        <td className="py-3 px-4 text-slate-600">
                          <div className="text-xs font-medium text-teal-800">
                            {apt.serviceType}
                          </div>
                          <div>{apt.reason}</div>
                        </td>
                        <td className="py-3 px-4">
                          <AppointmentStatusLabel status={apt.status} />
                        </td>
                        <td className="py-3 px-4 text-right whitespace-nowrap">
                          {apt.status === 'Запланирован' && (
                            <button
                              type="button"
                              onClick={() =>
                                onCancelAppointment(apt.id, 'Отменен')
                              }
                              className="text-xs font-medium text-red-700 hover:underline"
                            >
                              Отменить запись
                            </button>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {activeSection === 'billing' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 items-start">
          <section className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            <div className="px-5 py-4 border-b border-slate-200 flex items-center gap-2">
              <Pill size={16} className="text-teal-700" />
              <h3 className="text-sm font-semibold text-slate-900">
                Активные назначения на дом
              </h3>
            </div>
            <div className="divide-y divide-slate-200">
              {myRecords.flatMap((rec) =>
                rec.prescriptions.map((rx, idx) => {
                  const pet = myPets.find((p) => p.id === rec.petId);
                  return (
                    <div
                      key={`${rec.id}-home-${idx}`}
                      className="p-4 space-y-1"
                    >
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-semibold text-slate-900">
                          {rx.medicationName}
                        </span>
                        <span className="text-xs font-medium text-teal-700">
                          {pet?.name}
                        </span>
                      </div>
                      <div className="text-xs text-slate-600">
                        Дозировка: {rx.dosage} · {rx.frequency} · курс{' '}
                        {rx.durationDays} дн.
                      </div>
                      <div className="text-xs text-slate-400 tabular-nums">
                        Назначено {rec.visitDate} · Диагноз: {rec.diagnosis}
                      </div>
                    </div>
                  );
                }),
              )}
              {myRecords.every((r) => r.prescriptions.length === 0) && (
                <div className="p-6 text-xs text-slate-500">
                  Активных рецептурных назначений сейчас нет.
                </div>
              )}
            </div>
          </section>

          <section className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            <div className="px-5 py-4 border-b border-slate-200 flex items-center gap-2">
              <Receipt size={16} className="text-teal-700" />
              <h3 className="text-sm font-semibold text-slate-900">
                Счета и история оплат
              </h3>
            </div>
            {myInvoices.length === 0 ? (
              <div className="p-6 text-xs text-slate-500">
                Выставленных счетов пока нет.
              </div>
            ) : (
              <div className="divide-y divide-slate-200">
                {myInvoices.map((inv) => {
                  const pet = myPets.find((p) => p.id === inv.petId);
                  return (
                    <div
                      key={inv.id}
                      className="p-4 flex items-center justify-between gap-4"
                    >
                      <div className="min-w-0">
                        <div className="text-xs text-slate-500 tabular-nums">
                          {inv.date} · {pet?.name}
                        </div>
                        <div className="text-sm font-medium text-slate-900 mt-0.5">
                          {inv.title}
                        </div>
                        <div className="mt-1">
                          {inv.status === 'Оплачен' ? (
                            <span className="inline-flex items-center gap-1 text-xs font-medium text-emerald-700">
                              <CheckCircle2 size={12} />
                              Оплачен
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1 text-xs font-medium text-amber-700">
                              <Clock size={12} />
                              Ожидает оплаты
                            </span>
                          )}
                        </div>
                      </div>
                      <div className="text-right shrink-0 space-y-1.5">
                        <div className="font-mono text-sm font-semibold text-slate-900 tabular-nums">
                          {inv.amountRub.toLocaleString('ru-RU')} ₽
                        </div>
                        {inv.status === 'Ожидает оплаты' && (
                          <button
                            type="button"
                            onClick={() => onPayInvoice(inv.id, 'Оплачен')}
                            className="px-3 py-1 text-xs font-medium text-white bg-teal-700 rounded-md hover:bg-teal-800 whitespace-nowrap"
                          >
                            Оплатить
                          </button>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </section>
        </div>
      )}
    </div>
  );
}
