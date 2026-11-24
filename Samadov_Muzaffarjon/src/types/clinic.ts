export type PetSpecies =
  | 'Собака'
  | 'Кошка'
  | 'Грызун и заяц'
  | 'Птица'
  | 'Рептилия';

export type AppointmentStatus =
  | 'Запланирован'
  | 'На приеме'
  | 'Завершен'
  | 'Отменен';

export type MedicationCategory =
  | 'Антибиотики'
  | 'Вакцины'
  | 'Противопаразитарные'
  | 'Анальгетики и НПВС'
  | 'Гастропротекторы'
  | 'Растворы и инфузии'
  | 'Дерматология и уши'
  | 'Кардиология';

export type LabTestStatus = 'Назначен' | 'В работе' | 'Готово';

export type InvoiceStatus = 'Оплачен' | 'Ожидает оплаты';

export interface Owner {
  id: string;
  fullName: string;
  phone: string;
  email: string;
  address: string;
  registeredAt: string;
  loyaltyTier: 'Стандарт' | 'Забота+' | 'VIP-полис';
}

export interface Veterinarian {
  id: string;
  fullName: string;
  specialization: string;
  cabinet: string;
  experienceYears: number;
  workDays: string;
  avatarUrl?: string;
}

export interface Pet {
  id: string;
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
  insurancePolicy?: string;
  cageNumber?: string;
  photoUrl?: string;
  status: 'Наблюдение' | 'Стационар' | 'Здоров';
}

export interface PrescriptionItem {
  medicationId: string;
  medicationName: string;
  dosage: string;
  frequency: string;
  durationDays: number;
  dispensedQty: number;
}

export interface MedicalRecord {
  id: string;
  petId: string;
  veterinarianId: string;
  visitDate: string;
  complaints: string;
  diagnosis: string;
  temperatureC: number;
  weightKg: number;
  heartRateBpm: number;
  treatmentPlan: string;
  prescriptions: PrescriptionItem[];
}

export interface Vaccination {
  id: string;
  petId: string;
  kind: 'Вакцинация' | 'Дегельминтизация' | 'Обработка от клещей';
  vaccineName: string;
  administeredDate: string;
  nextDueDate: string;
  batchNumber: string;
  veterinarianId: string;
}

export interface Appointment {
  id: string;
  petId: string;
  veterinarianId: string;
  date: string;
  timeSlot: string;
  serviceType:
    | 'Первичный осмотр'
    | 'Повторный прием'
    | 'Вакцинация'
    | 'УЗИ / Диагностика'
    | 'Хирургия / Стоматология';
  reason: string;
  status: AppointmentStatus;
  notes?: string;
}

export interface Medication {
  id: string;
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
}

export interface LabTest {
  id: string;
  petId: string;
  veterinarianId: string;
  orderedDate: string;
  testType: string;
  sampleType: string;
  status: LabTestStatus;
  resultSummary: string;
  isAbnormal: boolean;
}

export interface InpatientTask {
  id: string;
  petId: string;
  time: string;
  procedure: string;
  notes: string;
  completed: boolean;
}

export interface Invoice {
  id: string;
  ownerId: string;
  petId: string;
  date: string;
  title: string;
  amountRub: number;
  status: InvoiceStatus;
}

export interface ClinicState {
  owners: Owner[];
  veterinarians: Veterinarian[];
  pets: Pet[];
  appointments: Appointment[];
  medicalRecords: MedicalRecord[];
  vaccinations: Vaccination[];
  medications: Medication[];
  labTests: LabTest[];
  inpatientTasks: InpatientTask[];
  invoices: Invoice[];
}
