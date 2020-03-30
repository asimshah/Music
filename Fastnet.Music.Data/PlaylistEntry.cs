using System.Collections.Generic;

namespace Fastnet.Music.Data
{
    //public static class plExtensions
    //{
    //    //public static string GetDisplayName(this MusicDb db, Performance performance)
    //    //{
    //    //    //await db.Entry(performance).Reference(x => x.Composition).LoadAsync();
    //    //    //await db.Entry(performance.Composition).Reference(x => x.Artist).LoadAsync();
    //    //    var parts = performance.Composition.Artist.Name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    //    //    return $"{parts.Last()}, {performance.Composition.Name}";
    //    //}
    //    //public static IEnumerable<(Track track, long musicFileId)> GetTracks(this MusicDb db, IPlayable playable)
    //    //{
    //    //    var list = new List<(Track track, long musicFileId)>();
    //    //    //await db.LoadRelatedEntities(playable);
    //    //    foreach (var track in playable.Tracks.OrderBy(t => t.Number))
    //    //    {
    //    //        //await db.LoadRelatedEntities(track);
    //    //        list.Add((track, track.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Id));
    //    //    }
    //    //    return list;
    //    //}


    //    //private static async Task LoadRelatedEntities<T>(this MusicDb db, T playable) where T : class, IPlayable
    //    //{
    //    //    switch (playable)
    //    //    {
    //    //        case Work work:
    //    //            await db.LoadRelatedEntities(work);
    //    //            break;
    //    //        case Performance performance:
    //    //            await db.LoadRelatedEntities(performance);
    //    //            break;
    //    //    }
    //    //}
    //    //private static async Task LoadRelatedEntities(this MusicDb db, Performance performance)
    //    //{
    //    //    await db.Entry(performance).Collection(x => x.Movements).LoadAsync();
    //    //}
    //    //private static async Task LoadRelatedEntities(this MusicDb db, Work work)
    //    //{
    //    //    await db.Entry(work).Collection(x => x.Tracks).LoadAsync();
    //    //}
    //    //private static async Task LoadRelatedEntities(this MusicDb db, Track track)
    //    //{
    //    //    await db.Entry(track).Collection(x => x.MusicFiles).LoadAsync();
    //    //}
    //}
    public class PlaylistEntry
    {
        //public PlaylistEntryType Type { get; set; }
        public int Sequence { get; set; }
        public string Title { get; set; }
        public long PlaylistItemId { get; set; }
        public long PlaylistSubItemId { get; set; }
        public double TotalTime { get; set; }
        public List<PlaylistEntry> SubEntries { get; set; }
    }
}
