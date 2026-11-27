import {
  BedDouble,
  Calendar,
  CalendarPlus,
  FileText,
  FlaskConical,
  LayoutDashboard,
  Menu,
  Package,
  PawPrint,
  Receipt,
  RotateCcw,
  Search,
  Stethoscope,
  Syringe,
  UserCheck,
  Users,
  X,
} from 'lucide-react';
import {useCallback, useEffect, useMemo, useState} from 'react';
import {AppointmentsView} from './components/AppointmentsView.tsx';
import {InpatientView} from './components/InpatientView.tsx';
import {LabView} from './components/LabView.tsx';
import {
  AppointmentModal,
  MedicationModal,
  OwnerModal,
  PatientModal,
} from './components/Modals.tsx';
import {OverviewView, type DoctorTab} from './components/OverviewView.tsx';
import {
  OwnerPortalView,
  type OwnerSection,
} from './components/OwnerPortalView.tsx';
import {OwnersView} from './components/OwnersView.tsx';
import {PatientsView} from './components/PatientsView.tsx';
import {PharmacyView} from './components/PharmacyView.tsx';
import {PetAvatar} from './components/PetAvatar.tsx';
import {
  PrintPreviewModal,
  type PrintDocumentTarget,
} from './components/PrintPreviewModal.tsx';
import type {
  Appointment,
  AppointmentStatus,
  ClinicState,
  InvoiceStatus,
  LabTestStatus,
  MedicationCategory,
  Owner,
  Pet,
  PetSpecies,
  PrescriptionItem,
  Vaccination,
} from './types/clinic.ts';

type WorkspaceRole = 'doctor' | 'owner';

const DOCTOR_NAV: {
  id: DoctorTab;
  label: string;
  icon: typeof LayoutDashboard;
}[] = [
  {id: 'overview', label: 'Обзор смены', icon: LayoutDashboard},
  {id: 'patients', label: 'Пациенты и карты', icon: PawPrint},
  {id: 'appointments', label: 'Запись и расписание', icon: Calendar},
  {id: 'inpatient', label: 'Стационар и процедуры', icon: BedDouble},
  {id: 'lab', label: 'Лаборатория и анализы', icon: FlaskConical},
  {id: 'pharmacy', label: 'Аптека и склад', icon: Package},
  {id: 'owners', label: 'Владельцы и счета', icon: Users},
];

const OWNER_NAV: {
  id: OwnerSection;
  label: string;
  icon: typeof PawPrint;
}[] = [
  {id: 'pets', label: 'Мои питомцы', icon: PawPrint},
  {id: 'history', label: 'История болезни', icon: FileText},
  {id: 'preventive', label: 'Прививки и обработки', icon: Syringe},
  {id: 'appointments', label: 'Мои записи', icon: CalendarPlus},
  {id: 'billing', label: 'Назначения и чеки', icon: Receipt},
];

