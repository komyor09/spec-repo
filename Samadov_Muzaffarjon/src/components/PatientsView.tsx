import {
  AlertTriangle,
  CalendarPlus,
  ChevronLeft,
  Download,
  FilePlus2,
  Pencil,
  Plus,
  Printer,
  Search,
  Syringe,
  Trash2,
} from 'lucide-react';
import {useMemo, useState, type FormEvent} from 'react';
import type {
  ClinicState,
  Pet,
  PetSpecies,
  PrescriptionItem,
  Vaccination,
} from '../types/clinic.ts';
import {PetAvatar} from './PetAvatar.tsx';
import {AppointmentStatusLabel, PetHealthLabel} from './StatusText.tsx';

function formatAge(birthDate: string): string {
  const born = new Date(birthDate);
  if (Number.isNaN(born.getTime())) return '—';
  const now = new Date('2026-11-22');
  const months =
    (now.getFullYear() - born.getFullYear()) * 12 +
    (now.getMonth() - born.getMonth());
  if (months < 12) return `${Math.max(1, months)} мес.`;
  const years = Math.floor(months / 12);
  const mod = years % 10;
  const word =
    years >= 11 && years <= 14
      ? 'лет'
      : mod === 1
        ? 'год'
        : mod >= 2 && mod <= 4
          ? 'года'
          : 'лет';
  return `${years} ${word}`;
}

function downloadMedicalSummary(
  pet: Pet,
  state: ClinicState,
  ownerName: string,
) {
  const records = state.medicalRecords.filter((r) => r.petId === pet.id);
  const vaccines = state.vaccinations.filter((v) => v.petId === pet.id);
  const lines = [
    `ВЕТЕРИНАРНАЯ КЛИНИКА «ВЕТМЕД» — ВЫПИСКА ИЗ ЭЛЕКТРОННОЙ КАРТЫ`,
    `Дата выписки: 2026-11-22`,
    `================================================================`,
    `Пациент: ${pet.name} (${pet.species}, ${pet.breed})`,
    `Пол: ${pet.gender}${pet.sterilized ? ' (стерилизован)' : ''} | Окрас: ${pet.color}`,
    `Дата рождения: ${pet.birthDate} | Вес: ${pet.weightKg} кг | Группа крови: ${pet.bloodType}`,
    `Микрочип: ${pet.microchipNumber}`,
    `Аллергии: ${pet.allergies}`,
    `Рацион: ${pet.diet}`,
    `Владелец: ${ownerName}`,
    ``,
    `ИСТОРИЯ БОЛЕЗНИ И ОСМОТРЫ (${records.length}):`,
    ...records.flatMap((r) => {
      const vet = state.veterinarians.find((v) => v.id === r.veterinarianId);
      const rxList = r.prescriptions.map(
        (rx) =>
          `   - ${rx.medicationName}: ${rx.dosage}, ${rx.frequency} (${rx.durationDays} дн.)`,
      );
      return [
        `----------------------------------------------------------------`,
        `Дата: ${r.visitDate} | Врач: ${vet?.fullName ?? 'Врач клиники'}`,
        `Диагноз: ${r.diagnosis}`,
        `Показатели: T ${r.temperatureC.toFixed(1)} °C | Вес ${r.weightKg} кг | ЧСС ${r.heartRateBpm} уд/мин`,
        `Жалобы: ${r.complaints}`,
        `План лечения: ${r.treatmentPlan}`,
        ...(rxList.length > 0 ? [`Назначения:`, ...rxList] : []),
      ];
    }),
    ``,
    `ВАКЦИНАЦИЯ И ПРОФИЛАКТИКА (${vaccines.length}):`,
    ...vaccines.map(
      (v) =>
        `- [${v.kind}] ${v.vaccineName} (серия ${v.batchNumber}) — выполнена ${v.administeredDate}, следующая ${v.nextDueDate}`,
    ),
  ];

  const blob = new Blob([lines.join('\n')], {
    type: 'text/plain;charset=utf-8',
  });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `extract-${pet.name}-${pet.microchipNumber}.txt`;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}

const SPECIES_TABS: ('Все' | PetSpecies)[] = [
  'Все',
  'Собака',
  'Кошка',
  'Грызун и заяц',
  'Птица',
  'Рептилия',
];

