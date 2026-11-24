import express from 'express';
import fs from 'fs';
import path from 'path';
import {fileURLToPath} from 'url';
import {createServer as createViteServer} from 'vite';
import {INITIAL_CLINIC_STATE} from './src/data/seedClinic.ts';
import type {
  Appointment,
  AppointmentStatus,
  ClinicState,
  Invoice,
  InvoiceStatus,
  LabTest,
  LabTestStatus,
  MedicalRecord,
  Medication,
  Owner,
  Pet,
  Vaccination,
} from './src/types/clinic.ts';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const DATA_DIR = path.resolve(__dirname, 'data');
const DB_FILE = path.join(DATA_DIR, 'clinic-db.json');

function ensureDb(): ClinicState {
  if (!fs.existsSync(DATA_DIR)) {
    fs.mkdirSync(DATA_DIR, {recursive: true});
  }
  if (!fs.existsSync(DB_FILE)) {
    fs.writeFileSync(DB_FILE, JSON.stringify(INITIAL_CLINIC_STATE, null, 2), 'utf-8');
    return structuredClone(INITIAL_CLINIC_STATE);
  }
  try {
    const raw = fs.readFileSync(DB_FILE, 'utf-8');
    const parsed = JSON.parse(raw) as Partial<ClinicState>;
    // Upgrade legacy clinic-db.json seamlessly if new collections are missing
    if (!parsed.labTests || !parsed.inpatientTasks || !parsed.invoices) {
      const upgraded = structuredClone(INITIAL_CLINIC_STATE);
      fs.writeFileSync(DB_FILE, JSON.stringify(upgraded, null, 2), 'utf-8');
      return upgraded;
    }
    return parsed as ClinicState;
  } catch {
    fs.writeFileSync(DB_FILE, JSON.stringify(INITIAL_CLINIC_STATE, null, 2), 'utf-8');
    return structuredClone(INITIAL_CLINIC_STATE);
  }
}

function saveDb(state: ClinicState): void {
  if (!fs.existsSync(DATA_DIR)) {
    fs.mkdirSync(DATA_DIR, {recursive: true});
  }
  fs.writeFileSync(DB_FILE, JSON.stringify(state, null, 2), 'utf-8');
}