export default function App() {
  const [state, setState] = useState<ClinicState | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [role, setRole] = useState<WorkspaceRole>('doctor');
  const [doctorTab, setDoctorTab] = useState<DoctorTab>('overview');
  const [ownerSection, setOwnerSection] = useState<OwnerSection>('pets');
  const [selectedPetId, setSelectedPetId] = useState<string | null>(null);
  const [activeOwnerId, setActiveOwnerId] = useState<string>('own-1');
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const [appointmentModalPetId, setAppointmentModalPetId] = useState<
    string | null | undefined
  >(undefined);
  const [showPatientModal, setShowPatientModal] = useState(false);
  const [showOwnerModal, setShowOwnerModal] = useState(false);
  const [showMedicationModal, setShowMedicationModal] = useState(false);
  const [printTarget, setPrintTarget] = useState<PrintDocumentTarget | null>(
    null,
  );
  const [globalSearch, setGlobalSearch] = useState('');
  const [searchOpen, setSearchOpen] = useState(false);

  const fetchState = useCallback(async () => {
    try {
      setLoading(true);
      setLoadError(null);
      const res = await fetch('/api/clinic/state');
      if (!res.ok) throw new Error('Не удалось загрузить данные клиники');
      const data = (await res.json()) as ClinicState;
      setState(data);
    } catch (err) {
      setLoadError(
        err instanceof Error ? err.message : 'Ошибка подключения к серверу',
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchState();
  }, [fetchState]);

  const handleResetDemo = async () => {
    const res = await fetch('/api/clinic/reset', {method: 'POST'});
    if (res.ok) {
      const data = (await res.json()) as ClinicState;
      setState(data);
      setSelectedPetId(null);
    }
  };

  const handleBookAppointment = async (payload: {
    petId: string;
    veterinarianId: string;
    date: string;
    timeSlot: string;
    serviceType: Appointment['serviceType'];
    reason: string;
    notes?: string;
  }): Promise<string | null> => {
    const res = await fetch('/api/appointments', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    const data = await res.json();
    if (!res.ok) return data.error || 'Ошибка создания записи';
    setState(data.state);
    return null;
  };

  const handleUpdateAppointmentStatus = async (
    id: string,
    status: AppointmentStatus,
  ) => {
    const res = await fetch(`/api/appointments/${id}/status`, {
      method: 'PATCH',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify({status}),
    });
    if (res.ok) {
      const data = await res.json();
      setState(data.state);
    }
  };

  const handleCreatePatient = async (payload: {
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
  }): Promise<string | null> => {
    const res = await fetch('/api/patients', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    const data = await res.json();
    if (!res.ok) return data.error || 'Ошибка регистрации пациента';
    setState(data.state);
    setSelectedPetId(data.pet.id);
    setRole('doctor');
    setDoctorTab('patients');
    return null;
  };

  const handleUpdatePetDetails = async (
    petId: string,
    payload: Partial<
      Pick<Pet, 'status' | 'weightKg' | 'allergies' | 'diet' | 'cageNumber'>
    >,
  ) => {
    const res = await fetch(`/api/patients/${petId}`, {
      method: 'PATCH',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    if (res.ok) {
      const data = await res.json();
      setState(data.state);
    }
  };

  const handleAddMedicalRecord = async (payload: {
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
  }): Promise<string | null> => {
    const res = await fetch('/api/medical-records', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    const data = await res.json();
    if (!res.ok) return data.error || 'Ошибка сохранения осмотра';
    setState(data.state);
    return null;
  };

  const handleAddVaccination = async (payload: {
    petId: string;
    kind: Vaccination['kind'];
    vaccineName: string;
    administeredDate: string;
    nextDueDate: string;
    batchNumber: string;
    veterinarianId: string;
  }): Promise<string | null> => {
    const res = await fetch('/api/vaccinations', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    const data = await res.json();
    if (!res.ok) return data.error || 'Ошибка сохранения прививки';
    setState(data.state);
    return null;
  };

  const handleCreateOwner = async (payload: {
    fullName: string;
    phone: string;
    email: string;
    address: string;
    loyaltyTier: Owner['loyaltyTier'];
  }): Promise<string | null> => {
    const res = await fetch('/api/owners', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    const data = await res.json();
    if (!res.ok) return data.error || 'Ошибка добавления владельца';
    setState(data.state);
    return null;
  };

  const handleCreateMedication = async (payload: {
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
  }): Promise<string | null> => {
    const res = await fetch('/api/medications', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    const data = await res.json();
    if (!res.ok) return data.error || 'Ошибка добавления препарата';
    setState(data.state);
    return null;
  };

  const handleAdjustMedicationStock = async (
    medicationId: string,
    delta: number,
  ) => {
    const res = await fetch(`/api/medications/${medicationId}/stock`, {
      method: 'PATCH',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify({delta}),
    });
    if (res.ok) {
      const data = await res.json();
      setState(data.state);
    }
  };

  const handleOrderLabTest = async (payload: {
    petId: string;
    veterinarianId: string;
    testType: string;
    sampleType: string;
  }): Promise<string | null> => {
    const res = await fetch('/api/lab-tests', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    const data = await res.json();
    if (!res.ok) return data.error || 'Ошибка создания заказа';
    setState(data.state);
    return null;
  };

  const handleUpdateLabTest = async (
    id: string,
    payload: {
      status?: LabTestStatus;
      resultSummary?: string;
      isAbnormal?: boolean;
    },
  ) => {
    const res = await fetch(`/api/lab-tests/${id}`, {
      method: 'PATCH',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    if (res.ok) {
      const data = await res.json();
      setState(data.state);
    }
  };

  const handleToggleInpatientTask = async (taskId: string) => {
    const res = await fetch(`/api/inpatient-tasks/${taskId}/toggle`, {
      method: 'PATCH',
    });
    if (res.ok) {
      const data = await res.json();
      setState(data.state);
    }
  };

  const handleAddInpatientTask = async (payload: {
    petId: string;
    time: string;
    procedure: string;
    notes: string;
  }): Promise<string | null> => {
    const res = await fetch('/api/inpatient-tasks', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    const data = await res.json();
    if (!res.ok) return data.error || 'Ошибка';
    setState(data.state);
    return null;
  };

  const handleCreateInvoice = async (payload: {
    ownerId: string;
    petId: string;
    title: string;
    amountRub: number;
  }) => {
    const res = await fetch('/api/invoices', {
      method: 'POST',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify(payload),
    });
    if (res.ok) {
      const data = await res.json();
      setState(data.state);
    }
  };

  const handleUpdateInvoiceStatus = async (
    id: string,
    status: InvoiceStatus,
  ) => {
    const res = await fetch(`/api/invoices/${id}/status`, {
      method: 'PATCH',
      headers: {'Content-Type': 'application/json'},
      body: JSON.stringify({status}),
    });
    if (res.ok) {
      const data = await res.json();
      setState(data.state);
    }
  };

  const openPetRecord = (petId: string) => {
    setRole('doctor');
    setDoctorTab('patients');
    setSelectedPetId(petId);
    setMobileMenuOpen(false);
  };

  const breadcrumbText = (() => {
    if (role === 'owner') {
      const ownerName =
        state?.owners.find((o) => o.id === activeOwnerId)?.fullName ??
        'Владелец';
      const secLabel =
        OWNER_NAV.find((s) => s.id === ownerSection)?.label ?? 'Мои питомцы';
      return `ВетМед · Кабинет владельца / ${ownerName} / ${secLabel}`;
    }
    const tabLabel =
      DOCTOR_NAV.find((t) => t.id === doctorTab)?.label ?? 'Обзор смены';
    if (doctorTab === 'patients' && selectedPetId && state) {
      const petName = state.pets.find((p) => p.id === selectedPetId)?.name;
      if (petName) return `ВетМед / Пациенты / ${petName}`;
    }
    return `ВетМед / ${tabLabel}`;
  })();

  const primaryDoctor = state?.veterinarians[0];
  const currentOwner = state?.owners.find((o) => o.id === activeOwnerId);

  const quickResults = useMemo(() => {
    const q = globalSearch.trim().toLowerCase();
    if (!q || !state) return {pets: [], owners: [], meds: []};
    return {
      pets: state.pets
        .filter(
          (p) =>
            p.name.toLowerCase().includes(q) ||
            p.breed.toLowerCase().includes(q) ||
            p.microchipNumber.toLowerCase().includes(q),
        )
        .slice(0, 4),
      owners: state.owners
        .filter(
          (o) =>
            o.fullName.toLowerCase().includes(q) ||
            o.phone.toLowerCase().includes(q),
        )
        .slice(0, 3),
      meds: state.medications
        .filter(
          (m) =>
            m.name.toLowerCase().includes(q) ||
            m.activeIngredient.toLowerCase().includes(q),
        )
        .slice(0, 3),
    };
  }, [globalSearch, state]);

  const hasQuickMatches =
    quickResults.pets.length > 0 ||
    quickResults.owners.length > 0 ||
    quickResults.meds.length > 0;

  const sidebarContent = (
    <div className="flex flex-col h-full bg-slate-900 text-slate-100 border-r border-slate-800">
      <div className="h-16 px-6 flex items-center justify-between border-b border-slate-800">
        <span className="text-lg font-bold tracking-tight text-white whitespace-nowrap">
          ВетМед
        </span>
        <button
          type="button"
          onClick={() => setMobileMenuOpen(false)}
          className="lg:hidden p-1.5 text-slate-400 hover:text-white"
          aria-label="Закрыть меню"
        >
          <X size={18} />
        </button>
      </div>

      <div className="p-4 border-b border-slate-800 space-y-2">
        <div className="px-2 text-xs font-medium text-slate-400">
          Переключение роли
        </div>
        <div className="grid grid-cols-1 gap-1.5">
          <button
            type="button"
            onClick={() => {
              setRole('doctor');
              setMobileMenuOpen(false);
            }}
            className={`w-full flex items-center gap-2.5 px-3 py-2 text-xs font-medium rounded-lg transition-colors whitespace-nowrap ${
              role === 'doctor'
                ? 'bg-teal-600 text-white'
                : 'text-slate-300 hover:bg-slate-800 hover:text-white'
            }`}
          >
            <Stethoscope size={15} />
            Рабочее место врача
          </button>
          <button
            type="button"
            onClick={() => {
              setRole('owner');
              setMobileMenuOpen(false);
            }}
            className={`w-full flex items-center gap-2.5 px-3 py-2 text-xs font-medium rounded-lg transition-colors whitespace-nowrap ${
              role === 'owner'
                ? 'bg-teal-600 text-white'
                : 'text-slate-300 hover:bg-slate-800 hover:text-white'
            }`}
          >
            <UserCheck size={15} />
            Кабинет владельца
          </button>
        </div>
      </div>

      <nav className="flex-1 p-4 space-y-1 overflow-y-auto">
        {role === 'doctor' ? (
          <>
            <div className="px-2 text-xs font-medium text-slate-400 mb-2">
              Отделения и модули
            </div>
            {DOCTOR_NAV.map((item) => {
              const Icon = item.icon;
              const isActive = doctorTab === item.id;
              return (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => {
                    setDoctorTab(item.id);
                    if (item.id !== 'patients') setSelectedPetId(null);
                    setMobileMenuOpen(false);
                  }}
                  className={`w-full flex items-center gap-2.5 px-3 py-2.5 text-sm font-medium rounded-lg transition-colors whitespace-nowrap ${
                    isActive
                      ? 'bg-slate-800 text-white'
                      : 'text-slate-300 hover:text-white hover:bg-slate-800/60'
                  }`}
                >
                  <Icon
                    size={16}
                    className={isActive ? 'text-teal-400' : 'text-slate-400'}
                  />
                  {item.label}
                </button>
              );
            })}
          </>
        ) : (
          <>
            <div className="px-2 text-xs font-medium text-slate-400 mb-2">
              Личный кабинет
            </div>
            {OWNER_NAV.map((item) => {
              const Icon = item.icon;
              const isActive = ownerSection === item.id;
              return (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => {
                    setOwnerSection(item.id);
                    setMobileMenuOpen(false);
                  }}
                  className={`w-full flex items-center gap-2.5 px-3 py-2.5 text-sm font-medium rounded-lg transition-colors whitespace-nowrap ${
                    isActive
                      ? 'bg-slate-800 text-white'
                      : 'text-slate-300 hover:text-white hover:bg-slate-800/60'
                  }`}
                >
                  <Icon
                    size={16}
                    className={isActive ? 'text-teal-400' : 'text-slate-400'}
                  />
                  {item.label}
                </button>
              );
            })}

            {state && (
              <div className="pt-4 mt-4 border-t border-slate-800 px-2 space-y-2">
                <div className="text-xs text-slate-400">Питомцы профиля</div>
                <div className="space-y-1.5">
                  {state.pets
                    .filter((p) => p.ownerId === activeOwnerId)
                    .map((pet) => (
                      <button
                        key={pet.id}
                        type="button"
                        onClick={() => {
                          setOwnerSection('pets');
                          setMobileMenuOpen(false);
                        }}
                        className="w-full text-left px-2.5 py-1.5 rounded-md bg-slate-800/70 hover:bg-slate-800 text-xs text-slate-200 flex items-center justify-between"
                      >
                        <span className="truncate font-medium">{pet.name}</span>
                        <span className="text-slate-400 text-[11px] shrink-0">
                          {pet.species}
                        </span>
                      </button>
                    ))}
                </div>
              </div>
            )}
          </>
        )}
      </nav>

      <div className="p-4 border-t border-slate-800 space-y-3">
        {role === 'doctor' && primaryDoctor && (
          <div className="flex items-center gap-3">
            <PetAvatar
              src={primaryDoctor.avatarUrl}
              alt={primaryDoctor.fullName}
              size="sm"
              variant="doctor"
            />
            <div className="min-w-0">
              <div className="text-xs font-semibold text-white truncate">
                {primaryDoctor.fullName}
              </div>
              <div className="text-xs text-slate-400 truncate">
                {primaryDoctor.cabinet}
              </div>
            </div>
          </div>
        )}

        {role === 'owner' && currentOwner && (
          <div className="px-1">
            <div className="text-xs font-semibold text-white truncate">
              {currentOwner.fullName}
            </div>
            <div className="text-xs text-slate-400 truncate">
              {currentOwner.loyaltyTier}
            </div>
          </div>
        )}

        <button
          type="button"
          onClick={handleResetDemo}
          className="w-full inline-flex items-center justify-center gap-1.5 px-3 py-1.5 text-xs font-medium text-slate-300 border border-slate-700 rounded-lg hover:bg-slate-800 hover:text-white transition-colors"
        >
          <RotateCcw size={13} />
          Сбросить базу данных
        </button>
      </div>
    </div>
  );

  return (
    <>
      <div
        className={`min-h-screen flex bg-[#F4F7F9] text-slate-900 ${
          printTarget ? 'print:hidden' : ''
        }`}
      >
        <aside className="hidden lg:block w-64 shrink-0 sticky top-0 h-screen">
          {sidebarContent}
        </aside>

        {mobileMenuOpen && (
          <div className="fixed inset-0 z-50 lg:hidden flex">
            <div className="w-64 h-full">{sidebarContent}</div>
            <div
              className="flex-1 bg-slate-900/50"
              onClick={() => setMobileMenuOpen(false)}
            />
          </div>
        )}

        <div className="flex-1 flex flex-col min-w-0">
          <header className="h-16 bg-white border-b border-slate-200 px-6 flex items-center justify-between gap-8 sticky top-0 z-30">
            <div className="flex items-center gap-3 min-w-0">
              <button
                type="button"
                onClick={() => setMobileMenuOpen(true)}
                className="lg:hidden p-2 -ml-2 text-slate-600 hover:text-slate-900"
                aria-label="Открыть навигацию"
              >
                <Menu size={20} />
              </button>
              <div className="text-sm font-medium text-slate-700 truncate">
                {breadcrumbText}
              </div>
            </div>

            <div className="hidden md:block relative flex-1 max-w-sm">
              <Search
                size={15}
                className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none"
              />
              <input
                type="text"
                value={globalSearch}
                onFocus={() => setSearchOpen(true)}
                onBlur={() => setTimeout(() => setSearchOpen(false), 160)}
                onChange={(e) => {
                  setGlobalSearch(e.target.value);
                  setSearchOpen(true);
                }}
                placeholder="Быстрый поиск: питомец, чип, владелец..."
                className="w-full h-9 pl-9 pr-3 rounded-lg border border-slate-200 bg-slate-50 text-xs text-slate-900 focus:bg-white focus:outline-none focus:border-teal-700 transition-colors"
              />

              {searchOpen && globalSearch.trim().length > 0 && (
                <div className="absolute left-0 right-0 top-11 bg-white border border-slate-200 rounded-xl shadow-lg py-2 z-40 max-h-80 overflow-y-auto">
                  {!hasQuickMatches ? (
                    <div className="px-4 py-3 text-xs text-slate-500">
                      Совпадений не найдено
                    </div>
                  ) : (
                    <div className="divide-y divide-slate-100 text-xs">
                      {quickResults.pets.length > 0 && (
                        <div className="py-1">
                          <div className="px-3 py-1 text-[11px] font-medium text-slate-400">
                            Пациенты
                          </div>
                          {quickResults.pets.map((pet) => (
                            <button
                              key={pet.id}
                              type="button"
                              onMouseDown={() => {
                                openPetRecord(pet.id);
                                setGlobalSearch('');
                                setSearchOpen(false);
                              }}
                              className="w-full px-3 py-1.5 text-left hover:bg-slate-50 flex items-center justify-between gap-2"
                            >
                              <span className="font-medium text-slate-900">
                                {pet.name} ({pet.species})
                              </span>
                              <span className="font-mono text-[11px] text-slate-400 tabular-nums">
                                {pet.microchipNumber}
                              </span>
                            </button>
                          ))}
                        </div>
                      )}
                      {quickResults.owners.length > 0 && (
                        <div className="py-1">
                          <div className="px-3 py-1 text-[11px] font-medium text-slate-400">
                            Владельцы
                          </div>
                          {quickResults.owners.map((owner) => (
                            <button
                              key={owner.id}
                              type="button"
                              onMouseDown={() => {
                                setActiveOwnerId(owner.id);
                                setRole('owner');
                                setOwnerSection('pets');
                                setGlobalSearch('');
                                setSearchOpen(false);
                              }}
                              className="w-full px-3 py-1.5 text-left hover:bg-slate-50 flex items-center justify-between gap-2"
                            >
                              <span className="font-medium text-slate-900">
                                {owner.fullName}
                              </span>
                              <span className="text-[11px] text-slate-500 tabular-nums">
                                {owner.phone}
                              </span>
                            </button>
                          ))}
                        </div>
                      )}
                      {quickResults.meds.length > 0 && (
                        <div className="py-1">
                          <div className="px-3 py-1 text-[11px] font-medium text-slate-400">
                            Аптека
                          </div>
                          {quickResults.meds.map((med) => (
                            <button
                              key={med.id}
                              type="button"
                              onMouseDown={() => {
                                setRole('doctor');
                                setDoctorTab('pharmacy');
                                setGlobalSearch('');
                                setSearchOpen(false);
                              }}
                              className="w-full px-3 py-1.5 text-left hover:bg-slate-50 flex items-center justify-between gap-2"
                            >
                              <span className="font-medium text-slate-900">
                                {med.name}
                              </span>
                              <span className="font-mono text-[11px] text-slate-500 tabular-nums">
                                {med.stockQty} {med.unit}
                              </span>
                            </button>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className="shrink-0">
              <button
                type="button"
                onClick={() => setAppointmentModalPetId(null)}
                disabled={!state || state.pets.length === 0}
                className="px-4 py-2 text-xs font-medium text-white bg-teal-700 rounded-lg hover:bg-teal-800 disabled:opacity-50 transition-colors whitespace-nowrap shrink-0"
              >
                Записать на прием
              </button>
            </div>
          </header>

        <main className="flex-1 p-6 lg:p-8 max-w-7xl w-full mx-auto">
          {loading && !state && (
            <div className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                {[0, 1, 2, 3].map((i) => (
                  <div
                    key={i}
                    className="h-24 bg-white border border-slate-200 rounded-xl animate-pulse"
                  />
                ))}
              </div>
              <div className="h-72 bg-white border border-slate-200 rounded-xl animate-pulse" />
            </div>
          )}

          {loadError && (
            <div className="bg-white border border-red-200 rounded-xl p-6 text-sm text-red-700 flex items-center justify-between gap-4">
              <span>{loadError}</span>
              <button
                type="button"
                onClick={fetchState}
                className="px-3.5 py-2 text-xs font-medium text-white bg-red-700 rounded-lg hover:bg-red-800"
              >
                Повторить
              </button>
            </div>
          )}

          {state && role === 'doctor' && doctorTab === 'overview' && (
            <OverviewView
              state={state}
              onSelectPet={openPetRecord}
              onNavigate={(tab) => setDoctorTab(tab)}
              onUpdateAppointmentStatus={handleUpdateAppointmentStatus}
              onOpenNewAppointment={() => setAppointmentModalPetId(null)}
            />
          )}

          {state && role === 'doctor' && doctorTab === 'patients' && (
            <PatientsView
              state={state}
              selectedPetId={selectedPetId}
              onSelectPet={setSelectedPetId}
              onOpenNewPatient={() => setShowPatientModal(true)}
              onOpenNewAppointment={(petId) =>
                setAppointmentModalPetId(petId ?? null)
              }
              onAddMedicalRecord={handleAddMedicalRecord}
              onAddVaccination={handleAddVaccination}
              onUpdatePetDetails={handleUpdatePetDetails}
              onPrintExtract={(petId) =>
                setPrintTarget({kind: 'pet-extract', petId})
              }
            />
          )}

          {state && role === 'doctor' && doctorTab === 'appointments' && (
            <AppointmentsView
              state={state}
              onOpenNewAppointment={() => setAppointmentModalPetId(null)}
              onUpdateStatus={handleUpdateAppointmentStatus}
              onSelectPet={openPetRecord}
            />
          )}

          {state && role === 'doctor' && doctorTab === 'inpatient' && (
            <InpatientView
              state={state}
              onToggleTask={handleToggleInpatientTask}
              onAddTask={handleAddInpatientTask}
              onSelectPet={openPetRecord}
            />
          )}

          {state && role === 'doctor' && doctorTab === 'lab' && (
            <LabView
              state={state}
              onOrderLabTest={handleOrderLabTest}
              onUpdateLabTest={handleUpdateLabTest}
              onSelectPet={openPetRecord}
            />
          )}

          {state && role === 'doctor' && doctorTab === 'pharmacy' && (
            <PharmacyView
              state={state}
              onAdjustStock={handleAdjustMedicationStock}
              onOpenNewMedication={() => setShowMedicationModal(true)}
            />
          )}

          {state && role === 'doctor' && doctorTab === 'owners' && (
            <OwnersView
              state={state}
              onOpenNewOwner={() => setShowOwnerModal(true)}
              onSelectPet={openPetRecord}
              onOpenOwnerPortal={(ownerId) => {
                setActiveOwnerId(ownerId);
                setRole('owner');
                setOwnerSection('pets');
              }}
              onCreateInvoice={handleCreateInvoice}
              onUpdateInvoiceStatus={handleUpdateInvoiceStatus}
              onPrintInvoice={(invoiceId) =>
                setPrintTarget({kind: 'invoice', invoiceId})
              }
            />
          )}

          {state && role === 'owner' && (
            <OwnerPortalView
              state={state}
              activeOwnerId={activeOwnerId}
              activeSection={ownerSection}
              onChangeOwner={setActiveOwnerId}
              onBookAppointment={(petId) =>
                setAppointmentModalPetId(petId ?? null)
              }
              onCancelAppointment={handleUpdateAppointmentStatus}
              onPayInvoice={handleUpdateInvoiceStatus}
            />
          )}
        </main>
      </div>

      {state && appointmentModalPetId !== undefined && (
        <AppointmentModal
          pets={
            role === 'owner'
              ? state.pets.filter((p) => p.ownerId === activeOwnerId)
              : state.pets
          }
          owners={state.owners}
          veterinarians={state.veterinarians}
          appointments={state.appointments}
          defaultPetId={appointmentModalPetId ?? undefined}
          onClose={() => setAppointmentModalPetId(undefined)}
          onSubmit={handleBookAppointment}
        />
      )}

      {state && showPatientModal && (
        <PatientModal
          owners={state.owners}
          onClose={() => setShowPatientModal(false)}
          onSubmit={handleCreatePatient}
        />
      )}

      {showOwnerModal && (
        <OwnerModal
          onClose={() => setShowOwnerModal(false)}
          onSubmit={handleCreateOwner}
        />
      )}

      {showMedicationModal && (
        <MedicationModal
          onClose={() => setShowMedicationModal(false)}
          onSubmit={handleCreateMedication}
        />
      )}
    </div>

      {state && printTarget && (
        <PrintPreviewModal
          target={printTarget}
          state={state}
          onClose={() => setPrintTarget(null)}
        />
      )}
    </>
  );
}
