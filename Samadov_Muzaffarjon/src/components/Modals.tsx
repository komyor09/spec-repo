import {X} from 'lucide-react';
import {useState, type FormEvent, type ReactNode} from 'react';
import type {
  Appointment,
  MedicationCategory,
  Owner,
  Pet,
  PetSpecies,
  Veterinarian,
} from '../types/clinic.ts';

function ModalShell({
  title,
  onClose,
  children,
}: {
  title: string;
  onClose: () => void;
  children: ReactNode;
}) {
  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4"
      role="dialog"
      aria-modal="true"
      aria-label={title}
    >
      <div className="w-full max-w-lg rounded-xl bg-white border border-slate-200 shadow-xl overflow-hidden">
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-200">
          <h2 className="text-base font-semibold text-slate-900">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            className="p-2 rounded-lg text-slate-500 hover:text-slate-900 hover:bg-slate-100 transition-colors"
            aria-label="Закрыть окно"
          >
            <X size={18} />
          </button>
        </div>
        <div className="p-6 max-h-[80vh] overflow-y-auto">{children}</div>
      </div>
    </div>
  );
}

const TIME_SLOTS = [
  '09:00',
  '09:30',
  '10:00',
  '10:30',
  '11:00',
  '11:30',
  '12:00',
  '13:30',
  '14:00',
  '14:30',
  '15:00',
  '15:30',
  '16:00',
  '16:30',
  '17:00',
];

const SERVICE_TYPES: Appointment['serviceType'][] = [
  'Первичный осмотр',
  'Повторный прием',
  'Вакцинация',
  'УЗИ / Диагностика',
  'Хирургия / Стоматология',
];