async function startServer() {
  const app = express();
  app.use(express.json({limit: '2mb'}));

  app.get('/api/clinic/state', (_req, res) => {
    const state = ensureDb();
    res.json(state);
  });

  app.post('/api/clinic/reset', (_req, res) => {
    const fresh = structuredClone(INITIAL_CLINIC_STATE);
    saveDb(fresh);
    res.json(fresh);
  });

  app.post('/api/owners', (req, res) => {
    const state = ensureDb();
    const {fullName, phone, email, address, loyaltyTier} =
      req.body as Partial<Owner>;
    if (!fullName?.trim() || !phone?.trim()) {
      res.status(400).json({error: 'Укажите ФИО и телефон владельца'});
      return;
    }
    const owner: Owner = {
      id: `own-${Date.now()}`,
      fullName: fullName.trim(),
      phone: phone.trim(),
      email: (email ?? '').trim(),
      address: (address ?? '').trim(),
      registeredAt: new Date().toISOString().slice(0, 10),
      loyaltyTier: loyaltyTier ?? 'Стандарт',
    };
    state.owners.unshift(owner);
    saveDb(state);
    res.status(201).json({owner, state});
  });

  app.post('/api/patients', (req, res) => {
    const state = ensureDb();
    const body = req.body as Partial<Pet>;
    if (!body.name?.trim() || !body.ownerId || !body.species) {
      res.status(400).json({error: 'Заполните кличку, вид и владельца питомца'});
      return;
    }
    const pet: Pet = {
      id: `pet-${Date.now()}`,
      ownerId: body.ownerId,
      name: body.name.trim(),
      species: body.species,
      breed: (body.breed ?? 'Метис').trim(),
      color: (body.color ?? 'Не указан').trim(),
      gender: body.gender ?? 'Самец',
      sterilized: Boolean(body.sterilized),
      birthDate: body.birthDate || '2024-01-01',
      weightKg: Number(body.weightKg) || 1,
      bloodType: (body.bloodType ?? 'Не типируется').trim(),
      microchipNumber:
        (body.microchipNumber ?? '').trim() ||
        `643098100${Math.floor(100000 + Math.random() * 899999)}`,
      allergies: (body.allergies ?? 'Не выявлены').trim() || 'Не выявлены',
      diet: (body.diet ?? 'Стандартный рацион').trim(),
      insurancePolicy: body.insurancePolicy?.trim(),
      cageNumber: body.cageNumber?.trim(),
      photoUrl: body.photoUrl,
      status: body.status ?? 'Здоров',
    };
    state.pets.unshift(pet);
    saveDb(state);
    res.status(201).json({pet, state});
  });

  app.patch('/api/patients/:id', (req, res) => {
    const state = ensureDb();
    const pet = state.pets.find((p) => p.id === req.params.id);
    if (!pet) {
      res.status(404).json({error: 'Пациент не найден'});
      return;
    }
    const {weightKg, status, allergies, diet, cageNumber} =
      req.body as Partial<Pet>;
    if (typeof weightKg === 'number' && weightKg > 0) pet.weightKg = weightKg;
    if (status) pet.status = status;
    if (typeof allergies === 'string') pet.allergies = allergies;
    if (typeof diet === 'string') pet.diet = diet;
    if (typeof cageNumber === 'string') pet.cageNumber = cageNumber;
    saveDb(state);
    res.json({pet, state});
  });

  app.post('/api/appointments', (req, res) => {
    const state = ensureDb();
    const {petId, veterinarianId, date, timeSlot, serviceType, reason, notes} =
      req.body as Partial<Appointment>;
    if (!petId || !veterinarianId || !date || !timeSlot || !reason?.trim()) {
      res.status(400).json({error: 'Заполните все поля записи на прием'});
      return;
    }
    const conflict = state.appointments.find(
      (a) =>
        a.veterinarianId === veterinarianId &&
        a.date === date &&
        a.timeSlot === timeSlot &&
        a.status !== 'Отменен',
    );
    if (conflict) {
      res.status(409).json({
        error: 'Выбранное время у данного врача уже занято',
      });
      return;
    }
    const appointment: Appointment = {
      id: `apt-${Date.now()}`,
      petId,
      veterinarianId,
      date,
      timeSlot,
      serviceType: serviceType ?? 'Первичный осмотр',
      reason: reason.trim(),
      status: 'Запланирован',
      notes: notes?.trim(),
    };
    state.appointments.unshift(appointment);
    saveDb(state);
    res.status(201).json({appointment, state});
  });

  app.patch('/api/appointments/:id/status', (req, res) => {
    const state = ensureDb();
    const apt = state.appointments.find((a) => a.id === req.params.id);
    if (!apt) {
      res.status(404).json({error: 'Запись не найдена'});
      return;
    }
    const {status} = req.body as {status: AppointmentStatus};
    if (!status) {
      res.status(400).json({error: 'Не указан новый статус'});
      return;
    }
    apt.status = status;
    saveDb(state);
    res.json({appointment: apt, state});
  });

  app.post('/api/medical-records', (req, res) => {
    const state = ensureDb();
    const body = req.body as Partial<MedicalRecord>;
    if (!body.petId || !body.veterinarianId || !body.diagnosis?.trim()) {
      res.status(400).json({error: 'Укажите пациента, врача и диагноз'});
      return;
    }
    const pet = state.pets.find((p) => p.id === body.petId);
    if (!pet) {
      res.status(404).json({error: 'Пациент не найден'});
      return;
    }

    const prescriptions = Array.isArray(body.prescriptions) ? body.prescriptions : [];
    for (const rx of prescriptions) {
      if (rx.dispensedQty > 0) {
        const med = state.medications.find((m) => m.id === rx.medicationId);
        if (med) {
          med.stockQty = Math.max(0, med.stockQty - rx.dispensedQty);
        }
      }
    }

    const weightVal = Number(body.weightKg) || pet.weightKg;
    pet.weightKg = weightVal;

    const record: MedicalRecord = {
      id: `rec-${Date.now()}`,
      petId: body.petId,
      veterinarianId: body.veterinarianId,
      visitDate: body.visitDate || new Date().toISOString().slice(0, 10),
      complaints: (body.complaints ?? '').trim() || 'Плановый осмотр',
      diagnosis: body.diagnosis.trim(),
      temperatureC: Number(body.temperatureC) || 38.5,
      weightKg: weightVal,
      heartRateBpm: Number(body.heartRateBpm) || 95,
      treatmentPlan: (body.treatmentPlan ?? '').trim() || 'Наблюдение в динамике',
      prescriptions,
    };

    state.medicalRecords.unshift(record);
    saveDb(state);
    res.status(201).json({record, state});
  });

  app.post('/api/vaccinations', (req, res) => {
    const state = ensureDb();
    const body = req.body as Partial<Vaccination>;
    if (!body.petId || !body.vaccineName?.trim() || !body.veterinarianId) {
      res.status(400).json({error: 'Укажите препарат, пациента и врача'});
      return;
    }
    const vaccination: Vaccination = {
      id: `vac-${Date.now()}`,
      petId: body.petId,
      kind: body.kind ?? 'Вакцинация',
      vaccineName: body.vaccineName.trim(),
      administeredDate: body.administeredDate || new Date().toISOString().slice(0, 10),
      nextDueDate: body.nextDueDate || new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10),
      batchNumber: (body.batchNumber ?? '').trim() || `VAC-${Math.floor(1000 + Math.random() * 9000)}`,
      veterinarianId: body.veterinarianId,
    };
    state.vaccinations.unshift(vaccination);
    saveDb(state);
    res.status(201).json({vaccination, state});
  });

  app.post('/api/medications', (req, res) => {
    const state = ensureDb();
    const body = req.body as Partial<Medication>;
    if (!body.name?.trim() || !body.activeIngredient?.trim() || !body.category) {
      res.status(400).json({error: 'Укажите название, действующее вещество и категорию'});
      return;
    }
    const medication: Medication = {
      id: `med-${Date.now()}`,
      name: body.name.trim(),
      activeIngredient: body.activeIngredient.trim(),
      manufacturer: (body.manufacturer ?? 'ВетФарм').trim(),
      category: body.category,
      dosageForm: (body.dosageForm ?? 'Упаковка').trim(),
      unit: (body.unit ?? 'уп.').trim(),
      stockQty: Math.max(0, Number(body.stockQty) || 0),
      minThreshold: Math.max(1, Number(body.minThreshold) || 5),
      storageCondition: (body.storageCondition ?? 'До +25 °C').trim(),
      batchNumber: (body.batchNumber ?? '').trim() || `LOT-${Math.floor(1000 + Math.random() * 9000)}`,
      prescriptionRequired: Boolean(body.prescriptionRequired),
      expiryDate: body.expiryDate || '2028-01-01',
      unitPriceRub: Math.max(0, Number(body.unitPriceRub) || 0),
    };
    state.medications.unshift(medication);
    saveDb(state);
    res.status(201).json({medication, state});
  });

  app.patch('/api/medications/:id/stock', (req, res) => {
    const state = ensureDb();
    const med = state.medications.find((m) => m.id === req.params.id);
    if (!med) {
      res.status(404).json({error: 'Препарат не найден'});
      return;
    }
    const {delta} = req.body as {delta: number};
    med.stockQty = Math.max(0, med.stockQty + Number(delta || 0));
    saveDb(state);
    res.json({medication: med, state});
  });

  app.post('/api/lab-tests', (req, res) => {
    const state = ensureDb();
    const body = req.body as Partial<LabTest>;
    if (!body.petId || !body.veterinarianId || !body.testType?.trim()) {
      res.status(400).json({error: 'Укажите пациента, врача и вид исследования'});
      return;
    }
    const labTest: LabTest = {
      id: `lab-${Date.now()}`,
      petId: body.petId,
      veterinarianId: body.veterinarianId,
      orderedDate: new Date().toISOString().slice(0, 10),
      testType: body.testType.trim(),
      sampleType: (body.sampleType ?? 'Биоматериал').trim(),
      status: 'Назначен',
      resultSummary: (body.resultSummary ?? 'Ожидает забора или обработки').trim(),
      isAbnormal: false,
    };
    state.labTests.unshift(labTest);
    saveDb(state);
    res.status(201).json({labTest, state});
  });

  app.patch('/api/lab-tests/:id', (req, res) => {
    const state = ensureDb();
    const lab = state.labTests.find((l) => l.id === req.params.id);
    if (!lab) {
      res.status(404).json({error: 'Исследование не найдено'});
      return;
    }
    const {status, resultSummary, isAbnormal} = req.body as {
      status?: LabTestStatus;
      resultSummary?: string;
      isAbnormal?: boolean;
    };
    if (status) lab.status = status;
    if (typeof resultSummary === 'string') lab.resultSummary = resultSummary;
    if (typeof isAbnormal === 'boolean') lab.isAbnormal = isAbnormal;
    saveDb(state);
    res.json({labTest: lab, state});
  });

  app.post('/api/inpatient-tasks', (req, res) => {
    const state = ensureDb();
    const {petId, time, procedure, notes} = req.body as {
      petId?: string;
      time?: string;
      procedure?: string;
      notes?: string;
    };
    if (!petId || !procedure?.trim()) {
      res.status(400).json({error: 'Укажите пациента и процедуру'});
      return;
    }
    state.inpatientTasks.unshift({
      id: `inp-${Date.now()}`,
      petId,
      time: time || '12:00',
      procedure: procedure.trim(),
      notes: (notes ?? '').trim(),
      completed: false,
    });
    saveDb(state);
    res.status(201).json({state});
  });

  app.patch('/api/inpatient-tasks/:id/toggle', (req, res) => {
    const state = ensureDb();
    const task = state.inpatientTasks.find((t) => t.id === req.params.id);
    if (!task) {
      res.status(404).json({error: 'Задача не найдена'});
      return;
    }
    task.completed = !task.completed;
    saveDb(state);
    res.json({task, state});
  });

  app.post('/api/invoices', (req, res) => {
    const state = ensureDb();
    const body = req.body as Partial<Invoice>;
    if (!body.ownerId || !body.petId || !body.title?.trim()) {
      res.status(400).json({error: 'Укажите владельца, питомца и описание счета'});
      return;
    }
    const invoice: Invoice = {
      id: `inv-${Date.now()}`,
      ownerId: body.ownerId,
      petId: body.petId,
      date: new Date().toISOString().slice(0, 10),
      title: body.title.trim(),
      amountRub: Math.max(0, Number(body.amountRub) || 0),
      status: 'Ожидает оплаты',
    };
    state.invoices.unshift(invoice);
    saveDb(state);
    res.status(201).json({invoice, state});
  });

  app.patch('/api/invoices/:id/status', (req, res) => {
    const state = ensureDb();
    const inv = state.invoices.find((i) => i.id === req.params.id);
    if (!inv) {
      res.status(404).json({error: 'Счет не найден'});
      return;
    }
    const {status} = req.body as {status: InvoiceStatus};
    if (status) inv.status = status;
    saveDb(state);
    res.json({invoice: inv, state});
  });

  if (process.env.NODE_ENV !== 'production') {
    const vite = await createViteServer({
      server: {middlewareMode: true},
      appType: 'spa',
    });
    app.use(vite.middlewares);
  } else {
    const distPath = path.resolve(__dirname, 'dist');
    app.use(express.static(distPath));
    app.get('*', (_req, res) => {
      res.sendFile(path.join(distPath, 'index.html'));
    });
  }

  const PORT = 3000;
  app.listen(PORT, '0.0.0.0', () => {
    console.log(`VetMed server listening on http://localhost:${PORT}`);
  });
}

startServer();
