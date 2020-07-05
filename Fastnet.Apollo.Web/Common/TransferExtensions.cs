using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Messages;
using Fastnet.Music.Metatools;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fastnet.Apollo.Web
{
    public static partial class TransferExtensions
    {
        private static readonly ILogger log = ApplicationLoggerFactory.CreateLogger("Fastnet.Apollo.Web.TransferExtensions");
        public static ArtistSetDTO ToDTO(this IEnumerable<ArtistSetRagaPerformance> list)
        {
            return new ArtistSetDTO
            {
                ArtistIds = list.First().ArtistSet.ArtistIds.ToArray(),//.Artists, //.Select(x => x.Id).ToArray(),
                RagaCount = list.Select(x => x.Raga).Distinct().Count(),
                PerformanceCount = list.Select(x => x.Performance).Distinct().Count()
            };
        }
        public static ArtistSetDTO ToDTO(this IEnumerable<RagaPerformance> list/*, IndianClassicalInformation ici*/)
        {
            return new ArtistSetDTO
            {
                ArtistIds = list.Select(x => x.ArtistId).Distinct().ToArray(),
                RagaCount = list.Select(x => x.Raga).Distinct()/*.Select(r => r.ToDTO(ici)).OrderBy(x => x.Name)*/.Count(),
                PerformanceCount = list.Select(x => x.Performance).Distinct().Count()
            };
        }
        public static ArtistDTO ToDTO(this Artist a, MusicStyles style)
        {            
            var dto = new ArtistDTO
            {
                Id = a.Id,
                Name = a.Name,
                Lastname = a.Name.GetLastName(),
                DisplayName = a.Name,
                ArtistType = a.Type,
                Styles = a.ArtistStyles.Select(s => s.StyleId),

                Quality = a.ParsingStage.ToMetadataQuality(),
                ImageUrl = $"lib/get/artist/imageart/{a.Id}"
            };
            switch (style)
            {
                case MusicStyles.IndianClassical:
                    dto.RagaCount = a.RagaPerformances.Select(x => x.Raga).Distinct().Count();
                    dto.PerformanceCount = a.RagaPerformances.Select(x => x.Performance).Distinct().Count();
                    //var trackCount = a.RagaPerformances.SelectMany(x => x.Performance.Movements).Count();
                    //log.Information($"{a.Name} {trackCount}");
                    break;
                default:
                    dto.WorkCount = a.Works.Count(w => w.StyleId == style && w.Type != OpusType.Singles);
                    dto.SinglesCount = a.Works.Where(w => w.StyleId == style && w.Type == OpusType.Singles).SelectMany(x => x.Tracks).Count();
                    dto.CompositionCount = a.Compositions?.Count() ?? 0;
                    dto.PerformanceCount = a.Compositions?.SelectMany(x => x.Performances).Count() ?? 0;
                    break;
            }
            return dto;
        }
        public static CompositionDTO ToDTO(this Composition c, bool fullContent = false)
        {
            if (!fullContent)
            {
                return new CompositionDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    DisplayName = c.Name
                };
            }
            return new CompositionDTO
            {
                Id = c.Id,
                Name = c.Name,
                DisplayName = c.Name,
                Performances = c.Performances.Select(p => p.ToDTO(c.Name)).ToArray()
            };
        }
        public static RagaDTO ToDTO(this Raga raga)
        {
            //var rn = ici.Lookup[raga.AlphamericName];
            return new RagaDTO
            {
                Id = raga.Id,
                Name = raga.Name,
                DisplayName = raga.DisplayName// rn.DisplayName ??  $"Raga {raga.Name}"
            };
        }
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="p"></param>
        ///// <param name="full">movements are included if true</param>
        ///// <returns></returns>
        //public static PerformanceDTO ToDTO(this Performance p, Composition composition, bool full/* = false*/)
        //{
        //    //if (!full)
        //    //{
        //    //    return new PerformanceDTO
        //    //    {
        //    //        Id = p.Id,
        //    //        Performers = p.GetAllPerformersCSV(),
        //    //        Year = p.Year,
        //    //        AlbumName = p.Movements.First().Work.Name,
        //    //        DisplayName = p.Movements.First().Work.Name,
        //    //        AlbumCoverArt = $"lib/get/work/coverart/{p.Movements.First().Work.Id}",// p.Movements.First().Work
        //    //        MovementCount = p.Movements.Count,
        //    //        FormattedDuration = p.Movements
        //    //        .Sum(x => x.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Duration)
        //    //        .FormatDuration()
        //    //    };
        //    //}
        //    return new PerformanceDTO
        //    {
        //        Id = p.Id,
        //        Performers = p.GetAllPerformersCSV(),
        //        Year = p.Year,
        //        AlbumName = p.Movements.First().Work.Name,
        //        DisplayName = p.Movements.First().Work.Name,
        //        AlbumCoverArt = $"lib/get/work/coverart/{p.Movements.First().Work.Id}",
        //        MovementCount = p.Movements.Count,
        //        Movements = p.Movements.ToDTO(composition.Name),
        //        FormattedDuration = p.Movements
        //            .Sum(x => x.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Duration)
        //            .FormatDuration()
        //    };
        //}
        public static PerformanceDTO ToDTO(this Performance p, string parentName/*, bool full = false*/)
        {
            var work = p.Movements.Select(x => x.Work).Distinct().Single();
            var names = p.GetNames();
            return new PerformanceDTO
            {
                Id = p.Id,
                Performers = p.GetAllPerformersCSV(),
                Year = p.Year,
                MovementCount = p.Movements.Count,
                Movements = p.Movements.ToDTO(parentName),
                FormattedDuration = p.Movements
                            .Sum(x => x.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Duration)
                            .FormatDuration(),
                AlbumName = names.albumName,
                DisplayName = names.albumName,
                ArtistName = names.artistNames,
                WorkName = names.workName,
                AlbumCoverArt = $"lib/get/work/coverart/{work.Id}",
            };
            //switch (p.StyleId)
            //{
            //    case MusicStyles.WesternClassical:
            //        return new PerformanceDTO
            //        {
            //            Id = p.Id,
            //            Performers = p.GetAllPerformersCSV(),
            //            Year = p.Year,
            //            AlbumName = p.Movements.First().Work.Name,
            //            DisplayName = p.Movements.First().Work.Name,
            //            AlbumCoverArt = $"lib/get/work/coverart/{p.Movements.First().Work.Id}",
            //            ArtistName = p.Composition.Artist.Name,
            //            WorkName = p.Composition.Name,
            //            MovementCount = p.Movements.Count,
            //            Movements = p.Movements.ToDTO(parentName),
            //            FormattedDuration = p.Movements
            //                .Sum(x => x.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Duration)
            //                .FormatDuration()
            //        };
            //    case MusicStyles.IndianClassical:
            //        var raga = p.RagaPerformances.Select(x => x.Raga).Distinct().Single();
            //        var work = p.Movements.Select(x => x.Work).Distinct().Single();
            //        return new PerformanceDTO
            //        {
            //            Id = p.Id,
            //            Performers = p.GetAllPerformersCSV(),
            //            Year = p.Year,
            //            AlbumName = work.Name, // p.Movements.First().Work.Name,
            //            DisplayName = work.Name, // p.Movements.First().Work.Name,
            //            AlbumCoverArt = $"lib/get/work/coverart/{work.Id}",
            //            ArtistName = work.GetArtistNames(),
            //            WorkName = raga.Name,
            //            MovementCount = p.Movements.Count,
            //            Movements = p.Movements.ToDTO(parentName),
            //            FormattedDuration = p.Movements
            //                .Sum(x => x.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Duration)
            //                .FormatDuration()
            //        };
            //}
            //throw new Exception($"style {p.StyleId} does not use performances");

        }
        public static WorkDTO ToDTO(this Work w, bool full = false)
        {
            var artistId = w.Artists.Select(x => x.Id).First();
            if (!full)
            {
                return new WorkDTO
                {
                    Id = w.Id,
                    ArtistIdList =  w.Artists.Select(x => x.Id),
                    ArtistName = w.GetArtistNames(),//  w.Artists.Select(x => x.Name).ToCSV(),
                    OpusType = w.Type,
                    Name = w.Name,
                    Year = w.Year,
                    CoverArtUrl = $"lib/get/work/coverart/{w.Id}",
                    TrackCount = w.Tracks.Count(),
                    FormattedDuration = w.Tracks
                        .Sum(x => x.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Duration)
                        .FormatDuration()
                };
            }
            else
            {
                return new WorkDTO
                {
                    Id = w.Id,
                    ArtistIdList = w.Artists.Select(x => x.Id),
                    OpusType = w.Type,
                    Name = w.Name,
                    ArtistName = w.GetArtistNames(), // w.Artists.Select(x => x.Name).ToCSV(),
                    Year = w.Year,
                    CoverArtUrl = $"lib/get/work/coverart/{w.Id}",
                    TrackCount = w.Tracks.Count(),
                    Tracks = w.Tracks.Select(t => t.ToDTO()).OrderBy(x => x.Number).ToArray(),
                    FormattedDuration = w.Tracks
                        .Sum(x => x.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Duration)
                        .FormatDuration()
                };
            }
        }
        ///// <summary>
        ///// use ONLY for movements as tracks are renumbered
        ///// </summary>
        ///// <param name="movements"></param>
        ///// <returns></returns>
        //public static TrackDTO[] ToDTO(this IEnumerable<Track> movements, Composition composition)
        //{
        //    // Note: this version is only for movements
        //    var list = new List<TrackDTO>();
        //    foreach (var movement in movements.OrderBy((t) => t.Number))
        //    {
        //        var dto = movement.ToDTO();
        //        if (dto.Title.Contains(":"))
        //        {
        //            var parts = dto.Title.Split(":");
        //            if (parts[0].IsEqualIgnoreAccentsAndCase(composition.Name))
        //            {
        //                dto.Title = string.Join(":", parts.Skip(1));
        //            }
        //        }
        //        list.Add(dto);
        //        dto.Number = movement.MovementNumber;// list.Count();
        //    }
        //    return list.ToArray();
        //}
        /// <summary>
        /// use ONLY for movements as tracks are renumbered
        /// </summary>
        /// <param name="movements"></param>
        /// <returns></returns>
        public static TrackDTO[] ToDTO(this IEnumerable<Track> movements, string parentName)
        {
            // Note: this version is only for movements
            var performance = movements.Select(x => x.Performance).Distinct().Single();
            //var workName = performance.StyleId == MusicStyles.WesternClassical ?
            //    performance.Composition.Name :
            //    performance.RagaPerformances.Select(x => x.Raga).Distinct().Single().Name;
            var names = performance.GetNames();
            var titlePrefixes = movements.Select(m => m.Title.Split(":").First()).Select(x => x.Trim()).Distinct();
            var list = new List<TrackDTO>();
            foreach (var movement in movements.OrderBy((t) => t.Number))
            {
                var dto = movement.ToDTO();
                dto.WorkName = names.workName;
                if (dto.Title.Contains(":"))
                {
                    var parts = dto.Title.Split(":");
                    if (titlePrefixes.Count() == 1)
                    {
                        dto.Title = string.Join(":", parts.Skip(1));
                    }
                    else /*if (dto.Title.Contains(":"))*/
                    {
                        //var parts = dto.Title.Split(":");
                        if (parts[0].IsEqualIgnoreAccentsAndCase(parentName))
                        {
                            dto.Title = string.Join(":", parts.Skip(1));
                        }
                    } 
                }
                list.Add(dto);
                dto.Number = movement.MovementNumber;// list.Count();
            }
            return list.ToArray();
        }
        public static TrackDTO ToDTO(this Track t)
        {
            return new TrackDTO
            {
                Id = t.Id,
                WorkId = t.WorkId,
                Number = t.Number,
                Title = t.Title,
                DisplayName = t.Title,
                ArtistName = t.Work.GetArtistNames(), // t.Work.Artists.Select(x => x.Name).ToCSV(),
                AlbumName = t.Work.Name,
                CoverArtUrl = $"lib/get/work/coverart/{t.Work.Id}",
                MusicFileCount = t.MusicFiles.Count(),
                NumberQuality = t.NumberParsingStage.ToMetadataQuality(),
                TitleQuality = t.ParsingStage.ToMetadataQuality(),
                MusicFiles = t.MusicFiles.Select(mf => mf.ToDTO()).OrderByDescending(mf => mf.Rank)
            };
        }
        public static MusicFileDTO ToDTO(this MusicFile mf)
        {
            var t_m = new MusicFileDTO
            {
                Id = mf.Id,
                IsGenerated = mf.IsGenerated,
                Encoding = mf.Encoding,
                Duration = mf.Duration,
                IsFaulty = mf.IsFaulty,
                FormattedDuration = mf.Duration.FormatDuration(),
                BitRate = mf.GetBitRate().rate,
                BitsPerSample = mf.BitsPerSample,
                SampleRate = mf.SampleRate.HasValue ? mf.SampleRate / 1000.0 : null,
                AudioProperties = mf.GetAudioProperties()
            };
            t_m.Rank = mf.Rank();
            return t_m;
        }
        public static AudioDevice ToDTO(this Device d)
        {
            return new AudioDevice
            {
                Id = d.Id,
                Key = d.KeyName,
                Name = d.Name,
                DisplayName = d.DisplayName,
                Capability = new AudioCapability { MaxSampleRate = d.MaxSampleRate },
                Type = d.Type,
                Enabled = !d.IsDisabled,
                HostMachine = d.HostMachine,
                MACAddress = d.MACAddress,
                CanReposition = d.CanReposition
            };
        }
        public static DeviceStatusDTO ToDTO(this DeviceStatus ds, DeviceRuntime dr)
        {
            var ct = ds.CurrentTime.TotalMilliseconds;
            var tt = ds.TotalTime.TotalMilliseconds;
            var rt = tt - ct;
            return new DeviceStatusDTO
            {
                Key = ds.Key,
                //PlaylistName = dr.Playlist.Type == PlaylistType.UserCreated ? dr.Playlist.Name : string.Empty,
                //PlaylistType = dr.Playlist.Type,
                PlaylistPosition = dr.CurrentPosition,
                CurrentTime = ct,
                TotalTime = tt,
                RemainingTime = rt,
                FormattedCurrentTime = ct.FormatDuration(),
                FormattedTotalTime = tt.FormatDuration(),
                FormattedRemainingTime = rt.FormatDuration(),
                State = ds.State,
                Volume = ds.Volume,
                CommandSequence = dr.CommandSequenceNumber
            };
        }
        /// <summary>
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        public static PlaylistDTO ToDTO(this ExtendedPlaylist playlist)
        {
            var dto = new PlaylistDTO
            {
                Id = playlist.PlaylistId,
                DeviceKey = playlist.DeviceKey,// null, 
                PlaylistType = playlist.Type,
                PlaylistName = playlist.Name,
                Items = playlist.Items.Select(x => x.ToDTO()),
                TotalTime = playlist.Duration.TotalMilliseconds,
                FormattedTotalTime = playlist.Duration.ToDuration()
            };
            return dto;
        }
        /// <summary>
        /// this takes complete runtime inform ation from DeviceRuntime and creates a complete PlaylistDTO
        /// which includes the devicekey for the device where this playlist is running
        /// and PlaylistItems (and subitems) from PlaylistRunTime items in the DeviceRuntime
        /// cf. with PlaylistDTO ToDTO(this Playlist playlist)
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public static PlaylistDTO ToPlaylistDTO(this DeviceRuntime dr)
        {
            //var dto = new PlaylistDTO
            //{
            //    Id = dr.Playlist.Id,
            //    DeviceKey = dr.Key,
            //    PlaylistType = dr.Playlist.Type,
            //    PlaylistName = dr.Playlist.Name,
            //    Items = dr.Playlist.Items.Select(x => x.ToDTO())
            //};
            //dto.TotalTime = dto.Items.Sum(x => x.TotalTime);
            //dto.FormattedTotalTime = dto.TotalTime.FormatDuration();
            var dto = dr.ExtendedPlaylist.ToDTO();
            return dto;
        }
        public static PlaylistItemDTO ToDTO(this ExtendedPlaylistItem pli)
        {
            PlaylistItemDTO getBaseDTO(ExtendedPlaylistItem pli)
            {
                return new PlaylistItemDTO
                {
                    Position = pli.Position,
                    Titles = pli.Titles,
                    CoverArtUrl = pli.CoverArtUrl,
                    TotalTime = pli.Duration.TotalMilliseconds,
                    FormattedTotalTime = pli.Duration.ToDuration()
                };
            }
            PlaylistItemDTO getMultiTrackDTO(MultiTrackPlaylistItem mti)
            {
                var dto = getBaseDTO(mti);
                dto.Type = PlaylistRuntimeItemType.MultipleItems;
                dto.SubItems = mti.SubItems.Select(x => getSingleTrackDTO(x));
                return dto;
            }
            PlaylistItemDTO getSingleTrackDTO(SingleTrackPlaylistItem sti)
            {
                var dto = getBaseDTO(sti);
                dto.Type = PlaylistRuntimeItemType.SingleItem;
                dto.AudioProperties = sti.AudioProperties;
                dto.SampleRate = sti.SampleRate;
                dto.NotPlayableOnCurrentDevice = sti.NotPlayableOnCurrentDevice;
                return dto;
            }
            var dto = pli switch
            {
                //MultiTrackPlaylistItem mti => new PlaylistItemDTO { Type = PlaylistRuntimeItemType.MultipleItems, Position = mti.Position, Titles = mti.Titles, CoverArtUrl = mti.CoverArtUrl, TotalTime = mti.Duration.TotalMilliseconds, FormattedTotalTime = mti.Duration.ToDuration() },
                //SingleTrackPlaylistItem sti => new PlaylistItemDTO { Type = PlaylistRuntimeItemType.SingleItem, Position = sti.Position, Titles = sti.Titles, CoverArtUrl = sti.CoverArtUrl, TotalTime = sti.Duration.TotalMilliseconds, FormattedTotalTime = sti.Duration.ToDuration(), AudioProperties = sti.AudioProperties, SampleRate = sti.SampleRate, NotPlayableOnCurrentDevice = sti.NotPlayableOnCurrentDevice },
                MultiTrackPlaylistItem mti => getMultiTrackDTO(mti),
                SingleTrackPlaylistItem sti => getSingleTrackDTO(sti),
                _ => throw new Exception()
            };
            return dto;
        }
        //public static PlaylistItemDTO ToDTO(this PlaylistItemRuntime pli, bool isSubitem = false)
        //{
        //    var dto = new PlaylistItemDTO
        //    {
        //        //Id = pli.Id,
        //        Position = pli.Position,
        //        Type = pli.Type,
        //        NotPlayableOnCurrentDevice = pli.NotPlayableOnCurrentDevice,
        //        Titles = pli.Titles,
        //        CoverArtUrl = pli.CoverArtUrl,
        //        AudioProperties = pli.AudioProperties,
        //        SampleRate = pli.SampleRate,
        //        //Sequence = pli.Sequence,
        //        TotalTime = pli.TotalTime,
        //        FormattedTotalTime = pli.FormattedTotalTime,
        //        IsSubitem = isSubitem,
        //        SubItems = pli.SubItems?.Select(x => x.ToDTO(true))
        //    };
        //    return dto;
        //}
        public static IOpusDetails ToDetails(this Performance performance)
        {
            return new PerformanceDetails
            {
                Id = performance.Id,
                OpusName = performance.GetParentEntityName(),//.Composition.Name,
                ArtistName = performance. GetParentArtistsName(), //.Composition.Artist.Name,
                TrackDetails = performance.Movements.ToDetails(),
                CompressedArtistName = performance.GetParentArtistsName(true),// .Composition.Artist.CompressedName,
                CompressedOpusName = performance.GetParentEntityName(true), //.Composition.CompressedName,
                CompressedPerformanceName = performance.CompressedName
            };
        }
        public static IOpusDetails ToDetails(this Work work)
        {
            return new WorkDetails
            {
                Id = work.Id,
                OpusName = work.Name,
                TrackDetails = work.Tracks.ToDetails(),
                CompressedOpusName = work.CompressedName
            };
        }
        public static TrackDetail[] ToDetails(this IEnumerable<Track> tracks)
        {
            var list = new List<TrackDetail>();
            var index = 0;
            foreach (var track in tracks.OrderBy((t) => t.Number))
            {
                var td = new TrackDetail
                {
                    Id = track.Id,
                    Number = ++index,
                    Title = track.Title,
                    MusicFileCount = track.MusicFiles.Count()
                };
                var musicFiles = new List<MusicFileDetail>();
                foreach (var mf in track.MusicFiles.Where(x => !x.IsGenerated))
                {
                    var name = System.IO.Path.GetFileName(mf.File);
                    var rpath = !string.IsNullOrWhiteSpace(mf.PartName) ?
                            System.IO.Path.Combine(mf.OpusPath, mf.PartName) : System.IO.Path.Combine(mf.OpusPath);
                    var md = new MusicFileDetail
                    {
                        Id = mf.Id,
                        File = System.IO.Path.Combine(rpath, name),
                        FileLength = mf.FileLength,
                        FileLastWriteTimeString = mf.FileLastWriteTimeUtc.ToDefaultWithTime()
                    };
                    musicFiles.Add(md);
                }
                td.MusicFiles = musicFiles;
                list.Add(td);
            }
            return list.ToArray();
        }
        public static StatsDTO ToStats(this MusicStyles style,  IEnumerable<Artist> artists)
        {
            (int artistCount, int albumCount, int trackCount, TimeSpan duration) popularTotals(IEnumerable<Artist> artists)
            {
                var albums = artists.SelectMany(x => x.ArtistWorkList.Select(x => x.Work));
                var tracks = albums.SelectMany(x => x.Tracks).ToArray();
                var duration = TimeSpan.FromMilliseconds(tracks.Select(t => t.GetBestQuality()).Sum(mf => mf.Duration ?? 0));
                return (artists.Count(), albums.Count(), tracks.Count(), duration);
                //return ($"{artists.Count()} artists, {albums.Count()} works, {tracks.Count()} tracks", $"{duration.ToDefault()}");
            }
            (int artistCount, int compositionCount, int performanceCount, int movementCount, TimeSpan duration) westernClassicalTotals(IEnumerable<Artist> artists)
            {
                var compositions = artists.SelectMany(a => a.Compositions);
                var performances = compositions.SelectMany(c => c.Performances);
                var movements = performances.SelectMany(p => p.Movements);
                var duration = TimeSpan.FromMilliseconds(movements.Select(t => t.GetBestQuality()).Sum(mf => mf.Duration ?? 0));
                return (artists.Count(), compositions.Count(), performances.Count(), movements.Count(), duration);
                //return ($"{artists.Count()} artists, {compositions.Count()} compositions, {performances.Count()} performances", $"{duration.ToDefault()}");
            }
            switch (style)
            {
                case MusicStyles.Popular:
                    var (a, b, c, d) = popularTotals(artists);
                    return new PopularStatsDTO { ArtistCount = a, AlbumCount = b, TrackCount = c, Duration = d };
                case MusicStyles.WesternClassical:
                    var (e, f, g, h, i) = westernClassicalTotals(artists);
                    return new WesternClassicalStatsDTO { ArtistCount = e, CompositionCount = f, PerformanceCount = g, MovementCount = h, Duration = i };
                default:
                    throw new ArgumentException($"method invalid for {style}", "style");
            }
        }
        public static StatsDTO ToStats(this MusicStyles style, IEnumerable<RagaPerformance> rpList)
        {
            (int artistCount, int ragaCount, int performanceCount, int movementCount, TimeSpan duration) indianClassicalTotals(IEnumerable<RagaPerformance> rpList)
            {
                var artists = rpList.Select(rp => rp.Artist).Distinct();
                var ragas = rpList.Select(rp => rp.Raga).Distinct();
                var performances = rpList.Select(rp => rp.Performance).Distinct();
                var movements = performances.SelectMany(p => p.Movements);
                var duration = TimeSpan.FromMilliseconds(movements.Select(t => t.GetBestQuality()).Sum(mf => mf.Duration ?? 0));
                return (artists.Count(), ragas.Count(), performances.Count(), movements.Count(), duration);
                //return ($"{artists.Count()} artists, {ragas.Count()} ragas, {performances.Count()} performances", $"{duration.ToDefault()}");
            }
            switch (style)
            {
                case MusicStyles.IndianClassical:
                    var (a, b, c, d, e) = indianClassicalTotals(rpList);
                    return new IndianClassicalStatsDTO { ArtistCount = a, RagaCount = b, PerformanceCount= c, MovementCount = d,  Duration = e };
                default:
                    throw new ArgumentException($"method invalid for {style}", "style");
            }
        }
    }
}

