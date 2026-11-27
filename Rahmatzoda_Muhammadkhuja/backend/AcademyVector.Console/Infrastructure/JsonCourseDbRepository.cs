using System.Text.Encodings.Web;
using System.Text.Json;
using AcademyVector.ConsoleApp.Application.Interfaces;
using AcademyVector.ConsoleApp.Domain.Entities;

namespace AcademyVector.ConsoleApp.Infrastructure;

public sealed class JsonCourseDbRepository : ICourseDbRepository
{
    private readonly string _dbPath;
    private readonly ISeedDataProvider _seedProvider;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public JsonCourseDbRepository(ISeedDataProvider seedProvider)
    {
        _seedProvider = seedProvider;
        _dbPath = ResolveDbPath();
    }

    private static string ResolveDbPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AcademyVector.sln")) ||
                Directory.Exists(Path.Combine(current.FullName, "data")))
            {
                return Path.Combine(current.FullName, "data", "courses-db.json");
            }

            current = current.Parent;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), "data", "courses-db.json");
    }

    public async Task<PlatformDatabase> ReadAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            return await EnsureDbAsync(ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<T> UpdateAsync<T>(Func<PlatformDatabase, T> mutator, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var db = await EnsureDbAsync(ct);
            var result = mutator(db);
            await WriteAtomicAsync(db, ct);
            return result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PlatformDatabase> ResetAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var seed = _seedProvider.CreateSeedDatabase();
            await WriteAtomicAsync(seed, ct);
            return seed;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<PlatformDatabase> EnsureDbAsync(CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_dbPath)!;
        Directory.CreateDirectory(directory);

        if (!File.Exists(_dbPath))
        {
            var seed = _seedProvider.CreateSeedDatabase();
            await WriteAtomicAsync(seed, ct);
            return seed;
        }

        var json = await File.ReadAllTextAsync(_dbPath, ct);
        var deserialized = JsonSerializer.Deserialize<PlatformDatabase>(json, JsonOptions);
        var isCurrentSchema = deserialized != null
            && deserialized.Reviews.Count > 0
            && deserialized.Courses.Count >= 8
            && deserialized.Students.Count >= 12;

        if (!isCurrentSchema)
        {
            var seed = _seedProvider.CreateSeedDatabase();
            await WriteAtomicAsync(seed, ct);
            return seed;
        }

        return deserialized!;
    }

    private async Task WriteAtomicAsync(PlatformDatabase db, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_dbPath)!;
        Directory.CreateDirectory(directory);
        var tempPath = Path.Combine(directory, $"courses-db.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");
        var json = JsonSerializer.Serialize(db, JsonOptions);
        await File.WriteAllTextAsync(tempPath, json, ct);
        File.Move(tempPath, _dbPath, overwrite: true);
    }
}
