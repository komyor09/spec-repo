
namespace GreenhouseControl
{
    public class Greenhouse
    {
        public string Name { get; set; }

        public double Temperature { get; private set; }

        public double SoilMoisture { get; private set; }

        public bool IsWatering { get; private set; }

        public double MinTemperature { get; set; } = 15;
        public double MaxTemperature { get; set; } = 30;

        public Greenhouse(string name, double temperature)
        {
            Name = name;
            Temperature = temperature;
            SoilMoisture = 45;
            IsWatering = false;
        }

        public void SetTemperature(double temperature)
        {
            Temperature = temperature;
        }

        public void SetSoilMoisture(double moisture)
        {
            if (moisture < 0)
                moisture = 0;

            if (moisture > 100)
                moisture = 100;

            SoilMoisture = moisture;
        }

        public string CheckTemperature()
        {
            if (Temperature < MinTemperature)
                return "Температура слишком низкая!";

            if (Temperature > MaxTemperature)
                return "Температура слишком высокая!";

            return "Температура в норме.";
        }

        public string StartWatering()
        {
            if (IsWatering)
                return "Полив уже включён.";

            IsWatering = true;
            return "Полив включён.";
        }

        public string StopWatering()
        {
            if (!IsWatering)
                return "Полив уже выключен.";

            IsWatering = false;
            return "Полив выключен.";
        }
    }
}
