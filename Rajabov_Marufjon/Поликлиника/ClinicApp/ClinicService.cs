using System.Text.Json;

public sealed class ClinicService
{
    private readonly string _filePath;
    private readonly ClinicData _data;
    private int _nextPatientId;
    private int _nextAppointmentId;

    public IReadOnlyList<Patient> Patients => _data.Patients;
    public IReadOnlyList<Doctor> Doctors => _data.Doctors;
    public IReadOnlyList<Appointment> Appointments => _data.Appointments;

    public ClinicService(string filePath)
    {
        _filePath = filePath;

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                _data = JsonSerializer.Deserialize<ClinicData>(json) ?? new ClinicData();
            }
            catch (JsonException)
            {
                _data = new ClinicData();
                Console.WriteLine("Файл данных повреждён. Создан новый набор данных.");
            }
        }
        else
        {
            _data = new ClinicData();
        }

        if (_data.Doctors.Count == 0)
        {
            AddDefaultDoctors();
        }

        if (_data.Patients.Count == 0)
        {
            AddDefaultPatients();
        }

        if (_data.Appointments.Count == 0)
        {
            AddDefaultAppointments();
        }

        _nextPatientId = _data.Patients.Select(patient => patient.Id).DefaultIfEmpty(0).Max() + 1;
        _nextAppointmentId = _data.Appointments.Select(appointment => appointment.Id).DefaultIfEmpty(0).Max() + 1;
        Save();
    }

    private void AddDefaultDoctors()
    {
        _data.Doctors.Add(new Doctor
        {
            Id = 1,
            FullName = "Иванов Иван Иванович",
            Specialty = "Терапевт",
            Cabinet = 101
        });
        _data.Doctors.Add(new Doctor
        {
            Id = 2,
            FullName = "Петров Пётр Петрович",
            Specialty = "Хирург",
            Cabinet = 102
        });
        _data.Doctors.Add(new Doctor
        {
            Id = 3,
            FullName = "Сидоров Сидор Сидорович",
            Specialty = "Кардиолог",
            Cabinet = 103
        });
        _data.Doctors.Add(new Doctor
        {
            Id = 4,
            FullName = "Кузнецова Анна Сергеевна",
            Specialty = "Педиатр",
            Cabinet = 104
        });
    }


    private void AddDefaultPatients()
    {
        _data.Patients.Add(new Patient
        {
            Id = 1,
            FullName = "Александров Александр Александрович",
            Phone = "1234567890",
            BirthDate = new DateOnly(2000, 1, 10)
        });
        _data.Patients.Add(new Patient
        {
            Id = 2,
            FullName = "Смирнова Мария Ивановна",
            Phone = "0987654321",
            BirthDate = new DateOnly(1990, 6, 21)
        });
        _data.Patients.Add(new Patient
        {
            Id = 3,
            FullName = "Козлов Дмитрий Сергеевич",
            Phone = "1112223333",
            BirthDate = new DateOnly(1999, 7, 5)
        });
        _data.Patients.Add(new Patient
        {
            Id = 4,
            FullName = "Васильева Елена Петровна",
            Phone = "4445556666",
            BirthDate = new DateOnly(1995, 12, 3)
        });
    }

    private void AddDefaultAppointments()
    {
        DateOnly appointmentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        _data.Appointments.Add(new Appointment
        {
            Id = 1,
            PatientId = 1,
            DoctorId = 1,
            Date = appointmentDate,
            Hour = 9
        });
        _data.Appointments.Add(new Appointment
        {
            Id = 2,
            PatientId = 2,
            DoctorId = 3,
            Date = appointmentDate,
            Hour = 10
        });
    }
    public void Save()
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        string json = JsonSerializer.Serialize(_data, options);
        File.WriteAllText(_filePath, json);
    }

    public IEnumerable<Patient> SearchPatients(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Patients;
        }

        return Patients.Where(patient =>
            patient.FullName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            patient.Phone.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    public Patient RegisterPatient(string name, string phone, DateOnly birthDate)
    {
        name = name.Trim();
        phone = phone.Trim();

        if (name.Length < 5)
        {
            throw new ArgumentException("Укажите ФИО полностью.");
        }

        if (phone.Length < 5 || !phone.Any(char.IsDigit))
        {
            throw new ArgumentException("Укажите корректный телефон.");
        }

        if (birthDate > DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException("Дата рождения не может быть в будущем.");
        }

        bool phoneAlreadyExists = Patients.Any(patient => patient.Phone == phone);
        if (phoneAlreadyExists)
        {
            throw new ArgumentException("Пациент с таким телефоном уже зарегистрирован.");
        }

        Patient patient = new Patient
        {
            Id = _nextPatientId,
            FullName = name,
            Phone = phone,
            BirthDate = birthDate
        };
        _nextPatientId++;

        _data.Patients.Add(patient);
        Save();
        return patient;
    }

    public List<int> GetFreeSlots(int doctorId, DateOnly date)
    {
        bool doctorExists = Doctors.Any(doctor => doctor.Id == doctorId);
        if (!doctorExists)
        {
            throw new ArgumentException("Врач не найден.");
        }

        if (date < DateOnly.FromDateTime(DateTime.Today))
        {
            return new List<int>();
        }

        List<int> freeSlots = new List<int>();
        for (int hour = 9; hour < 16; hour++)
        {
            bool slotIsBusy = Appointments.Any(appointment =>
                !appointment.IsCancelled &&
                appointment.DoctorId == doctorId &&
                appointment.Date == date &&
                appointment.Hour == hour);

            if (!slotIsBusy)
            {
                freeSlots.Add(hour);
            }
        }

        return freeSlots;
    }

    public Appointment Book(int patientId, int doctorId, DateOnly date, int hour)
    {
        bool patientExists = Patients.Any(patient => patient.Id == patientId);
        if (!patientExists)
        {
            throw new ArgumentException("Пациент не найден.");
        }

        if (date < DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException("Нельзя записаться на прошедшую дату.");
        }

        List<int> freeSlots = GetFreeSlots(doctorId, date);
        if (!freeSlots.Contains(hour))
        {
            throw new ArgumentException("Это время недоступно.");
        }

        Appointment appointment = new Appointment
        {
            Id = _nextAppointmentId,
            PatientId = patientId,
            DoctorId = doctorId,
            Date = date,
            Hour = hour
        };
        _nextAppointmentId++;

        _data.Appointments.Add(appointment);
        Save();
        return appointment;
    }

    public void Cancel(int appointmentId)
    {
        Appointment? appointment = Appointments.FirstOrDefault(item =>
            item.Id == appointmentId && !item.IsCancelled);

        if (appointment == null)
        {
            throw new ArgumentException("Активная запись не найдена.");
        }

        appointment.IsCancelled = true;
        Save();
    }
}


