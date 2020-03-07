//using Microsoft.AspNetCore.Hosting;

namespace Fastnet.Music.Data
{

    ///// <summary>
    ///// Use this to create a disposable instance of the MusicDb when DI cannot be used
    ///// </summary>
    ////public class MusicDbContextFactory : WebDbContextFactory
    //public class MusicDbContextFactory //: WebDbContextFactory
    //{
    //    //private readonly IHostingEnvironment environment;
    //    private readonly MusicDbOptions musicDbOptions;
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="options"></param>
    //    /// <param name="sp"></param>
    //    public MusicDbContextFactory(IOptions<MusicDbOptions> options, IServiceProvider sp) //: base(options, sp)
    //    {
    //        //this.environment = environment;
    //        this.musicDbOptions = options.Value;
    //    }
    //    ///// <summary>
    //    ///// 
    //    ///// </summary>
    //    ///// <param name="AutoDetectChangesEnabled"></param>
    //    ///// <returns></returns>
    //    //public MusicDb GetMusicDb(bool AutoDetectChangesEnabled = false)
    //    //{
    //    //    var db = this.GetWebDbContext<MusicDb>();
    //    //    db.ChangeTracker.AutoDetectChangesEnabled = AutoDetectChangesEnabled;
    //    //    return db;
    //    //}
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="AutoDetectChangesEnabled"></param>
    //    /// <returns></returns>
    //    public MusicDb GetMusicDb(bool AutoDetectChangesEnabled = false)
    //    {
    //        var cs = GetMusicDbConnectionString();
    //        var optionsBuilder = new DbContextOptionsBuilder<MusicDb>();
    //        optionsBuilder.UseSqlServer(GetMusicDbConnectionString());
    //        var db =  new MusicDb(optionsBuilder.Options);
    //        db.ChangeTracker.AutoDetectChangesEnabled = AutoDetectChangesEnabled;
    //        return db;
    //    }
    //    private string GetMusicDbConnectionString()
    //    {
    //        var path = @"C:\devroot\Standard2\Music\Fastnet.Music.Web";
    //        var databaseFilename = @"Music.mdf";
    //        var catalog = @"Music-dev";// : @"Music";
    //        //string path = environment.ContentRootPath;
    //        string dataFolder = Path.Combine(path, "Data");
    //        if (!Directory.Exists(dataFolder))
    //        {
    //            Directory.CreateDirectory(dataFolder);
    //        }
    //        string databaseFile = Path.Combine(dataFolder, databaseFilename);
    //        SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder();
    //        csb.AttachDBFilename = databaseFile;
    //        csb.DataSource = ".\\SQLEXPRESS";
    //        csb.InitialCatalog = catalog;
    //        csb.IntegratedSecurity = true;
    //        csb.MultipleActiveResultSets = true;
    //        return csb.ToString();
    //    }
    //}
}
