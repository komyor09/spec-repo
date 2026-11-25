using System.Globalization;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

string dataFile = Path.Combine(AppContext.BaseDirectory, "clinic-data.json");
ClinicService clinic = new ClinicService(dataFile);

string[] menuItems =
{
    "Список врачей",
    "Пациенты и поиск",
    "Зарегистрировать пациента",
    "Записать пациента на приём",
    "Список записей",
    "Отменить запись",
    "Выход"
};

bool isRunning = true;
while (isRunning)
{
    int selectedItem = ShowMenu(menuItems);
    Console.Clear();

    switch (selectedItem)
    {
        case 0:
            ShowDoctors(clinic);
            break;
        case 1:
            SearchPatients(clinic);
            break;
        case 2:
            RegisterPatient(clinic);
            break;
        case 3:
            MakeAppointment(clinic);
            break;
        case 4:
            ShowAppointments(clinic);
            break;
        case 5:
            CancelAppointment(clinic);
            break;
        case 6:
            clinic.Save();
            isRunning = false;
            break;
    }

    if (isRunning)
    {
        Console.WriteLine();
        Console.WriteLine("Нажмите любую клавишу, чтобы вернуться в меню...");
        Console.ReadKey(true);
    }
}

static int ShowMenu(string[] items)
{
    int selected = 0;

    while (true)
    {
        Console.Clear();
        Console.WriteLine("=== ПОЛИКЛИНИКА ===");
        Console.WriteLine("Используйте стрелки вверх и вниз, Enter — выбрать.\n");

        for (int i = 0; i < items.Length; i++)
        {
            if (i == selected)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.WriteLine(" > " + items[i] + " ");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("   " + items[i]);
            }
        }

        ConsoleKey key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.UpArrow)
        {
            selected = (selected - 1 + items.Length) % items.Length;
        }
        else if (key == ConsoleKey.DownArrow)
        {
            selected = (selected + 1) % items.Length;
        }
        else if (key == ConsoleKey.Enter)
        {
            return selected;
        }
    }
}

static void ShowDoctors(ClinicService clinic)
{
    Console.WriteLine("=== ВРАЧИ ===");
    foreach (Doctor doctor in clinic.Doctors)
    {
        Console.WriteLine($"#{doctor.Id} {doctor.FullName} — {doctor.Specialty}, кабинет {doctor.Cabinet}");
    }
}

static void SearchPatients(ClinicService clinic)
{
    Console.WriteLine("=== ПОИСК ПАЦИЕНТА ===");
    string query = Read("Введите ФИО или телефон (Enter — показать всех): ");
    List<Patient> patients = clinic.SearchPatients(query).ToList();

    if (patients.Count == 0)
    {
        Console.WriteLine("Пациенты не найдены.");
        return;
    }

    foreach (Patient patient in patients)
    {
        Console.WriteLine($"#{patient.Id} {patient.FullName}, {patient.Phone}, дата рождения {patient.BirthDate:dd.MM.yyyy}");
    }
}

static void RegisterPatient(ClinicService clinic)
{
    Console.WriteLine("=== РЕГИСТРАЦИЯ ПАЦИЕНТА ===");
    string name = Read("ФИО: ");
    string phone = Read("Телефон: ");
    DateOnly birthDate = ReadDate("Дата рождения (дд.мм.гггг): ");

    try
    {
        Patient patient = clinic.RegisterPatient(name, phone, birthDate);
        Console.WriteLine($"Пациент зарегистрирован. Номер: #{patient.Id}.");
    }
    catch (ArgumentException exception)
    {
        Console.WriteLine(exception.Message);
    }
}

static void MakeAppointment(ClinicService clinic)
{
    Console.WriteLine("=== ЗАПИСЬ НА ПРИЁМ ===");
    if (clinic.Patients.Count == 0)
    {
        Console.WriteLine("Сначала зарегистрируйте пациента.");
        return;
    }

    foreach (Patient patient in clinic.Patients)
    {
        Console.WriteLine($"#{patient.Id} {patient.FullName} ({patient.Phone})");
    }
    int patientId = ReadInt("Номер пациента: ");

    foreach (Doctor doctor in clinic.Doctors)
    {
        Console.WriteLine($"#{doctor.Id} {doctor.FullName} — {doctor.Specialty}");
    }
    int doctorId = ReadInt("Номер врача: ");
    DateOnly date = ReadDate("Дата приёма (дд.мм.гггг): ");

    try
    {
        List<int> freeSlots = clinic.GetFreeSlots(doctorId, date);
        if (freeSlots.Count == 0)
        {
            Console.WriteLine("Свободного времени на эту дату нет.");
            return;
        }

        Console.WriteLine("Свободные часы: " + string.Join(", ", freeSlots));
        int hour = ReadInt("Выберите час: ");
        Appointment appointment = clinic.Book(patientId, doctorId, date, hour);
        Console.WriteLine($"Запись оформлена. Номер: #{appointment.Id}.");
    }
    catch (ArgumentException exception)
    {
        Console.WriteLine(exception.Message);
    }
}

static void ShowAppointments(ClinicService clinic)
{
    Console.WriteLine("=== ЗАПИСИ НА ПРИЁМ ===");
    if (clinic.Appointments.Count == 0)
    {
        Console.WriteLine("Записей пока нет.");
        return;
    }

    foreach (Appointment appointment in clinic.Appointments.OrderBy(item => item.Date).ThenBy(item => item.Hour))
    {
        Patient? patient = clinic.Patients.FirstOrDefault(item => item.Id == appointment.PatientId);
        Doctor? doctor = clinic.Doctors.FirstOrDefault(item => item.Id == appointment.DoctorId);
        string status = appointment.IsCancelled ? "отменена" : "активна";
        Console.WriteLine($"#{appointment.Id} {appointment.Date:dd.MM.yyyy} в {appointment.Hour}:00 | {patient?.FullName} | {doctor?.FullName} | {status}");
    }
}

static void CancelAppointment(ClinicService clinic)
{
    Console.WriteLine("=== ОТМЕНА ЗАПИСИ ===");
    int appointmentId = ReadInt("Номер записи: ");

    try
    {
        clinic.Cancel(appointmentId);
        Console.WriteLine("Запись отменена.");
    }
    catch (ArgumentException exception)
    {
        Console.WriteLine(exception.Message);
    }
}

static string Read(string prompt)
{
    Console.Write(prompt);
    return Console.ReadLine()?.Trim() ?? "";
}

static int ReadInt(string prompt)
{
    while (true)
    {
        string input = Read(prompt);
        if (int.TryParse(input, out int value))
        {
            return value;
        }

        Console.WriteLine("Введите целое число.");
    }
}

static DateOnly ReadDate(string prompt)
{
    while (true)
    {
        string input = Read(prompt);
        bool isValidDate = DateOnly.TryParseExact(
            input,
            "dd.MM.yyyy",
            CultureInfo.GetCultureInfo("ru-RU"),
            DateTimeStyles.None,
            out DateOnly date);

        if (isValidDate)
        {
            return date;
        }

        Console.WriteLine("Введите дату в формате дд.мм.гггг.");
    }
}
