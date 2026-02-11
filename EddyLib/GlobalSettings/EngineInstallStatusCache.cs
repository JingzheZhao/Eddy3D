using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EddyLib
{
    /// <summary>
    /// Stores per-user engine installation checks for Grasshopper components.
    /// </summary>
    public static class EngineInstallStatusCache
    {
        public const string EddyCacheFileName = "install_engine_status_eddy3d.json";
        public const string OutdoorPlusCacheFileName = "install_engine_status_outdoorplus.json";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static string CacheDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Eddy3D");

        public static string EddyCachePath => Path.Combine(CacheDirectory, EddyCacheFileName);

        public static string OutdoorPlusCachePath => Path.Combine(CacheDirectory, OutdoorPlusCacheFileName);

        public static EngineInstallStatusSnapshot CreateSnapshot(string projectName, string platform)
        {
            return new EngineInstallStatusSnapshot
            {
                Project = projectName ?? string.Empty,
                Platform = platform ?? string.Empty,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        public static void SetEngineStatus(
            EngineInstallStatusSnapshot snapshot,
            string engineName,
            bool installed,
            string details)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(engineName))
                return;

            if (snapshot.Engines == null)
            {
                snapshot.Engines = new Dictionary<string, EngineInstallStatusEntry>(StringComparer.OrdinalIgnoreCase);
            }

            snapshot.Engines[engineName] = new EngineInstallStatusEntry
            {
                Installed = installed,
                Details = details ?? string.Empty
            };
        }

        public static bool TryWrite(
            string path,
            EngineInstallStatusSnapshot snapshot,
            out string error)
        {
            error = string.Empty;

            if (snapshot == null)
            {
                error = "Snapshot is null.";
                return false;
            }

            try
            {
                var directory = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directory))
                    directory = CacheDirectory;

                Directory.CreateDirectory(directory);
                snapshot.UpdatedAtUtc = DateTime.UtcNow;

                var json = JsonSerializer.Serialize(snapshot, JsonOptions);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryRead(
            string path,
            out EngineInstallStatusSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    error = "Cache file not found.";
                    return false;
                }

                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<EngineInstallStatusSnapshot>(json);

                if (parsed == null)
                {
                    error = "Cache file is empty or invalid.";
                    return false;
                }

                if (parsed.Engines == null)
                {
                    parsed.Engines = new Dictionary<string, EngineInstallStatusEntry>(StringComparer.OrdinalIgnoreCase);
                }

                snapshot = parsed;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static bool TryGetEngineStatus(
            EngineInstallStatusSnapshot snapshot,
            string engineName,
            out bool installed,
            out string details)
        {
            installed = false;
            details = string.Empty;

            if (snapshot == null || snapshot.Engines == null || string.IsNullOrWhiteSpace(engineName))
                return false;

            if (!snapshot.Engines.TryGetValue(engineName, out var entry) || entry == null)
                return false;

            installed = entry.Installed;
            details = entry.Details ?? string.Empty;
            return true;
        }
    }

    public sealed class EngineInstallStatusSnapshot
    {
        public int SchemaVersion { get; set; } = 1;

        public string Project { get; set; } = string.Empty;

        public string Platform { get; set; } = string.Empty;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public Dictionary<string, EngineInstallStatusEntry> Engines { get; set; } =
            new Dictionary<string, EngineInstallStatusEntry>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class EngineInstallStatusEntry
    {
        public bool Installed { get; set; }

        public string Details { get; set; } = string.Empty;
    }
}
