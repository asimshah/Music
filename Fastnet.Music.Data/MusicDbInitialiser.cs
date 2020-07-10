using Fastnet.Core;
using Fastnet.Music.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Fastnet.Music.Data
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class MusicDbInitialiser
    {
        public static void Initialise(MusicDb db, MusicOptions options)
        {
            var log = db.Database.GetService<ILogger<MusicDbInitialiser>>() as ILogger;
            var creator = db.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            var dbExists = creator.Exists();
            if (!dbExists)
            {
                log.Warning("No MusicDb found, new one will be created");
            }
            var pendingMigrations = db.Database.GetPendingMigrations();
            if (pendingMigrations.Count() > 0)
            {
                log.Information($"The foolowing migration(s) are pending");
                foreach (var migration in pendingMigrations)
                {
                    log.Information($"\t{migration}");
                }
            }
            db.Database.Migrate();

            var migrations = db.Database.GetAppliedMigrations();
            if (migrations.Count() > 0)
            {
                log.Debug("The following migrations have been applied:");
                foreach (var migration in migrations)
                {
                    log.Debug($"\t{migration}");
                }
            }
            log.Information($"MusicDb is: {db.Database.GetDbConnection().ConnectionString}");
            db.UpgradeContent(options);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
