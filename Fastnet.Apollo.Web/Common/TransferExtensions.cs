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
    //public class MyComparer : IEqualityComparer<string>
    //{
    //    ///// <summary>
    //    ///// 
    //    ///// </summary>
    //    ///// <param name="x"></param>
    //    ///// <param name="y"></param>
    //    ///// <returns></returns>
    //    //public override int Compare(string x, string y)
    //    //{
    //    //    var r = string.Compare(x, y, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase);
    //    //    Debug.WriteLine($"{x} against {y}, result {r}");
    //    //    return r;
    //    //}
    //    ///// <summary>
    //    ///// 
    //    ///// </summary>
    //    ///// <param name="x"></param>
    //    ///// <param name="y"></param>
    //    ///// <returns></returns>
    //    //public override bool Equals(string x, string y)
    //    //{
    //    //    return Compare(x, y) == 0;
    //    //}
    //    ///// <summary>
    //    ///// 
    //    ///// </summary>
    //    ///// <param name="obj"></param>
    //    ///// <returns></returns>
    //    //public override int GetHashCode(string obj)
    //    //{
    //    //    return obj.GetHashCode();
    //    //}
    //    public bool Equals(string x, string y)
    //    {
    //        if(object.ReferenceEquals(x, y)) {
    //            return true;
    //        }
    //        if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
    //        {
    //            return false;
    //        }
    //        var r = string.Compare(x, y, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase);
    //        //var r = string.Compare(x.RemoveDiacritics(), y.RemoveDiacritics(), CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);
    //        Debug.WriteLine($"{x} against {y}, result {r}");
    //        return r == 0;
    //    }

    //    public int GetHashCode(string obj)
    //    {
    //        return obj.RemoveDiacritics().GetHashCode();
    //    }
    //}
    //public static class core_extensions // move the to fastnet.core
    //{
    //    //public static string GetLastName(this string text)
    //    //{
    //    //    var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    //    //    return parts.Length > 1 ? parts.Last() : parts.First();
    //    //}
    //}
    public static partial class TransferExtensions
    {
        private static ILogger log = ApplicationLoggerFactory.CreateLogger("Fastnet.Apollo.Web.TransferExtensions");
        //public static StyleDTO ToDTO(this Style style, MusicOptions musicOptions)
        //{
        //    StyleInformation si = musicOptions.Styles.Single(x => x.Style == style.Id);
        //    return new StyleDTO
        //    {
        //        Id = si.Style,
        //        Enabled = si.Enabled,
        //        DisplayName = si.Style.ToDescription()
        //    };
        //}
        public static ArtistDTO ToDTO(this Artist a)
        {
            return new ArtistDTO
            {
                Id = a.Id,
                Name = a.Name,
                Lastname = a.Name.GetLastName(),
                DisplayName = a.Name,
                ArtistType = a.Type,
                Styles = a.ArtistStyles.Select(s => s.StyleId),
                WorkCount = a.Works.Count(w => w.Type != OpusType.Singles),
                SinglesCount = a.Works.Where(w => w.Type == OpusType.Singles).SelectMany(x => x.Tracks).Count(),
                CompositionCount = a.Compositions?.Count() ?? 0,
                PerformanceCount = a.Compositions?.SelectMany(x => x.Performances).Count() ?? 0,
                Quality = a.ParsingStage.ToMetadataQuality(),
                ImageUrl = $"lib/get/artist/imageart/{a.Id}"
            };
        }
        //public static CompositionDTO ToDTO(this Composition c)
        //{
        //    return new CompositionDTO
        //    {
        //        Id = c.Id,
        //        Name = c.Name,
        //        DisplayName = c.Name
        //    };
        //}
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
                Performances = c.Performances.Select(p => p.ToDTO(true)).ToArray()
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="full">movements are included if true</param>
        /// <returns></returns>
        public static PerformanceDTO ToDTO(this Performance p, bool full = false)
        {
            //string getFullPerformersText()
            //{
            //    var list = new List<string>();
            //    if(p.Performers != null && p.Performers.Length > 0)
            //    {
            //        list.Add(p.Performers);
            //    }
            //    if (p.Orchestras != null && p.Orchestras.Length > 0)
            //    {
            //        list.Add(p.Orchestras);
            //    }
            //    if (p.Conductors != null && p.Conductors.Length > 0)
            //    {
            //        list.Add(p.Conductors);
            //    }
            //    return string.Join(", ", list.Where(x => x.Trim().Length > 0));
            //}
            if (!full)
            {
                return new PerformanceDTO
                {
                    Id = p.Id,
                    Performers = p.GetAllPerformersCSV(),// getFullPerformersText(),// p.Performers,
                    Year = p.Year,
                    AlbumName = p.Movements.First().Work.Name,
                    DisplayName = p.Movements.First().Work.Name,
                    AlbumCoverArt = $"lib/get/work/coverart/{p.Movements.First().Work.Id}",// p.Movements.First().Work
                    MovementCount = p.Movements.Count,
                    FormattedDuration = p.Movements
                    .Sum(x => x.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Duration)
                    .FormatDuration()
                };
            }
            return new PerformanceDTO
            {
                Id = p.Id,
                Performers = p.GetAllPerformersCSV(),// getFullPerformersText(), //p.Performers,
                Year = p.Year,
                AlbumName = p.Movements.First().Work.Name,
                DisplayName = p.Movements.First().Work.Name,
                AlbumCoverArt = $"lib/get/work/coverart/{p.Movements.First().Work.Id}",// p.Movements.First().Work
                MovementCount = p.Movements.Count,
                Movements = p.Movements.ToDTO(),
                FormattedDuration = p.Movements
                    .Sum(x => x.MusicFiles.OrderByDescending(mf => mf.Rank()).First().Duration)
                    .FormatDuration()
            };
        }
        public static WorkDTO ToDTO(this Work w, bool full = false)
        {
            if (!full)
            {
                return new WorkDTO
                {
                    Id = w.Id,
                    ArtistId = w.ArtistId,
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
                    ArtistId = w.ArtistId,
                    OpusType = w.Type,
                    Name = w.Name,
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
        /// <summary>
        /// use ONLY for movements as tracks are renumbered
        /// </summary>
        /// <param name="movements"></param>
        /// <returns></returns>
        public static TrackDTO[] ToDTO(this IEnumerable<Track> movements)
        {
            // Note: this version is only for movements
            var list = new List<TrackDTO>();
            foreach (var movement in movements.OrderBy((t) => t.Number))
            {
                var dto = movement.ToDTO();
                if (dto.Title.Contains(":"))
                {
                    var parts = dto.Title.Split(":");
                    if (parts[0].IsEqualIgnoreAccentsAndCase(movement.CompositionName))
                    {
                        dto.Title = string.Join(":", parts.Skip(1));
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
                ArtistId = t.Work.ArtistId,
                Number = t.Number,
                Title = t.Title,
                DisplayName = t.Title,
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
                Encoding = mf.Encoding,
                Duration = mf.Duration,
                IsFaulty = mf.IsFaulty,
                FormattedDuration = mf.Duration.FormatDuration(),// mf.Duration.HasValue ? FormatDuration(mf.Duration.Value) : "",
                BitRate = mf.GetBitRate().rate,// getBitRate(mf).rate,
                BitsPerSample = mf.BitsPerSample,
                SampleRate = mf.SampleRate.HasValue ? mf.SampleRate / 1000.0 : null,
                AudioProperties = mf.GetAudioProperties() // getAudioProperties(mf),
            };
            t_m.Rank = mf.Rank();// getRanking(t_m);
            return t_m;
        }
        public static AudioDevice /*DeviceDTO*/ ToDTO(this Device d)
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
            //(PlaylistItemRuntime major, PlaylistItemRuntime minor) findItem(PlaylistPosition pos)
            //{
            //    PlaylistItemRuntime maj = dr.Playlist.Items[pos.Major - 1];
            //    PlaylistItemRuntime min = null;
            //    if (maj.Type == PlaylistRuntimeItemType.MultipleItems && pos.Minor > 0)
            //    {
            //        min = maj.SubItems.ToArray()[pos.Minor - 1];
            //    }
            //    return (maj, min);
            //}
            //(PlaylistItemRuntime majorPli, PlaylistItemRuntime minorPli) = dr.CurrentPosition.IsUnset() ? (null, null) : findItem(dr.CurrentPosition);
            var ct = ds.CurrentTime.TotalMilliseconds;
            var tt = ds.TotalTime.TotalMilliseconds;
            var rt = tt - ct;
            return new DeviceStatusDTO
            {
                Key = ds.Key,
                PlaylistPosition = dr.CurrentPosition,
                //Title = majorPli?.Type == PlaylistRuntimeItemType.SingleItem ? majorPli.Title : minorPli?.Title,
                //CoverArtUrl = majorPli?.CoverArtUrl,
                CurrentTime = ct,
                TotalTime = tt,
                RemainingTime = rt,
                FormattedCurrentTime = ct.FormatDuration(),
                FormattedTotalTime = tt.FormatDuration(),
                FormattedRemainingTime = rt.FormatDuration(),
                //AudioProperties = majorPli?.Type == PlaylistRuntimeItemType.SingleItem ? majorPli?.AudioProperties : minorPli?.AudioProperties,
                State = ds.State,
                Volume = ds.Volume,
                CommandSequence = dr.CommandSequenceNumber
            };
        }
        public static PlaylistItemDTO ToDTO(this PlaylistItemRuntime pli, bool isSubitem = false)
        {
            var dto = new PlaylistItemDTO
            {
                Id = pli.Id,
                Position = pli.Position,
                Type = pli.Type,
                //Title = pli.Title,
                NotPlayableOnCurrentDevice = pli.NotPlayableOnCurrentDevice,
                Titles = pli.Titles,
                CoverArtUrl = pli.CoverArtUrl,
                AudioProperties = pli.AudioProperties,
                SampleRate = pli.SampleRate,
                Sequence = pli.Sequence,
                TotalTime = pli.TotalTime,
                FormattedTotalTime = pli.FormattedTotalTime,
                IsSubitem = isSubitem,
                SubItems = pli.SubItems?.Select(x => x.ToDTO(true))
            };
            //if(pli.)
            return dto;
        }
        public static IOpusDetails ToDetails(this Performance performance)
        {
            return new PerformanceDetails
            {
                Id = performance.Id,
                OpusName = performance.Composition.Name,
                ArtistName = performance.Composition.Artist.Name,
                TrackDetails = performance.Movements.ToDetails(),
                CompressedArtistName = performance.Composition.Artist.CompressedName,
                CompressedOpusName = performance.Composition.CompressedName,
                CompressedPerformanceName = performance.CompressedName
            };
        }
        public static IOpusDetails ToDetails(this Work work)
        {
            return new WorkDetails
            {
                Id = work.Id,
                OpusName = work.Name,
                ArtistName = work.Artist.Name,
                TrackDetails = work.Tracks.ToDetails(),
                CompressedArtistName = work.Artist.CompressedName,
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
                        File = System.IO.Path.Combine(rpath, name),// mf.File,
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

    }

}

