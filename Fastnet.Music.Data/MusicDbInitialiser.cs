using Fastnet.Core;
using Fastnet.Music.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;

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
            if (dbExists)
            {
                log.Information($"MusicDb exists: {db.Database.GetDbConnection().ConnectionString}");
            }
            else
            {
                log.Warning("No MusicDb found");
            }

            db.Database.Migrate();
            log.Debug("The following migrations have been applied:");
            var migrations = db.Database.GetAppliedMigrations();
            foreach (var migration in migrations)
            {
                log.Information($"\t{migration}");
            }
            db.UpgradeContent(options);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