export function PatientsView({
  state,
  selectedPetId,
  onSelectPet,
  onOpenNewPatient,
  onOpenNewAppointment,
  onAddMedicalRecord,
  onAddVaccination,
  onUpdatePetDetails,
  onPrintExtract,
}: {
  state: ClinicState;
  selectedPetId: string | null;
  onSelectPet: (id: string | null) => void;
  onOpenNewPatient: () => void;
  onOpenNewAppointment: (petId?: string) => void;
  onAddMedicalRecord: (payload: {
    petId: string;
    veterinarianId: string;
    complaints: string;
    diagnosis: string;
    temperatureC: number;
    weightKg: number;
    heartRateBpm: number;
    treatmentPlan: string;
    prescriptions: PrescriptionItem[];
    createInvoice?: boolean;
    consultationFeeRub?: number;
  }) => Promise<string | null>;
  onAddVaccination: (payload: {
    petId: string;
    kind: Vaccination['kind'];
    vaccineName: string;
    administeredDate: string;
    nextDueDate: string;
    batchNumber: string;
    veterinarianId: string;
  }) => Promise<string | null>;
  onUpdatePetDetails: (
    petId: string,
    payload: Partial<
      Pick<Pet, 'status' | 'weightKg' | 'allergies' | 'diet' | 'cageNumber'>
    >,
  ) => Promise<void>;
  onPrintExtract: (petId: string) => void;
}) {
  const [search, setSearch] = useState('');
  const [speciesFilter, setSpeciesFilter] = useState<'Все' | PetSpecies>('Все');
  const [detailTab, setDetailTab] = useState<
    'history' | 'vaccines' | 'labs' | 'visits'
  >('history');
  const [showRecordForm, setShowRecordForm] = useState(false);
  const [showVaccineForm, setShowVaccineForm] = useState(false);
  const [editingProfile, setEditingProfile] = useState(false);
  const [draftWeight, setDraftWeight] = useState('');
  const [draftAllergies, setDraftAllergies] = useState('');
  const [draftDiet, setDraftDiet] = useState('');
  const [draftCage, setDraftCage] = useState('');

  const selectedPet = useMemo(
    () => state.pets.find((p) => p.id === selectedPetId) ?? null,
    [state.pets, selectedPetId],
  );

  const filteredPets = useMemo(() => {
    const q = search.trim().toLowerCase();
    return state.pets.filter((pet) => {
      if (speciesFilter !== 'Все' && pet.species !== speciesFilter) return false;
      if (!q) return true;
      const owner = state.owners.find((o) => o.id === pet.ownerId);
      return (
        pet.name.toLowerCase().includes(q) ||
        pet.breed.toLowerCase().includes(q) ||
        pet.microchipNumber.includes(q) ||
        (owner?.fullName.toLowerCase().includes(q) ?? false)
      );
    });
  }, [state.pets, state.owners, search, speciesFilter]);

  if (selectedPet) {
    const owner = state.owners.find((o) => o.id === selectedPet.ownerId);
    const records = state.medicalRecords.filter(
      (r) => r.petId === selectedPet.id,
    );
    const vaccines = state.vaccinations.filter(
      (v) => v.petId === selectedPet.id,
    );
    const labs = state.labTests.filter((l) => l.petId === selectedPet.id);
    const visits = state.appointments.filter((a) => a.petId === selectedPet.id);

    const startProfileEdit = () => {
      setDraftWeight(String(selectedPet.weightKg));
      setDraftAllergies(selectedPet.allergies);
      setDraftDiet(selectedPet.diet);
      setDraftCage(selectedPet.cageNumber ?? '');
      setEditingProfile(true);
    };

    const saveProfileEdit = async (e: FormEvent) => {
      e.preventDefault();
      await onUpdatePetDetails(selectedPet.id, {
        weightKg: Number(draftWeight) || selectedPet.weightKg,
        allergies: draftAllergies.trim() || 'Не выявлены',
        diet: draftDiet.trim() || selectedPet.diet,
        cageNumber: draftCage.trim(),
      });
      setEditingProfile(false);
    };

    return (
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <button
            type="button"
            onClick={() => {
              setShowRecordForm(false);
              setShowVaccineForm(false);
              setEditingProfile(false);
              onSelectPet(null);
            }}
            className="inline-flex items-center gap-1.5 text-sm font-medium text-slate-600 hover:text-slate-900 transition-colors"
          >
            <ChevronLeft size={16} />
            Вернуться к списку пациентов
          </button>

          <div className="flex flex-wrap items-center gap-2.5">
            <button
              type="button"
              onClick={() => onPrintExtract(selectedPet.id)}
              className="inline-flex items-center gap-2 px-3.5 py-2 text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors whitespace-nowrap"
            >
              <Printer size={14} />
              Печать / PDF
            </button>
            <button
              type="button"
              onClick={() =>
                downloadMedicalSummary(
                  selectedPet,
                  state,
                  owner?.fullName ?? 'Не указан',
                )
              }
              className="inline-flex items-center gap-2 px-3.5 py-2 text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors whitespace-nowrap"
            >
              <Download size={14} />
              Выписка (.txt)
            </button>
            <button
              type="button"
              onClick={() => onOpenNewAppointment(selectedPet.id)}
              className="inline-flex items-center gap-2 px-3.5 py-2 text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors whitespace-nowrap"
            >
              <CalendarPlus size={15} />
              Записать на прием
            </button>
            <button
              type="button"
              onClick={() => {
                setDetailTab('history');
                setShowRecordForm((v) => !v);
              }}
              className="inline-flex items-center gap-2 px-3.5 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 transition-colors whitespace-nowrap"
            >
              <FilePlus2 size={15} />
              Добавить осмотр
            </button>
          </div>
        </div>

        <section className="bg-white border border-slate-200 rounded-xl p-6 space-y-5">
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
            <div className="flex items-center gap-5">
              <PetAvatar
                src={selectedPet.photoUrl}
                alt={selectedPet.name}
                size="lg"
              />
              <div>
                <div className="flex flex-wrap items-center gap-3">
                  <h2 className="text-xl font-semibold text-slate-900">
                    {selectedPet.name}
                  </h2>
                  <PetHealthLabel status={selectedPet.status} />
                  {selectedPet.cageNumber && (
                    <span className="text-xs font-mono text-slate-700">
                      · {selectedPet.cageNumber}
                    </span>
                  )}
                </div>
                <div className="mt-1.5 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-slate-600">
                  <span>{selectedPet.species}</span>
                  <span aria-hidden="true">·</span>
                  <span>{selectedPet.breed}</span>
                  <span aria-hidden="true">·</span>
                  <span>{selectedPet.color}</span>
                  <span aria-hidden="true">·</span>
                  <span>
                    {selectedPet.gender}
                    {selectedPet.sterilized ? ' (стерилизован)' : ''}
                  </span>
                  <span aria-hidden="true">·</span>
                  <span className="tabular-nums">
                    {formatAge(selectedPet.birthDate)}
                  </span>
                  <span aria-hidden="true">·</span>
                  <span className="tabular-nums font-medium text-slate-900">
                    {selectedPet.weightKg} кг
                  </span>
                </div>
                <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-500">
                  <span>
                    Микрочип:{' '}
                    <span className="font-mono text-slate-700 tabular-nums">
                      {selectedPet.microchipNumber}
                    </span>
                  </span>
                  <span>
                    Группа крови:{' '}
                    <span className="font-mono text-slate-700">
                      {selectedPet.bloodType}
                    </span>
                  </span>
                  {selectedPet.insurancePolicy && (
                    <span>
                      Полис:{' '}
                      <span className="font-mono text-slate-700">
                        {selectedPet.insurancePolicy}
                      </span>
                    </span>
                  )}
                </div>
              </div>
            </div>

            <div className="flex flex-col sm:items-end gap-2 border-t md:border-t-0 pt-4 md:pt-0 border-slate-200">
              <div className="text-xs text-slate-500">
                Владелец · {owner?.loyaltyTier ?? 'Стандарт'}
              </div>
              <div className="text-sm font-medium text-slate-900">
                {owner?.fullName ?? 'Не указан'}
              </div>
              <div className="text-xs text-slate-600 tabular-nums">
                {owner?.phone} · {owner?.email}
              </div>
              <div className="mt-1 flex items-center gap-2">
                <select
                  value={selectedPet.status}
                  onChange={(e) =>
                    onUpdatePetDetails(selectedPet.id, {
                      status: e.target.value as Pet['status'],
                    })
                  }
                  className="h-8 px-2.5 text-xs rounded-lg border border-slate-200 bg-white text-slate-900 focus:outline-none focus:border-teal-700"
                >
                  <option value="Здоров">Здоров</option>
                  <option value="Наблюдение">Наблюдение</option>
                  <option value="Стационар">Стационар</option>
                </select>
                <button
                  type="button"
                  onClick={() =>
                    editingProfile ? setEditingProfile(false) : startProfileEdit()
                  }
                  className="h-8 px-2.5 inline-flex items-center gap-1 text-xs font-medium text-slate-700 border border-slate-200 rounded-lg hover:bg-slate-50"
                >
                  <Pencil size={12} />
                  {editingProfile ? 'Закрыть' : 'Изменить'}
                </button>
              </div>
            </div>
          </div>

          {editingProfile ? (
            <form
              onSubmit={saveProfileEdit}
              className="pt-4 border-t border-slate-100 grid grid-cols-1 md:grid-cols-12 gap-3 items-end"
            >
              <div className="md:col-span-2">
                <label className="block text-xs text-slate-500 mb-1">
                  Вес (кг)
                </label>
                <input
                  type="number"
                  step="0.05"
                  value={draftWeight}
                  onChange={(e) => setDraftWeight(e.target.value)}
                  className="w-full h-9 px-2.5 rounded-lg border border-slate-200 text-xs font-mono tabular-nums"
                />
              </div>
              <div className="md:col-span-3">
                <label className="block text-xs text-slate-500 mb-1">
                  Аллергии
                </label>
                <input
                  type="text"
                  value={draftAllergies}
                  onChange={(e) => setDraftAllergies(e.target.value)}
                  className="w-full h-9 px-2.5 rounded-lg border border-slate-200 text-xs"
                />
              </div>
              <div className="md:col-span-3">
                <label className="block text-xs text-slate-500 mb-1">
                  Рацион
                </label>
                <input
                  type="text"
                  value={draftDiet}
                  onChange={(e) => setDraftDiet(e.target.value)}
                  className="w-full h-9 px-2.5 rounded-lg border border-slate-200 text-xs"
                />
              </div>
              <div className="md:col-span-2">
                <label className="block text-xs text-slate-500 mb-1">
                  Бокс стационара
                </label>
                <input
                  type="text"
                  value={draftCage}
                  onChange={(e) => setDraftCage(e.target.value)}
                  placeholder="Бокс С-01"
                  className="w-full h-9 px-2.5 rounded-lg border border-slate-200 text-xs font-mono"
                />
              </div>
              <div className="md:col-span-2">
                <button
                  type="submit"
                  className="w-full h-9 px-3 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800"
                >
                  Сохранить
                </button>
              </div>
            </form>
          ) : (
            <div className="pt-4 border-t border-slate-100 grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
              <div>
                <span className="text-slate-500">Аллергии и реакции: </span>
                <span className="font-medium text-slate-900">
                  {selectedPet.allergies}
                </span>
              </div>
              <div>
                <span className="text-slate-500">Назначенный рацион: </span>
                <span className="font-medium text-slate-900">
                  {selectedPet.diet}
                </span>
              </div>
            </div>
          )}

          {records.length > 0 && (
            <div className="pt-4 border-t border-slate-100">
              <div className="text-xs font-medium text-slate-500 mb-2">
                Динамика показателей по визитам
              </div>
              <div className="flex flex-wrap gap-3">
                {records
                  .slice()
                  .reverse()
                  .map((r) => (
                    <div
                      key={`vital-${r.id}`}
                      className="px-3 py-2 rounded-lg bg-slate-50 border border-slate-200 text-xs font-mono tabular-nums"
                    >
                      <div className="text-slate-500">{r.visitDate}</div>
                      <div className="text-slate-900 font-medium mt-0.5">
                        {r.weightKg} кг · {r.temperatureC.toFixed(1)} °C ·{' '}
                        {r.heartRateBpm} уд/м
                      </div>
                    </div>
                  ))}
              </div>
            </div>
          )}
        </section>

        <div className="flex items-center gap-1 border-b border-slate-200 overflow-x-auto">
          <button
            type="button"
            onClick={() => setDetailTab('history')}
            className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors whitespace-nowrap shrink-0 ${
              detailTab === 'history'
                ? 'border-teal-700 text-teal-700'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            История болезни ({records.length})
          </button>
          <button
            type="button"
            onClick={() => setDetailTab('vaccines')}
            className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors whitespace-nowrap shrink-0 ${
              detailTab === 'vaccines'
                ? 'border-teal-700 text-teal-700'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            Вакцинация и обработки ({vaccines.length})
          </button>
          <button
            type="button"
            onClick={() => setDetailTab('labs')}
            className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors whitespace-nowrap shrink-0 ${
              detailTab === 'labs'
                ? 'border-teal-700 text-teal-700'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            Анализы и УЗИ ({labs.length})
          </button>
          <button
            type="button"
            onClick={() => setDetailTab('visits')}
            className={`px-4 py-2.5 text-sm font-medium border-b-2 transition-colors whitespace-nowrap shrink-0 ${
              detailTab === 'visits'
                ? 'border-teal-700 text-teal-700'
                : 'border-transparent text-slate-600 hover:text-slate-900'
            }`}
          >
            Записи на прием ({visits.length})
          </button>
        </div>

        {detailTab === 'history' && (
          <div className="space-y-6">
            {showRecordForm && (
              <NewMedicalRecordForm
                pet={selectedPet}
                state={state}
                onCancel={() => setShowRecordForm(false)}
                onSubmit={async (payload) => {
                  const err = await onAddMedicalRecord(payload);
                  if (!err) setShowRecordForm(false);
                  return err;
                }}
              />
            )}

            {records.length === 0 ? (
              <div className="bg-white border border-slate-200 rounded-xl p-10 text-center">
                <p className="text-sm text-slate-600 mb-4">
                  В электронной карте пациента пока нет записей осмотров.
                </p>
                <button
                  type="button"
                  onClick={() => setShowRecordForm(true)}
                  className="px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800"
                >
                  Создать первую запись
                </button>
              </div>
            ) : (
              <div className="space-y-4">
                {records.map((rec) => {
                  const vet = state.veterinarians.find(
                    (v) => v.id === rec.veterinarianId,
                  );
                  return (
                    <article
                      key={rec.id}
                      className="bg-white border border-slate-200 rounded-xl p-6 space-y-4"
                    >
                      <div className="flex flex-wrap items-start justify-between gap-4 border-b border-slate-100 pb-4">
                        <div>
                          <div className="text-xs text-slate-500 tabular-nums">
                            {rec.visitDate} · {vet?.fullName ?? 'Врач клиники'}
                          </div>
                          <h3 className="text-base font-semibold text-slate-900 mt-1">
                            {rec.diagnosis}
                          </h3>
                        </div>
                        <div className="flex items-center gap-4 text-xs text-slate-600 font-mono tabular-nums">
                          <span>T: {rec.temperatureC.toFixed(1)} °C</span>
                          <span>Вес: {rec.weightKg} кг</span>
                          <span>ЧСС: {rec.heartRateBpm} уд/мин</span>
                        </div>
                      </div>

                      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 text-sm">
                        <div>
                          <div className="text-xs font-medium text-slate-500 mb-1">
                            Анамнез и жалобы
                          </div>
                          <p className="text-slate-700 leading-relaxed">
                            {rec.complaints}
                          </p>
                        </div>
                        <div>
                          <div className="text-xs font-medium text-slate-500 mb-1">
                            План лечения и рекомендации
                          </div>
                          <p className="text-slate-700 leading-relaxed">
                            {rec.treatmentPlan}
                          </p>
                        </div>
                      </div>

                      {rec.prescriptions.length > 0 && (
                        <div className="pt-3 border-t border-slate-100">
                          <div className="text-xs font-medium text-slate-500 mb-2">
                            Назначенные препараты
                          </div>
                          <div className="divide-y divide-slate-100 border border-slate-200 rounded-lg overflow-hidden">
                            {rec.prescriptions.map((rx, idx) => (
                              <div
                                key={`${rec.id}-rx-${idx}`}
                                className="px-4 py-2.5 flex flex-wrap items-center justify-between gap-3 text-xs bg-slate-50/50"
                              >
                                <span className="font-medium text-slate-900">
                                  {rx.medicationName}
                                </span>
                                <span className="text-slate-600">
                                  {rx.dosage} · {rx.frequency} ·{' '}
                                  <span className="tabular-nums">
                                    {rx.durationDays} дн.
                                  </span>
                                </span>
                                <span className="font-mono text-slate-500 tabular-nums">
                                  Списано со склада: {rx.dispensedQty}
                                </span>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </article>
                  );
                })}
              </div>
            )}
          </div>
        )}

        {detailTab === 'vaccines' && (
          <div className="space-y-4">
            <div className="flex justify-end">
              <button
                type="button"
                onClick={() => setShowVaccineForm((v) => !v)}
                className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800"
              >
                <Syringe size={14} />
                Добавить прививку / обработку
              </button>
            </div>

            {showVaccineForm && (
              <NewVaccinationForm
                pet={selectedPet}
                state={state}
                onCancel={() => setShowVaccineForm(false)}
                onSubmit={async (payload) => {
                  const err = await onAddVaccination(payload);
                  if (!err) setShowVaccineForm(false);
                  return err;
                }}
              />
            )}

            <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
              {vaccines.length === 0 ? (
                <div className="p-8 text-center text-sm text-slate-500">
                  Данные о прививках и обработках отсутствуют.
                </div>
              ) : (
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                      <th className="py-3 px-4">Тип</th>
                      <th className="py-3 px-4">Препарат</th>
                      <th className="py-3 px-4">Серия / Партия</th>
                      <th className="py-3 px-4">Дата введения</th>
                      <th className="py-3 px-4">Следующая</th>
                      <th className="py-3 px-4">Врач</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-200 text-sm">
                    {vaccines.map((v) => {
                      const vet = state.veterinarians.find(
                        (doc) => doc.id === v.veterinarianId,
                      );
                      return (
                        <tr key={v.id} className="hover:bg-slate-50">
                          <td className="py-3 px-4 text-xs text-slate-600">
                            {v.kind}
                          </td>
                          <td className="py-3 px-4 font-medium text-slate-900">
                            {v.vaccineName}
                          </td>
                          <td className="py-3 px-4 font-mono text-xs text-slate-600 tabular-nums">
                            {v.batchNumber}
                          </td>
                          <td className="py-3 px-4 text-slate-600 tabular-nums">
                            {v.administeredDate}
                          </td>
                          <td className="py-3 px-4 font-medium text-slate-900 tabular-nums">
                            {v.nextDueDate}
                          </td>
                          <td className="py-3 px-4 text-slate-600">
                            {vet?.fullName ?? '—'}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              )}
            </div>
          </div>
        )}

        {detailTab === 'labs' && (
          <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            {labs.length === 0 ? (
              <div className="p-8 text-center text-sm text-slate-500">
                Назначенных анализов и диагностических исследований пока нет.
              </div>
            ) : (
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                    <th className="py-3 px-4">Дата</th>
                    <th className="py-3 px-4">Исследование</th>
                    <th className="py-3 px-4">Заключение</th>
                    <th className="py-3 px-4">Статус</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200 text-sm">
                  {labs.map((lab) => (
                    <tr key={lab.id} className="hover:bg-slate-50">
                      <td className="py-3 px-4 font-mono text-xs tabular-nums">
                        {lab.orderedDate}
                      </td>
                      <td className="py-3 px-4">
                        <div className="font-medium text-slate-900">
                          {lab.testType}
                        </div>
                        <div className="text-xs text-slate-500">
                          {lab.sampleType}
                        </div>
                      </td>
                      <td className="py-3 px-4 text-xs text-slate-700">
                        {lab.resultSummary}
                        {lab.isAbnormal && (
                          <div className="mt-1 inline-flex items-center gap-1 text-red-700 font-medium">
                            <AlertTriangle size={12} />
                            Отклонение от нормы
                          </div>
                        )}
                      </td>
                      <td className="py-3 px-4 text-xs font-medium text-slate-700">
                        {lab.status}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}

        {detailTab === 'visits' && (
          <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
            {visits.length === 0 ? (
              <div className="p-8 text-center text-sm text-slate-500">
                Записей на прием для этого пациента пока нет.
              </div>
            ) : (
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                    <th className="py-3 px-4">Дата и время</th>
                    <th className="py-3 px-4">Врач и услуга</th>
                    <th className="py-3 px-4">Причина</th>
                    <th className="py-3 px-4">Статус</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200 text-sm">
                  {visits.map((a) => {
                    const vet = state.veterinarians.find(
                      (v) => v.id === a.veterinarianId,
                    );
                    return (
                      <tr key={a.id} className="hover:bg-slate-50">
                        <td className="py-3 px-4 font-mono text-xs text-slate-900 tabular-nums">
                          {a.date} · {a.timeSlot}
                        </td>
                        <td className="py-3 px-4">
                          <div className="text-slate-900">{vet?.fullName}</div>
                          <div className="text-xs text-slate-500">
                            {a.serviceType}
                          </div>
                        </td>
                        <td className="py-3 px-4 text-slate-600">{a.reason}</td>
                        <td className="py-3 px-4">
                          <AppointmentStatusLabel status={a.status} />
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            )}
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex flex-wrap items-center gap-3 flex-1 min-w-[260px]">
          <div className="relative flex-1 max-w-sm">
            <Search
              size={16}
              className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
            />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Поиск по кличке, породе, чипу, владельцу..."
              className="w-full h-10 pl-9 pr-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>

          <div className="flex items-center gap-1 p-1 bg-slate-200/70 rounded-lg overflow-x-auto max-w-full">
            {SPECIES_TABS.map((sp) => (
              <button
                key={sp}
                type="button"
                onClick={() => setSpeciesFilter(sp)}
                className={`px-3 py-1.5 text-xs font-medium rounded-md transition-colors whitespace-nowrap shrink-0 ${
                  speciesFilter === sp
                    ? 'bg-white text-slate-900 shadow-sm'
                    : 'text-slate-600 hover:text-slate-900'
                }`}
              >
                {sp}
              </button>
            ))}
          </div>
        </div>

        <button
          type="button"
          onClick={onOpenNewPatient}
          className="inline-flex items-center gap-2 px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 transition-colors whitespace-nowrap shrink-0"
        >
          <Plus size={15} />
          Новый пациент
        </button>
      </div>

      <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
        {filteredPets.length === 0 ? (
          <div className="p-10 text-center">
            <p className="text-sm text-slate-600 mb-3">
              По заданным параметрам пациенты не найдены.
            </p>
            <button
              type="button"
              onClick={() => {
                setSearch('');
                setSpeciesFilter('Все');
              }}
              className="text-xs font-medium text-teal-700 hover:text-teal-800"
            >
              Сбросить фильтры
            </button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50 text-xs font-medium text-slate-500">
                  <th className="py-3 px-4">Пациент</th>
                  <th className="py-3 px-4">Вид и порода</th>
                  <th className="py-3 px-4">Владелец</th>
                  <th className="py-3 px-4">Микрочип и кровь</th>
                  <th className="py-3 px-4 text-right">Вес</th>
                  <th className="py-3 px-4">Статус</th>
                  <th className="py-3 px-4 text-right">Карта</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 text-sm">
                {filteredPets.map((pet) => {
                  const owner = state.owners.find((o) => o.id === pet.ownerId);
                  return (
                    <tr
                      key={pet.id}
                      onClick={() => onSelectPet(pet.id)}
                      className="hover:bg-slate-50 cursor-pointer transition-colors"
                    >
                      <td className="py-3 px-4">
                        <div className="flex items-center gap-3">
                          <PetAvatar
                            src={pet.photoUrl}
                            alt={pet.name}
                            size="sm"
                          />
                          <div>
                            <div className="font-medium text-slate-900">
                              {pet.name}
                            </div>
                            <div className="text-xs text-slate-500 tabular-nums">
                              {formatAge(pet.birthDate)} · {pet.gender}
                            </div>
                          </div>
                        </div>
                      </td>
                      <td className="py-3 px-4 text-slate-700">
                        <div>{pet.species}</div>
                        <div className="text-xs text-slate-500">{pet.breed}</div>
                      </td>
                      <td className="py-3 px-4">
                        <div className="text-slate-900">
                          {owner?.fullName ?? '—'}
                        </div>
                        <div className="text-xs text-slate-500 tabular-nums">
                          {owner?.phone}
                        </div>
                      </td>
                      <td className="py-3 px-4">
                        <div className="font-mono text-xs text-slate-700 tabular-nums">
                          {pet.microchipNumber}
                        </div>
                        <div className="text-xs text-slate-500">
                          {pet.bloodType}
                        </div>
                      </td>
                      <td className="py-3 px-4 text-right font-mono text-xs text-slate-900 tabular-nums">
                        {pet.weightKg} кг
                      </td>
                      <td className="py-3 px-4">
                        <PetHealthLabel status={pet.status} />
                      </td>
                      <td className="py-3 px-4 text-right">
                        <button
                          type="button"
                          onClick={(e) => {
                            e.stopPropagation();
                            onSelectPet(pet.id);
                          }}
                          className="text-xs font-medium text-teal-700 hover:text-teal-800 whitespace-nowrap"
                        >
                          История болезни
                        </button>
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

function NewMedicalRecordForm({
  pet,
  state,
  onCancel,
  onSubmit,
}: {
  pet: Pet;
  state: ClinicState;
  onCancel: () => void;
  onSubmit: (payload: {
    petId: string;
    veterinarianId: string;
    complaints: string;
    diagnosis: string;
    temperatureC: number;
    weightKg: number;
    heartRateBpm: number;
    treatmentPlan: string;
    prescriptions: PrescriptionItem[];
    createInvoice?: boolean;
    consultationFeeRub?: number;
  }) => Promise<string | null>;
}) {
  const [veterinarianId, setVeterinarianId] = useState(
    state.veterinarians[0]?.id || '',
  );
  const [complaints, setComplaints] = useState('');
  const [diagnosis, setDiagnosis] = useState('');
  const [temperatureC, setTemperatureC] = useState('38.5');
  const [weightKg, setWeightKg] = useState(String(pet.weightKg));
  const [heartRateBpm, setHeartRateBpm] = useState('92');
  const [treatmentPlan, setTreatmentPlan] = useState('');
  const [prescriptions, setPrescriptions] = useState<PrescriptionItem[]>([]);
  const [selectedMedId, setSelectedMedId] = useState(
    state.medications[0]?.id || '',
  );
  const [dosage, setDosage] = useState('1 таблетка');
  const [frequency, setFrequency] = useState('2 раза в день');
  const [durationDays, setDurationDays] = useState('7');
  const [dispensedQty, setDispensedQty] = useState('1');
  const [createInvoice, setCreateInvoice] = useState(true);
  const [consultationFeeRub, setConsultationFeeRub] = useState('1800');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const owner = state.owners.find((o) => o.id === pet.ownerId);
  const discountRate =
    owner?.loyaltyTier === 'VIP-полис'
      ? 0.1
      : owner?.loyaltyTier === 'Забота+'
        ? 0.05
        : 0;
  const medsCost = prescriptions.reduce((sum, rx) => {
    const med = state.medications.find((m) => m.id === rx.medicationId);
    return sum + (med ? med.unitPriceRub * rx.dispensedQty : 0);
  }, 0);
  const feeValue = Math.max(0, Number(consultationFeeRub) || 0);
  const invoiceTotal = Math.round((feeValue + medsCost) * (1 - discountRate));

  const addRx = () => {
    const med = state.medications.find((m) => m.id === selectedMedId);
    if (!med) return;
    setPrescriptions((prev) => [
      ...prev,
      {
        medicationId: med.id,
        medicationName: med.name,
        dosage: dosage.trim() || 'По инструкции',
        frequency: frequency.trim() || '1 раз в сутки',
        durationDays: Number(durationDays) || 5,
        dispensedQty: Math.max(0, Number(dispensedQty) || 0),
      },
    ]);
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!diagnosis.trim()) {
      setError('Укажите клинический диагноз.');
      return;
    }
    setSaving(true);
    const err = await onSubmit({
      petId: pet.id,
      veterinarianId,
      complaints: complaints.trim() || 'Плановый клинический осмотр',
      diagnosis: diagnosis.trim(),
      temperatureC: Number(temperatureC) || 38.5,
      weightKg: Number(weightKg) || pet.weightKg,
      heartRateBpm: Number(heartRateBpm) || 92,
      treatmentPlan: treatmentPlan.trim() || 'Амбулаторное наблюдение',
      prescriptions,
      createInvoice,
      consultationFeeRub: feeValue,
    });
    setSaving(false);
    if (err) setError(err);
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="bg-white border border-slate-200 rounded-xl p-6 space-y-5"
    >
      <div className="flex items-center justify-between border-b border-slate-200 pb-3">
        <h3 className="text-base font-semibold text-slate-900">
          Новый протокол осмотра — {pet.name}
        </h3>
        <button
          type="button"
          onClick={onCancel}
          className="text-xs font-medium text-slate-500 hover:text-slate-800"
        >
          Отмена
        </button>
      </div>

      {error && (
        <div className="p-3 rounded-lg bg-red-50 border border-red-200 text-xs text-red-700">
          {error}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="md:col-span-2">
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Лечащий врач
          </label>
          <select
            value={veterinarianId}
            onChange={(e) => setVeterinarianId(e.target.value)}
            className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
          >
            {state.veterinarians.map((v) => (
              <option key={v.id} value={v.id}>
                {v.fullName}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Температура (°C)
          </label>
          <input
            type="number"
            step="0.1"
            value={temperatureC}
            onChange={(e) => setTemperatureC(e.target.value)}
            className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm font-mono tabular-nums focus:outline-none focus:border-teal-700"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Вес (кг) / ЧСС
          </label>
          <div className="grid grid-cols-2 gap-2">
            <input
              type="number"
              step="0.05"
              value={weightKg}
              onChange={(e) => setWeightKg(e.target.value)}
              className="w-full h-10 px-2.5 rounded-lg border border-slate-200 bg-white text-sm font-mono tabular-nums focus:outline-none focus:border-teal-700"
            />
            <input
              type="number"
              value={heartRateBpm}
              onChange={(e) => setHeartRateBpm(e.target.value)}
              className="w-full h-10 px-2.5 rounded-lg border border-slate-200 bg-white text-sm font-mono tabular-nums focus:outline-none focus:border-teal-700"
            />
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Клинический диагноз
          </label>
          <input
            type="text"
            value={diagnosis}
            onChange={(e) => setDiagnosis(e.target.value)}
            placeholder="Например: Острый отит наружного уха"
            className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm focus:outline-none focus:border-teal-700"
            required
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Анамнез и жалобы владельца
          </label>
          <input
            type="text"
            value={complaints}
            onChange={(e) => setComplaints(e.target.value)}
            placeholder="Беспокойство, снижение аппетита"
            className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm focus:outline-none focus:border-teal-700"
          />
        </div>
      </div>

      <div>
        <label className="block text-xs font-medium text-slate-700 mb-1.5">
          План лечения и рекомендации
        </label>
        <textarea
          rows={2}
          value={treatmentPlan}
          onChange={(e) => setTreatmentPlan(e.target.value)}
          placeholder="Режим кормления, обработки, повторный прием через 7 дней..."
          className="w-full px-3 py-2 rounded-lg border border-slate-200 bg-white text-sm focus:outline-none focus:border-teal-700"
        />
      </div>

      <div className="border-t border-slate-200 pt-4 space-y-3">
        <div className="text-xs font-semibold text-slate-800">
          Назначить препарат из аптеки (автосписание со склада)
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-6 gap-2.5 items-end">
          <div className="sm:col-span-2">
            <label className="block text-xs text-slate-500 mb-1">Препарат</label>
            <select
              value={selectedMedId}
              onChange={(e) => setSelectedMedId(e.target.value)}
              className="w-full h-9 px-2.5 rounded-lg border border-slate-200 bg-white text-xs text-slate-900"
            >
              {state.medications.map((m) => (
                <option key={m.id} value={m.id}>
                  {m.name} (ост. {m.stockQty} {m.unit})
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs text-slate-500 mb-1">Дозировка</label>
            <input
              type="text"
              value={dosage}
              onChange={(e) => setDosage(e.target.value)}
              className="w-full h-9 px-2.5 rounded-lg border border-slate-200 bg-white text-xs"
            />
          </div>
          <div>
            <label className="block text-xs text-slate-500 mb-1">Кратность</label>
            <input
              type="text"
              value={frequency}
              onChange={(e) => setFrequency(e.target.value)}
              className="w-full h-9 px-2.5 rounded-lg border border-slate-200 bg-white text-xs"
            />
          </div>
          <div>
            <label className="block text-xs text-slate-500 mb-1">
              Дней / Списать
            </label>
            <div className="grid grid-cols-2 gap-1.5">
              <input
                type="number"
                min="1"
                value={durationDays}
                onChange={(e) => setDurationDays(e.target.value)}
                className="w-full h-9 px-2 rounded-lg border border-slate-200 bg-white text-xs tabular-nums"
              />
              <input
                type="number"
                min="0"
                value={dispensedQty}
                onChange={(e) => setDispensedQty(e.target.value)}
                className="w-full h-9 px-2 rounded-lg border border-slate-200 bg-white text-xs tabular-nums"
              />
            </div>
          </div>
          <button
            type="button"
            onClick={addRx}
            className="h-9 px-3 text-xs font-medium text-teal-700 border border-teal-200 bg-teal-50 rounded-lg hover:bg-teal-100 whitespace-nowrap"
          >
            + В рецепт
          </button>
        </div>

        {prescriptions.length > 0 && (
          <div className="divide-y divide-slate-200 border border-slate-200 rounded-lg">
            {prescriptions.map((rx, idx) => (
              <div
                key={`draft-rx-${idx}`}
                className="px-3 py-2 flex items-center justify-between text-xs"
              >
                <span>
                  <strong className="text-slate-900">{rx.medicationName}</strong>{' '}
                  · {rx.dosage} · {rx.frequency} ({rx.durationDays} дн., списать{' '}
                  {rx.dispensedQty})
                </span>
                <button
                  type="button"
                  onClick={() =>
                    setPrescriptions((prev) => prev.filter((_, i) => i !== idx))
                  }
                  className="text-slate-400 hover:text-red-600 p-1"
                  aria-label="Удалить назначение"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="border-t border-slate-200 pt-4 space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <label className="inline-flex items-center gap-2 text-xs font-medium text-slate-800 cursor-pointer">
            <input
              type="checkbox"
              checked={createInvoice}
              onChange={(e) => setCreateInvoice(e.target.checked)}
              className="rounded border-slate-300 text-teal-700 focus:ring-teal-700"
            />
            Автоматически выставить счет владельцу после осмотра
          </label>
          {owner && (
            <span className="text-xs text-slate-500">
              Программа: {owner.loyaltyTier}
              {discountRate > 0 ? ` (скидка ${Math.round(discountRate * 100)}%)` : ''}
            </span>
          )}
        </div>

        {createInvoice && (
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 p-3.5 rounded-lg bg-slate-50 border border-slate-200 items-center text-xs">
            <div>
              <label className="block text-slate-500 mb-1">
                Стоимость приема (₽)
              </label>
              <input
                type="number"
                min="0"
                step="50"
                value={consultationFeeRub}
                onChange={(e) => setConsultationFeeRub(e.target.value)}
                className="w-full h-9 px-2.5 rounded-lg border border-slate-200 bg-white font-mono tabular-nums text-slate-900"
              />
            </div>
            <div className="text-slate-600 tabular-nums">
              <div>Препараты со склада:</div>
              <div className="font-mono font-semibold text-slate-900 mt-0.5">
                {medsCost.toLocaleString('ru-RU')} ₽
              </div>
            </div>
            <div className="sm:text-right tabular-nums">
              <div className="text-slate-500">Итого к оплате по счету:</div>
              <div className="font-mono text-sm font-semibold text-teal-700 mt-0.5">
                {invoiceTotal.toLocaleString('ru-RU')} ₽
              </div>
            </div>
          </div>
        )}
      </div>

      <div className="flex justify-end gap-3 pt-2">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 text-xs font-medium text-slate-700 border border-slate-200 rounded-lg hover:bg-slate-50"
        >
          Отмена
        </button>
        <button
          type="submit"
          disabled={saving}
          className="px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 disabled:opacity-50"
        >
          {saving ? 'Сохранение...' : 'Сохранить запись в карту'}
        </button>
      </div>
    </form>
  );
}

function NewVaccinationForm({
  pet,
  state,
  onCancel,
  onSubmit,
}: {
  pet: Pet;
  state: ClinicState;
  onCancel: () => void;
  onSubmit: (payload: {
    petId: string;
    kind: Vaccination['kind'];
    vaccineName: string;
    administeredDate: string;
    nextDueDate: string;
    batchNumber: string;
    veterinarianId: string;
  }) => Promise<string | null>;
}) {
  const [kind, setKind] = useState<Vaccination['kind']>('Вакцинация');
  const [vaccineName, setVaccineName] = useState('Нобивак DHPPi + RL');
  const [administeredDate, setAdministeredDate] = useState('2026-11-22');
  const [nextDueDate, setNextDueDate] = useState('2027-11-22');
  const [batchNumber, setBatchNumber] = useState('');
  const [veterinarianId, setVeterinarianId] = useState(
    state.veterinarians[0]?.id || '',
  );
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSaving(true);
    await onSubmit({
      petId: pet.id,
      kind,
      vaccineName,
      administeredDate,
      nextDueDate,
      batchNumber,
      veterinarianId,
    });
    setSaving(false);
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="bg-white border border-slate-200 rounded-xl p-5 space-y-4"
    >
      <div className="grid grid-cols-1 md:grid-cols-6 gap-3">
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1">
            Тип процедуры
          </label>
          <select
            value={kind}
            onChange={(e) => setKind(e.target.value as Vaccination['kind'])}
            className="w-full h-9 px-2.5 rounded-lg border border-slate-200 text-xs bg-white"
          >
            <option value="Вакцинация">Вакцинация</option>
            <option value="Дегельминтизация">Дегельминтизация</option>
            <option value="Обработка от клещей">Обработка от клещей</option>
          </select>
        </div>
        <div className="md:col-span-2">
          <label className="block text-xs font-medium text-slate-700 mb-1">
            Препарат
          </label>
          <input
            type="text"
            value={vaccineName}
            onChange={(e) => setVaccineName(e.target.value)}
            className="w-full h-9 px-3 rounded-lg border border-slate-200 text-sm"
            required
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1">
            Серия / Партия
          </label>
          <input
            type="text"
            value={batchNumber}
            onChange={(e) => setBatchNumber(e.target.value)}
            placeholder="NB-9012"
            className="w-full h-9 px-3 rounded-lg border border-slate-200 text-sm font-mono"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1">
            Дата введения
          </label>
          <input
            type="date"
            value={administeredDate}
            onChange={(e) => setAdministeredDate(e.target.value)}
            className="w-full h-9 px-3 rounded-lg border border-slate-200 text-sm tabular-nums"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1">
            Следующая дата
          </label>
          <input
            type="date"
            value={nextDueDate}
            onChange={(e) => setNextDueDate(e.target.value)}
            className="w-full h-9 px-3 rounded-lg border border-slate-200 text-sm tabular-nums"
          />
        </div>
      </div>

      <div className="flex items-center justify-between gap-4">
        <select
          value={veterinarianId}
          onChange={(e) => setVeterinarianId(e.target.value)}
          className="h-9 px-3 rounded-lg border border-slate-200 text-xs bg-white"
        >
          {state.veterinarians.map((v) => (
            <option key={v.id} value={v.id}>
              {v.fullName}
            </option>
          ))}
        </select>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={onCancel}
            className="px-3 py-1.5 text-xs font-medium text-slate-600 border border-slate-200 rounded-lg"
          >
            Отмена
          </button>
          <button
            type="submit"
            disabled={saving}
            className="px-3 py-1.5 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800"
          >
            Сохранить запись
          </button>
        </div>
      </div>
    </form>
  );
}
