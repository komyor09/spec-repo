
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Data;

namespace GreenhouseControl
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {
            string folderPath = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "GreenhouseControl");

            Directory.CreateDirectory(folderPath);

            string dbPath = Path.Combine(
                folderPath, "greenhouse.db");

            _connectionString = $"Data Source={dbPath}";

            InitializeDatabase();


        }


        private void InitializeDatabase()
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            string sql = @"
                CREATE TABLE IF NOT EXISTS TemperatureReadings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Temperature REAL NOT NULL,
                    MeasuredAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS WateringHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    IsEnabled INTEGER NOT NULL,
                    Mode TEXT NOT NULL,
                    SoilMoisture REAL NOT NULL,
                    RecordedAt TEXT NOT NULL
                );

CREATE TABLE IF NOT EXISTS GreenhouseSettings (
    Id INTEGER PRIMARY KEY CHECK (Id = 1),
    MinTemperature REAL NOT NULL,
    MaxTemperature REAL NOT NULL
);

INSERT OR IGNORE INTO GreenhouseSettings
    (Id, MinTemperature, MaxTemperature)
VALUES
    (1, 15, 30);

            ";

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public void SaveTemperature(double temperature)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO TemperatureReadings
                    (Temperature, MeasuredAt)
                VALUES
                    ($temperature, $measuredAt);
            ";

            command.Parameters.AddWithValue(
                "$temperature", temperature);

            command.Parameters.AddWithValue(
                "$measuredAt",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            command.ExecuteNonQuery();
        }

        public void SaveWatering(
            bool isEnabled,
            string mode,
            double soilMoisture)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
                INSERT INTO WateringHistory
                    (IsEnabled, Mode, SoilMoisture, RecordedAt)
                VALUES
                    ($isEnabled, $mode, $moisture, $recordedAt);
            ";

            command.Parameters.AddWithValue(
                "$isEnabled", isEnabled ? 1 : 0);

            command.Parameters.AddWithValue("$mode", mode);

            command.Parameters.AddWithValue(
                "$moisture", soilMoisture);

            command.Parameters.AddWithValue(
                "$recordedAt",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            command.ExecuteNonQuery();
        }

        public DataTable GetTemperatureHistory()
        {
            DataTable table = new DataTable();

            table.Columns.Add("№", typeof(long));
            table.Columns.Add("Температура, °C", typeof(double));
            table.Columns.Add("Дата и время", typeof(string));

            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
        SELECT Id, Temperature, MeasuredAt
        FROM TemperatureReadings
        ORDER BY Id DESC;
    ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                table.Rows.Add(
                    reader.GetInt64(0),
                    reader.GetDouble(1),
                    reader.GetString(2));
            }

            return table;
        }
        public DataTable GetWateringHistory()
        {
            DataTable table = new DataTable();

            table.Columns.Add("№", typeof(long));
            table.Columns.Add("Состояние", typeof(string));
            table.Columns.Add("Режим", typeof(string));
            table.Columns.Add("Влажность, %", typeof(double));
            table.Columns.Add("Дата и время", typeof(string));

            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
        SELECT Id, IsEnabled, Mode, SoilMoisture, RecordedAt
        FROM WateringHistory
        ORDER BY Id DESC;
    ";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string status = reader.GetInt64(1) == 1
                    ? "Включён"
                    : "Выключен";

                table.Rows.Add(
                    reader.GetInt64(0),
                    status,
                    reader.GetString(2),
                    reader.GetDouble(3),
                    reader.GetString(4));
            }

            return table;
        }

        public (double MinTemperature, double MaxTemperature)
            GetSettings()
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
        SELECT MinTemperature, MaxTemperature
        FROM GreenhouseSettings
        WHERE Id = 1;
    ";

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return (
                    reader.GetDouble(0),
                    reader.GetDouble(1)
                );
            }

            return (15, 30);
        }

        public void SaveSettings(
            double minTemperature,
            double maxTemperature)
        {
            using var connection =
                new SqliteConnection(_connectionString);

            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
        UPDATE GreenhouseSettings
        SET MinTemperature = $min,
            MaxTemperature = $max
        WHERE Id = 1;
    ";

            command.Parameters.AddWithValue("$min", minTemperature);
            command.Parameters.AddWithValue("$max", maxTemperature);

            command.ExecuteNonQuery();
        }

    }
}
