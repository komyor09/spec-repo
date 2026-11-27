using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using VetMed.Api.Application.Interfaces;
using VetMed.Api.Domain.Entities;

namespace VetMed.Api.Infrastructure.Persistence;

public static class WorkspacePathResolver
{
    public static string ResolveWorkspaceRoot()
    {
        var candidates = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var start in candidates)
        {
            var current = new DirectoryInfo(start);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "package.json"))
                    || File.Exists(Path.Combine(current.FullName, "VetMed.sln")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        return Directory.GetCurrentDirectory();
    }
}

public sealed class JsonClinicRepository : IClinicRepository
{
    private readonly IClinicSeedProvider _seedProvider;
    private readonly string _dataDir;
    private readonly string _dbFile;
    private readonly object _syncLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public JsonClinicRepository(IClinicSeedProvider seedProvider)
    {
        _seedProvider = seedProvider;
        var root = WorkspacePathResolver.ResolveWorkspaceRoot();
        _dataDir = Path.Combine(root, "data");
        _dbFile = Path.Combine(_dataDir, "clinic-db.json");
    }

    public ClinicState LoadState()
    {
        lock (_syncLock)
        {
            if (!Directory.Exists(_dataDir))
            {
                Directory.CreateDirectory(_dataDir);
            }

            if (!File.Exists(_dbFile))
            {
                var fresh = _seedProvider.CreateInitialState();
                WriteFileInternal(fresh);
                return fresh;
            }

            try
            {
                var raw = File.ReadAllText(_dbFile);
                var parsed = JsonSerializer.Deserialize<ClinicState>(raw, JsonOptions);
                if (parsed is null
                    || parsed.LabTests is null
                    || parsed.InpatientTasks is null
                    || parsed.Invoices is null
                    || (parsed.Pets is not { Count: > 0 } && parsed.Owners is not { Count: > 0 }))
                {
                    var upgraded = _seedProvider.CreateInitialState();
                    WriteFileInternal(upgraded);
                    return upgraded;
                }

                return parsed;
            }
            catch
            {
                var fallback = _seedProvider.CreateInitialState();
                WriteFileInternal(fallback);
                return fallback;
            }
        }
    }

    public void SaveState(ClinicState state)
    {
        lock (_syncLock)
        {
            if (!Directory.Exists(_dataDir))
            {
                Directory.CreateDirectory(_dataDir);
            }

            WriteFileInternal(state);
        }
    }

    public ClinicState ResetState()
    {
        lock (_syncLock)
        {
            var fresh = _seedProvider.CreateInitialState();
            if (!Directory.Exists(_dataDir))
            {
                Directory.CreateDirectory(_dataDir);
            }

            WriteFileInternal(fresh);
            return fresh;
        }
    }

    private void WriteFileInternal(ClinicState state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_dbFile, json);
    }
}
