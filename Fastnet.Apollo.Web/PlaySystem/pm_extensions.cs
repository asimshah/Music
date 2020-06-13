using Fastnet.Apollo.Web.Controllers;
using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Apollo.Web
{
    public static partial class pm_extensions
    {
        //public static PlaylistRuntime ToRuntime(this Playlist list, MusicDb db, DeviceRuntime dr)
        //{
        //    return new PlaylistRuntime
        //    {
        //        Id = list.Id,
        //        Name = list.Name,
        //        Type = list.Type,
        //        Items = list.Items
        //            .Select(x => x.ToRuntime(db, dr))
        //            .Where(x => x != null)
        //            .OrderBy(x => x.Sequence).ToList()
        //    };
        //}
        //public static PlaylistItemRuntime[] AsMovementsToRuntime(this IEnumerable<Track> trackList, DeviceRuntime dr, int majorSequence)
        //{
        //    var result = new List<PlaylistItemRuntime>();
        //    var index = 0;
        //    foreach (var track in trackList)
        //    {
        //        var dto = track.ToRuntime(dr, majorSequence);
        //        dto.Sequence = ++index;
        //        dto.Position = new PlaylistPosition(majorSequence, dto.Sequence);
        //        dto.Titles = new string[] { track.Performance.GetParentArtistsName(), track.Performance.GetParentEntityDisplayName(), track.Performance.GetAllPerformersCSV(), track.Title };
        //        result.Add(dto);
        //    }
        //    return result.ToArray();
        //}
        //public static PlaylistItemRuntime ToRuntime(this Track track, DeviceRuntime dr, int majorSequence)
        //{
        //    var mf = track.GetBestMusicFile(dr);// getBestMusicFile(track);
        //    return new PlaylistItemRuntime
        //    {
        //        Id = 0,// pli.Id,
        //        Type = PlaylistRuntimeItemType.SingleItem,
        //        Position = new PlaylistPosition(majorSequence, track.Number),
        //        Titles = new string[] { track.Work.Artists.First().Name, track.Performance?.GetParentEntityDisplayName() ?? track.Work.Name, track.Title },
        //        Sequence = track.Number,
        //        NotPlayableOnCurrentDevice = mf == null,
        //        ItemId = track.Id,
        //        MusicFileId = mf?.Id ?? 0,
        //        AudioProperties = mf?.GetAudioProperties(),
        //        SampleRate = mf?.SampleRate ?? 0,
        //        TotalTime = mf?.Duration ?? 0.0,
        //        FormattedTotalTime = mf?.Duration.FormatDuration(),
        //    };
        //}
        //public static PlaylistItemRuntime ToRuntime(this PlaylistItem pli, MusicDb db, DeviceRuntime dr)
        //{
        //    PlaylistItemRuntime plir = null;
        //    switch (pli.Type)
        //    {
        //        default:
        //        case PlaylistItemType.MusicFile:
        //            var playable = dr.MaxSampleRate == 0 || pli.MusicFile.SampleRate == 0 || pli.MusicFile.SampleRate <= dr.MaxSampleRate;
        //            plir = new PlaylistItemRuntime
        //            {
        //                Id = pli.Id,
        //                Type = PlaylistRuntimeItemType.SingleItem,
        //                Position = new PlaylistPosition(pli.Sequence, 0),
        //                Titles = new string[] {
        //                        pli.MusicFile.Track.Performance?.GetParentArtistsName() ?? pli.MusicFile.Track.Work.Artists.Select(a => a.Name).ToCSV(),
        //                        pli.MusicFile.Track.Performance?.GetParentEntityDisplayName() ?? pli.MusicFile.Track.Work.Name,
        //                        pli.MusicFile.Track.Title
        //                    },
        //                Sequence = pli.Sequence,
        //                NotPlayableOnCurrentDevice = !playable,
        //                ItemId = pli.ItemId,
        //                MusicFileId = pli.MusicFile.Id,
        //                AudioProperties = pli.MusicFile.GetAudioProperties(),
        //                SampleRate = pli.MusicFile.SampleRate ?? 0,
        //                TotalTime = pli.MusicFile.Duration ?? 0.0,
        //                FormattedTotalTime = pli.MusicFile.Duration?.FormatDuration() ?? "00:00",
        //                CoverArtUrl = $"lib/get/work/coverart/{pli.MusicFile.Track.Work.Id}"
        //            };
        //            break;
        //        case PlaylistItemType.Track:
        //            var mf = pli.Track.GetBestMusicFile(dr);
        //            plir = new PlaylistItemRuntime
        //            {
        //                Id = pli.Id,
        //                Type = PlaylistRuntimeItemType.SingleItem,
        //                Position = new PlaylistPosition(pli.Sequence, 0),
        //                Titles = new string[] {
        //                        pli.Track.Performance?.GetParentArtistsName() ?? pli.Track.Work.Artists.Select(a => a.Name).ToCSV(),
        //                        pli.Track.Performance?.GetParentEntityDisplayName() ?? pli.Track.Work.Name,
        //                        pli.Track.Title
        //                    },
        //                Sequence = pli.Sequence,
        //                NotPlayableOnCurrentDevice = mf == null,
        //                ItemId = pli.ItemId,
        //                MusicFileId = mf?.Id ?? 0,
        //                AudioProperties = mf?.GetAudioProperties(),
        //                SampleRate = mf?.SampleRate ?? 0,
        //                TotalTime = mf?.Duration ?? 0.0,
        //                FormattedTotalTime = mf?.Duration?.FormatDuration() ?? "00:00",
        //                CoverArtUrl = $"lib/get/work/coverart/{pli.Track.Work.Id}"
        //            };
        //            break;
        //        case PlaylistItemType.Work:
        //            var work = db.Works.Find(pli.Work.Id);
        //            var tracks = work.Tracks;
        //            plir = new PlaylistItemRuntime
        //            {
        //                Id = pli.Id,
        //                Type = PlaylistRuntimeItemType.MultipleItems,
        //                Position = new PlaylistPosition(pli.Sequence, 0),
        //                //Title = pli.Title,
        //                Titles = new string[] { pli.Title },
        //                Sequence = pli.Sequence,
        //                ItemId = pli.ItemId,
        //                CoverArtUrl = $"lib/get/work/coverart/{pli.Work.Id}",
        //                // ***NB*** the ToArray() at the end of the next line is very important, as 
        //                // otherwise the OrderBy...Select will be deferred and may execute *after* the db has been disposed!!
        //                SubItems = tracks.OrderBy(t => t.Number).Select(t => t.ToRuntime(dr, pli.Sequence)).ToArray()
        //            };
        //            plir.TotalTime = plir.SubItems.Sum(x => x.TotalTime);
        //            plir.FormattedTotalTime = plir.TotalTime.FormatDuration();
        //            break;
        //        case PlaylistItemType.Performance:
        //            var performance = db.Performances.Find(pli.Performance.Id);
        //            var movements = performance.Movements;
        //            plir = new PlaylistItemRuntime
        //            {
        //                Id = pli.Id,
        //                Type = PlaylistRuntimeItemType.MultipleItems,
        //                Position = new PlaylistPosition(pli.Sequence, 0),
        //                //Title = pli.Title,
        //                Titles = new string[] { pli.Title },
        //                Sequence = pli.Sequence,
        //                ItemId = pli.ItemId,
        //                CoverArtUrl = $"lib/get/work/coverart/{movements.First().Work.Id}",
        //                //SubItems = movements.OrderBy(t => t.Number).Select(t => t.ToRuntime(pli.Sequence)).ToArray()
        //                SubItems = movements.OrderBy(t => t.Number).AsMovementsToRuntime(dr, pli.Sequence) //**NB* this version of ToRuntime, rewrites the sequence (as movements cannot usse the track number
        //            };
        //            plir.TotalTime = plir.SubItems.Sum(x => x.TotalTime);
        //            plir.FormattedTotalTime = plir.TotalTime.FormatDuration();
        //            //return plir2;
        //            break;
        //    }
        //    Debug.Assert(plir != null);
        //    return plir;
        //}
    }
}
