using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.TorrentTags.Services;

public partial class DatabaseMigrationService : IDisposable
{
    private readonly SqliteConnection _connection;

    private int _isDisposed;

    public DatabaseMigrationService([FromKeyedServices(Plugin.DatabaseFileName)] SqliteConnection connection)
    {
        _connection = connection;
    }

    [GeneratedRegex(@"^Jellyfin\.Plugin\.TorrentTags\.Migrations\.(\d{3})_(\w+)\.sql$", RegexOptions.CultureInvariant)]
    private static partial Regex MigrationResourceRegex();

    public void ApplyMigrations()
    {
        _connection.Open();

        using (SqliteCommand createMigrationTableCommand = _connection.CreateCommand())
        {
            createMigrationTableCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS migration(
                    version INTEGER NOT NULL,
                    description TEXT NOT NULL);
                """;
            createMigrationTableCommand.ExecuteNonQuery();
        }

        int currentMigrationVersion = 0;

        using (SqliteCommand getCurrentMigrationVersionCommand = _connection.CreateCommand())
        {
            getCurrentMigrationVersionCommand.CommandText = """
                SELECT version
                FROM migration
                LIMIT 1;
                """;

            using SqliteDataReader getCurrentMigrationVersionReader = getCurrentMigrationVersionCommand.ExecuteReader();

            if (getCurrentMigrationVersionReader.Read())
                currentMigrationVersion = getCurrentMigrationVersionReader.GetInt32(0);
        }

        Assembly assembly = GetType().Assembly;

        IReadOnlyList<Migration> orderedMigrations = [.. assembly.GetManifestResourceNames()
            .Select(resource => MigrationResourceRegex().Match(resource))
            .Where(match => match.Success)
            .Select(match => new Migration(
                Version: int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                Description: match.Groups[2].Value,
                ManifestResourceName: match.Value))
            .OrderBy(migration => migration.Version)];

        int latestMigrationVersion = orderedMigrations[^1].Version;

        if (latestMigrationVersion < currentMigrationVersion)
            throw new InvalidOperationException($"Latest migration version ({latestMigrationVersion}) precedes current migration version ({currentMigrationVersion}).");

        foreach (Migration migrationToApply in orderedMigrations.Where(migration => migration.Version > currentMigrationVersion))
        {
            using Stream migrationResourceStream = assembly.GetManifestResourceStream(migrationToApply.ManifestResourceName)
                ?? throw new InvalidOperationException($"Manifest resource '{migrationToApply.ManifestResourceName}' is null.");

            using StreamReader migrationResourceStreamReader = new StreamReader(migrationResourceStream, Encoding.UTF8);

            using SqliteTransaction applyMigrationTransaction = _connection.BeginTransaction();

            using (SqliteCommand applyMigrationCommand = _connection.CreateCommand())
            {
                applyMigrationCommand.CommandText = migrationResourceStreamReader.ReadToEnd();
                applyMigrationCommand.ExecuteNonQuery();
            }

            using (SqliteCommand updateCurrentMigrationCommand = _connection.CreateCommand())
            {
                updateCurrentMigrationCommand.CommandText = """
                    DELETE FROM migration;
                    INSERT INTO migration(version, description)
                    VALUES($version, $description);
                    """;
                updateCurrentMigrationCommand.Parameters.AddWithValue("$version", migrationToApply.Version);
                updateCurrentMigrationCommand.Parameters.AddWithValue("$description", migrationToApply.Description);
                updateCurrentMigrationCommand.ExecuteNonQuery();
            }

            applyMigrationTransaction.Commit();
        }

        _connection.Close();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
        {
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }

    private record Migration(
        int Version,
        string Description,
        string ManifestResourceName);
}
