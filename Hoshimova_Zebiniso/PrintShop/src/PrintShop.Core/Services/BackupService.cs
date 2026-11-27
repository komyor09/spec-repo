using Microsoft.Data.Sqlite;
using PrintShop.Core.Data;

namespace PrintShop.Core.Services;

/// <summary>Резервное копирование и восстановление файла базы данных.</summary>
public class BackupService
{
    private readonly DatabaseContext _db;

    public BackupService(DatabaseContext db) => _db = db;

    public string DefaultBackupFolder =>
        Path.Combine(Path.GetDirectoryName(_db.DatabasePath)!, "Backups");

    public string SuggestFileName() => $"printshop_{DateTime.Now:yyyyMMdd_HHmmss}.db";

    /// <summary>
    /// Создаёт копию БД с помощью встроенного механизма SQLite Online Backup —
    /// корректно работает даже при открытом подключении.
    /// </summary>
    public string Backup(string? targetPath = null)
    {
        targetPath ??= Path.Combine(DefaultBackupFolder, SuggestFileName());
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(targetPath))!);
        if (File.Exists(targetPath)) File.Delete(targetPath);

        using var source = new SqliteConnection(_db.ConnectionString);
        using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = targetPath, Pooling = false }.ToString());
        source.Open();
        target.Open();
        source.BackupDatabase(target);
        return targetPath;
    }

    /// <summary>Восстановление БД из резервной копии (текущая база предварительно сохраняется).</summary>
    public void Restore(string backupPath)
    {
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Файл резервной копии не найден.", backupPath);

        Backup(); // страховочная копия текущего состояния

        using var source = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = backupPath, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        using var target = new SqliteConnection(_db.ConnectionString);
        source.Open();
        target.Open();
        source.BackupDatabase(target);
    }
}
