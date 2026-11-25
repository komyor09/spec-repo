using System.Text.Json;

public sealed class Patient
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateOnly BirthDate { get; set; }
}

public sealed class Doctor
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Specialty { get; set; } = "";
    public int Cabinet { get; set; }
}

public sealed class Appointment
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateOnly Date { get; set; }
    public int Hour { get; set; }
    public bool IsCancelled { get; set; }
}

public sealed class ClinicData
{
    public List<Patient> Patients { get; set; } = [];
    public List<Doctor> Doctors { get; set; } = [];
    public List<Appointment> Appointments { get; set; } = [];
}
