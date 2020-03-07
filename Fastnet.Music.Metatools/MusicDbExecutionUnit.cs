namespace Fastnet.Music.Metatools
{
    //public abstract class MusicDbExecutionUnit
    //{
    //    private static bool useEfCore = true;// false;
    //    protected TimeSpan maxRetryInterval = TimeSpan.FromSeconds(30);
    //    protected readonly int maxRetries = 10;
    //    protected readonly string connectionString;
    //    protected readonly ILogger log;
    //    public MusicDbExecutionUnit(string cs)
    //    {
    //        this.connectionString = cs;
    //        this.log = ApplicationLoggerFactory.CreateLogger(this.GetType().Name);
    //    }
    //    public abstract Task<bool> ExecuteAsync(Func<Task> method);
    //    //public static MusicDbExecutionUnit Create(string cs)
    //    //{
    //    //    if (useEfCore)
    //    //    {
    //    //        return new EFCoreMusicDbExecutionUnit(cs);
    //    //    }
    //    //    else
    //    //    {
    //    //        return new FastnetMusicDbExecutionUnit(cs);
    //    //    }
    //    //}
    //}
    //public class EFCoreMusicDbExecutionUnit : MusicDbExecutionUnit
    //{
    //    public EFCoreMusicDbExecutionUnit(string cs) : base(cs)
    //    {
    //    }

    //    public override async Task<bool> ExecuteAsync(Func<Task> method)
    //    {
    //        bool result = true;
    //        using (var db1 = new MusicDb(connectionString))
    //        {
    //            var strategy = db1.Database.CreateExecutionStrategy();
    //            await strategy.ExecuteAsync(async () =>
    //            {
    //                try
    //                {
    //                    await method();
    //                }
    //                catch(Exception xe)
    //                {
    //                    //log.Error(xe);
    //                    result = false;
    //                }
    //                return Task.CompletedTask;
    //            });
    //        }
    //        return result;
    //    }
    //}
    //public class FastnetMusicDbExecutionUnit : MusicDbExecutionUnit
    //{
    //    public FastnetMusicDbExecutionUnit(string cs) : base(cs)
    //    {
    //    }

    //    public override async Task<bool> ExecuteAsync(Func<Task> method)
    //    {
    //        bool result = false;
    //        int counter = 0;
    //        while (counter++ < maxRetries)
    //        {
    //            try
    //            {
    //                await method();
    //                result = true;
    //                break;
    //            }
    //            catch (DbUpdateException dbxe)
    //            {
    //                LogDbUpdateException(dbxe);
    //                log.Information($"DbUpdateException: retrying ...");
    //            }
    //            catch (Exception xe)
    //            {
    //                if (xe.InnerException is DbUpdateException)
    //                {
    //                    LogDbUpdateException(xe.InnerException as DbUpdateException);
    //                    log.Information($"DbUpdateException: retrying ...");
    //                }
    //                else
    //                {
    //                    log.Error(xe);
    //                    break;
    //                }
    //            }
    //            await Task.Delay(maxRetryInterval);
    //        }
    //        return result;
    //    }
    //    private void LogDbUpdateException(DbUpdateException dbxe)
    //    {
    //        log.Information($"Exception is {dbxe.GetType().Name}");
    //        foreach(var entry in dbxe.Entries)
    //        {
    //            log.Information($"DbUpdateException, entry {entry.Entity.GetType().Name}");
    //        }
    //    }
    //}

}