export function AppointmentModal({
  pets,
  owners,
  veterinarians,
  appointments,
  defaultPetId,
  onClose,
  onSubmit,
}: {
  pets: Pet[];
  owners: Owner[];
  veterinarians: Veterinarian[];
  appointments: Appointment[];
  defaultPetId?: string;
  onClose: () => void;
  onSubmit: (payload: {
    petId: string;
    veterinarianId: string;
    date: string;
    timeSlot: string;
    serviceType: Appointment['serviceType'];
    reason: string;
    notes?: string;
  }) => Promise<string | null>;
}) {
  const [petId, setPetId] = useState(defaultPetId || pets[0]?.id || '');
  const [veterinarianId, setVeterinarianId] = useState(veterinarians[0]?.id || '');
  const [date, setDate] = useState('2026-11-23');
  const [timeSlot, setTimeSlot] = useState('10:00');
  const [serviceType, setServiceType] =
    useState<Appointment['serviceType']>('Первичный осмотр');
  const [reason, setReason] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const bookedSlots = new Set(
    appointments
      .filter(
        (a) =>
          a.veterinarianId === veterinarianId &&
          a.date === date &&
          a.status !== 'Отменен',
      )
      .map((a) => a.timeSlot),
  );

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!petId || !veterinarianId || !reason.trim()) {
      setError('Выберите питомца, врача и укажите причину визита.');
      return;
    }
    if (bookedSlots.has(timeSlot)) {
      setError('Этот временной слот уже занят. Выберите свободное время.');
      return;
    }
    setSubmitting(true);
    setError(null);
    const err = await onSubmit({
      petId,
      veterinarianId,
      date,
      timeSlot,
      serviceType,
      reason: reason.trim(),
      notes: notes.trim() || undefined,
    });
    setSubmitting(false);
    if (err) {
      setError(err);
    } else {
      onClose();
    }
  };

  return (
    <ModalShell title="Новая запись на прием" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="p-3 rounded-lg bg-red-50 border border-red-200 text-xs text-red-700">
            {error}
          </div>
        )}
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Пациент
          </label>
          <select
            value={petId}
            onChange={(e) => setPetId(e.target.value)}
            className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
          >
            {pets.map((p) => {
              const owner = owners.find((o) => o.id === p.ownerId);
              return (
                <option key={p.id} value={p.id}>
                  {p.name} ({p.species} · {owner?.fullName ?? 'Владелец'})
                </option>
              );
            })}
          </select>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Ветеринарный врач
            </label>
            <select
              value={veterinarianId}
              onChange={(e) => setVeterinarianId(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            >
              {veterinarians.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.fullName}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Тип услуги
            </label>
            <select
              value={serviceType}
              onChange={(e) =>
                setServiceType(e.target.value as Appointment['serviceType'])
              }
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            >
              {SERVICE_TYPES.map((st) => (
                <option key={st} value={st}>
                  {st}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Дата приема
            </label>
            <input
              type="date"
              value={date}
              onChange={(e) => setDate(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 tabular-nums focus:outline-none focus:border-teal-700"
              required
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Время слота
            </label>
            <select
              value={timeSlot}
              onChange={(e) => setTimeSlot(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 tabular-nums focus:outline-none focus:border-teal-700"
            >
              {TIME_SLOTS.map((slot) => {
                const isBooked = bookedSlots.has(slot);
                return (
                  <option key={slot} value={slot} disabled={isBooked}>
                    {slot}
                    {isBooked ? ' (занято)' : ''}
                  </option>
                );
              })}
            </select>
          </div>
        </div>

        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Причина обращения
          </label>
          <input
            type="text"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Например: ежегодная вакцинация или осмотр"
            className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            required
          />
        </div>

        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Примечание (необязательно)
          </label>
          <textarea
            rows={2}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Дополнительная информация для врача"
            className="w-full px-3 py-2 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
          />
        </div>

        <div className="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-slate-700 rounded-lg border border-slate-200 hover:bg-slate-50 transition-colors whitespace-nowrap"
          >
            Отмена
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="px-4 py-2 text-sm font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 disabled:opacity-50 transition-colors whitespace-nowrap"
          >
            {submitting ? 'Сохранение...' : 'Записать на прием'}
          </button>
        </div>
      </form>
    </ModalShell>
  );
}

const SPECIES_OPTIONS: PetSpecies[] = [
  'Собака',
  'Кошка',
  'Грызун и заяц',
  'Птица',
  'Рептилия',
];

export function PatientModal({
  owners,
  onClose,
  onSubmit,
}: {
  owners: Owner[];
  onClose: () => void;
  onSubmit: (payload: {
    ownerId: string;
    name: string;
    species: PetSpecies;
    breed: string;
    color: string;
    gender: 'Самец' | 'Самка';
    sterilized: boolean;
    birthDate: string;
    weightKg: number;
    bloodType: string;
    microchipNumber: string;
    allergies: string;
    diet: string;
    status: Pet['status'];
  }) => Promise<string | null>;
}) {
  const [ownerId, setOwnerId] = useState(owners[0]?.id || '');
  const [name, setName] = useState('');
  const [species, setSpecies] = useState<PetSpecies>('Собака');
  const [breed, setBreed] = useState('');
  const [color, setColor] = useState('');
  const [gender, setGender] = useState<'Самец' | 'Самка'>('Самец');
  const [sterilized, setSterilized] = useState(false);
  const [birthDate, setBirthDate] = useState('2024-05-01');
  const [weightKg, setWeightKg] = useState('8.5');
  const [bloodType, setBloodType] = useState('DEA 1.1+');
  const [microchipNumber, setMicrochipNumber] = useState('');
  const [allergies, setAllergies] = useState('Не выявлены');
  const [diet, setDiet] = useState('Полнорационный сухой корм');
  const [status, setStatus] = useState<Pet['status']>('Здоров');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !ownerId) {
      setError('Укажите кличку и владельца питомца.');
      return;
    }
    setSubmitting(true);
    const err = await onSubmit({
      ownerId,
      name: name.trim(),
      species,
      breed: breed.trim() || 'Метис',
      color: color.trim() || 'Не указан',
      gender,
      sterilized,
      birthDate,
      weightKg: Number(weightKg) || 1,
      bloodType: bloodType.trim() || 'Не типируется',
      microchipNumber: microchipNumber.trim(),
      allergies: allergies.trim() || 'Не выявлены',
      diet: diet.trim() || 'Стандартный рацион',
      status,
    });
    setSubmitting(false);
    if (err) {
      setError(err);
    } else {
      onClose();
    }
  };

  return (
    <ModalShell title="Регистрация нового пациента" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="p-3 rounded-lg bg-red-50 border border-red-200 text-xs text-red-700">
            {error}
          </div>
        )}
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Владелец
          </label>
          <select
            value={ownerId}
            onChange={(e) => setOwnerId(e.target.value)}
            className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
          >
            {owners.map((o) => (
              <option key={o.id} value={o.id}>
                {o.fullName} ({o.phone})
              </option>
            ))}
          </select>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Кличка
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Например: Арчи"
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
              required
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Вид животного
            </label>
            <select
              value={species}
              onChange={(e) => setSpecies(e.target.value as PetSpecies)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            >
              {SPECIES_OPTIONS.map((sp) => (
                <option key={sp} value={sp}>
                  {sp}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Порода
            </label>
            <input
              type="text"
              value={breed}
              onChange={(e) => setBreed(e.target.value)}
              placeholder="Джек-рассел-терьер"
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Окрас
            </label>
            <input
              type="text"
              value={color}
              onChange={(e) => setColor(e.target.value)}
              placeholder="Триколор"
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
        </div>

        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Пол
            </label>
            <select
              value={gender}
              onChange={(e) => setGender(e.target.value as 'Самец' | 'Самка')}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            >
              <option value="Самец">Самец</option>
              <option value="Самка">Самка</option>
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Дата рождения
            </label>
            <input
              type="date"
              value={birthDate}
              onChange={(e) => setBirthDate(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 tabular-nums focus:outline-none focus:border-teal-700"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Вес (кг)
            </label>
            <input
              type="number"
              step="0.05"
              min="0.05"
              value={weightKg}
              onChange={(e) => setWeightKg(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 tabular-nums focus:outline-none focus:border-teal-700"
            />
          </div>
        </div>

        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Группа крови
            </label>
            <input
              type="text"
              value={bloodType}
              onChange={(e) => setBloodType(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Статус
            </label>
            <select
              value={status}
              onChange={(e) => setStatus(e.target.value as Pet['status'])}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            >
              <option value="Здоров">Здоров</option>
              <option value="Наблюдение">Наблюдение</option>
              <option value="Стационар">Стационар</option>
            </select>
          </div>
          <div className="flex items-end pb-2">
            <label className="inline-flex items-center gap-2 text-xs text-slate-700">
              <input
                type="checkbox"
                checked={sterilized}
                onChange={(e) => setSterilized(e.target.checked)}
              />
              Стерилизован
            </label>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Номер микрочипа
            </label>
            <input
              type="text"
              value={microchipNumber}
              onChange={(e) => setMicrochipNumber(e.target.value)}
              placeholder="Автогенерация если пусто"
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm font-mono text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Аллергии
            </label>
            <input
              type="text"
              value={allergies}
              onChange={(e) => setAllergies(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
        </div>

        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Рекомендуемый рацион
          </label>
          <input
            type="text"
            value={diet}
            onChange={(e) => setDiet(e.target.value)}
            className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
          />
        </div>

        <div className="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-slate-700 rounded-lg border border-slate-200 hover:bg-slate-50 transition-colors"
          >
            Отмена
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="px-4 py-2 text-sm font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 disabled:opacity-50 transition-colors"
          >
            {submitting ? 'Сохранение...' : 'Создать карту'}
          </button>
        </div>
      </form>
    </ModalShell>
  );
}

export function OwnerModal({
  onClose,
  onSubmit,
}: {
  onClose: () => void;
  onSubmit: (payload: {
    fullName: string;
    phone: string;
    email: string;
    address: string;
    loyaltyTier: Owner['loyaltyTier'];
  }) => Promise<string | null>;
}) {
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [address, setAddress] = useState('');
  const [loyaltyTier, setLoyaltyTier] =
    useState<Owner['loyaltyTier']>('Стандарт');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!fullName.trim() || !phone.trim()) {
      setError('Укажите ФИО и телефон владельца.');
      return;
    }
    setSubmitting(true);
    const err = await onSubmit({
      fullName: fullName.trim(),
      phone: phone.trim(),
      email: email.trim(),
      address: address.trim(),
      loyaltyTier,
    });
    setSubmitting(false);
    if (err) {
      setError(err);
    } else {
      onClose();
    }
  };

  return (
    <ModalShell title="Новый владелец (клиент)" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="p-3 rounded-lg bg-red-50 border border-red-200 text-xs text-red-700">
            {error}
          </div>
        )}
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            ФИО владельца
          </label>
          <input
            type="text"
            value={fullName}
            onChange={(e) => setFullName(e.target.value)}
            placeholder="Иванова Светлана Сергеевна"
            className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            required
          />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Телефон
            </label>
            <input
              type="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="+7 (999) 123-45-67"
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
              required
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Email для личного кабинета
            </label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="client@example.ru"
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Адрес проживания
            </label>
            <input
              type="text"
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              placeholder="ул. Профсоюзная, д. 12"
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Программа обслуживания
            </label>
            <select
              value={loyaltyTier}
              onChange={(e) =>
                setLoyaltyTier(e.target.value as Owner['loyaltyTier'])
              }
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            >
              <option value="Стандарт">Стандарт</option>
              <option value="Забота+">Забота+</option>
              <option value="VIP-полис">VIP-полис</option>
            </select>
          </div>
        </div>
        <div className="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-slate-700 rounded-lg border border-slate-200 hover:bg-slate-50 transition-colors"
          >
            Отмена
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="px-4 py-2 text-sm font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 disabled:opacity-50 transition-colors"
          >
            {submitting ? 'Сохранение...' : 'Добавить владельца'}
          </button>
        </div>
      </form>
    </ModalShell>
  );
}

const MED_CATEGORIES: MedicationCategory[] = [
  'Антибиотики',
  'Вакцины',
  'Противопаразитарные',
  'Анальгетики и НПВС',
  'Гастропротекторы',
  'Растворы и инфузии',
  'Дерматология и уши',
  'Кардиология',
];

export function MedicationModal({
  onClose,
  onSubmit,
}: {
  onClose: () => void;
  onSubmit: (payload: {
    name: string;
    activeIngredient: string;
    manufacturer: string;
    category: MedicationCategory;
    dosageForm: string;
    unit: string;
    stockQty: number;
    minThreshold: number;
    storageCondition: string;
    batchNumber: string;
    prescriptionRequired: boolean;
    expiryDate: string;
    unitPriceRub: number;
  }) => Promise<string | null>;
}) {
  const [name, setName] = useState('');
  const [activeIngredient, setActiveIngredient] = useState('');
  const [manufacturer, setManufacturer] = useState('Zoetis');
  const [category, setCategory] = useState<MedicationCategory>('Антибиотики');
  const [dosageForm, setDosageForm] = useState('Таблетки №10');
  const [unit, setUnit] = useState('уп.');
  const [stockQty, setStockQty] = useState('15');
  const [minThreshold, setMinThreshold] = useState('5');
  const [storageCondition, setStorageCondition] = useState('До +25 °C');
  const [batchNumber, setBatchNumber] = useState('');
  const [prescriptionRequired, setPrescriptionRequired] = useState(true);
  const [expiryDate, setExpiryDate] = useState('2028-03-01');
  const [unitPriceRub, setUnitPriceRub] = useState('750');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !activeIngredient.trim()) {
      setError('Укажите наименование и действующее вещество.');
      return;
    }
    setSubmitting(true);
    const err = await onSubmit({
      name: name.trim(),
      activeIngredient: activeIngredient.trim(),
      manufacturer: manufacturer.trim() || 'ВетФарм',
      category,
      dosageForm: dosageForm.trim(),
      unit: unit.trim(),
      stockQty: Number(stockQty) || 0,
      minThreshold: Number(minThreshold) || 5,
      storageCondition: storageCondition.trim() || 'До +25 °C',
      batchNumber: batchNumber.trim(),
      prescriptionRequired,
      expiryDate,
      unitPriceRub: Number(unitPriceRub) || 0,
    });
    setSubmitting(false);
    if (err) {
      setError(err);
    } else {
      onClose();
    }
  };

  return (
    <ModalShell title="Добавить препарат в аптеку" onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="p-3 rounded-lg bg-red-50 border border-red-200 text-xs text-red-700">
            {error}
          </div>
        )}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Наименование препарата
            </label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Амоксициллин 150 мг/мл"
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
              required
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Производитель
            </label>
            <input
              type="text"
              value={manufacturer}
              onChange={(e) => setManufacturer(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
        </div>
        <div>
          <label className="block text-xs font-medium text-slate-700 mb-1.5">
            Действующее вещество
          </label>
          <input
            type="text"
            value={activeIngredient}
            onChange={(e) => setActiveIngredient(e.target.value)}
            placeholder="Амоксициллина тригидрат"
            className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            required
          />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Категория
            </label>
            <select
              value={category}
              onChange={(e) => setCategory(e.target.value as MedicationCategory)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            >
              {MED_CATEGORIES.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Форма выпуска
            </label>
            <input
              type="text"
              value={dosageForm}
              onChange={(e) => setDosageForm(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
        </div>
        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Остаток
            </label>
            <input
              type="number"
              min="0"
              value={stockQty}
              onChange={(e) => setStockQty(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 tabular-nums focus:outline-none focus:border-teal-700"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Ед. изм.
            </label>
            <input
              type="text"
              value={unit}
              onChange={(e) => setUnit(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Мин. порог
            </label>
            <input
              type="number"
              min="1"
              value={minThreshold}
              onChange={(e) => setMinThreshold(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 tabular-nums focus:outline-none focus:border-teal-700"
            />
          </div>
        </div>
        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Условия хранения
            </label>
            <input
              type="text"
              value={storageCondition}
              onChange={(e) => setStorageCondition(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 focus:outline-none focus:border-teal-700"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Срок годности
            </label>
            <input
              type="date"
              value={expiryDate}
              onChange={(e) => setExpiryDate(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 tabular-nums focus:outline-none focus:border-teal-700"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Цена (₽)
            </label>
            <input
              type="number"
              min="0"
              value={unitPriceRub}
              onChange={(e) => setUnitPriceRub(e.target.value)}
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm text-slate-900 tabular-nums focus:outline-none focus:border-teal-700"
            />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-4 items-center">
          <div>
            <label className="block text-xs font-medium text-slate-700 mb-1.5">
              Серия / Партия
            </label>
            <input
              type="text"
              value={batchNumber}
              onChange={(e) => setBatchNumber(e.target.value)}
              placeholder="Автогенерация"
              className="w-full h-10 px-3 rounded-lg border border-slate-200 bg-white text-sm font-mono"
            />
          </div>
          <label className="inline-flex items-center gap-2 text-xs text-slate-700 pt-5">
            <input
              type="checkbox"
              checked={prescriptionRequired}
              onChange={(e) => setPrescriptionRequired(e.target.checked)}
            />
            Строго рецептурный отпуск
          </label>
        </div>
        <div className="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-slate-700 rounded-lg border border-slate-200 hover:bg-slate-50 transition-colors"
          >
            Отмена
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="px-4 py-2 text-sm font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 disabled:opacity-50 transition-colors"
          >
            {submitting ? 'Сохранение...' : 'Добавить в базу'}
          </button>
        </div>
      </form>
    </ModalShell>
  );
}
