using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;

namespace Fastnet.Music.Data
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class DesignTimeMusicDbFactory : IDesignTimeDbContextFactory<MusicDb>
    {
        public MusicDb CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MusicDb>();
            optionsBuilder.UseSqlServer(GetDesignTimeConnectionString());
            return new MusicDb(optionsBuilder.Options);
        }
        private string GetDesignTimeConnectionString()
        {
            //var path = @"C:\devroot\Standard2\Music\Fastnet.Music.Web";
            var path = @"C:\devroot\Music\Fastnet.Apollo.Web";
            var databaseFilename = @"Music.mdf";
            var catalog = @"Music-test-dev";
            string dataFolder = Path.Combine(path, "Data");
            //if (!Directory.Exists(dataFolder))
            //{
            //    Directory.CreateDirectory(dataFolder);
            //}
            string databaseFile = Path.Combine(dataFolder, databaseFilename);
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder();
            csb.AttachDBFilename = databaseFile;
            csb.DataSource = ".\\SQLEXPRESS";
            csb.InitialCatalog = catalog;
            csb.IntegratedSecurity = true;
            csb.MultipleActiveResultSets = true;
            return csb.ToString();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
