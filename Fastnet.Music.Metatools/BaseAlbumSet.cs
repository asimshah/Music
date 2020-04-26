using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public abstract class BaseAlbumSet : BaseMusicSet
    {
        private static class CollectionNames
        {
            public const string Popular = "Various Artists";
            public const string WesternClassicalArtistName = "Various Composers";
            public const string IndianClassicalArtistName = "Various Artists";
        }
        public string AlbumName { get; protected internal set; }
        internal BaseAlbumSet() : base()
        {

        }
        internal BaseAlbumSet(MusicDb db, MusicOptions musicOptions, MusicStyles musicStyle, IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
            : base(db, musicOptions, musicStyle, musicFiles, taskItem)
        {
            var albumNames = MusicFiles.Select(x => x.GetAlbumName()).Distinct(StringComparer.CurrentCultureIgnoreCase);
            if (albumNames.Count() > 1)
            {
                var opusPaths = MusicFiles.Select(x => x.OpusPath).Distinct();
                Debug.Assert(opusPaths.Count() == 1, $"{taskItem} more than 1 opus path: {opusPaths.ToCSV()}");
                var temp = opusPaths.First();
                var parts = temp.Split("\\");
                if (parts.Count() > 1)
                {
                    temp = parts[1];
                }
                AlbumName = temp;
                log.Information($"{taskItem} multiple album names found ({albumNames.ToCSV()}, changed to folder name {temp}");
            }
            else
            {
                AlbumName = albumNames.First();
            }

            if (string.Compare(this.AlbumName, "Greatest Hits", true) == 0)
            {
                // prefix with artist name as there are so many called "Greatest Hits"
                this.AlbumName = $"{GetArtistsCSV()} Greatest Hits";
            }
            if(OpusType == OpusType.Collection)
            {
                artistPerformers = new MetaPerformer[]
                {
                    musicStyle switch 
                    {
                        MusicStyles.Popular => new MetaPerformer(PerformerType.Artist, CollectionNames.Popular),
                        MusicStyles.WesternClassical => new MetaPerformer(PerformerType.Artist, CollectionNames.WesternClassicalArtistName),
                        MusicStyles.IndianClassical => new MetaPerformer(PerformerType.Artist, CollectionNames.IndianClassicalArtistName),
                        _ => throw new Exception($"Style {musicStyle} not supported yet")
                    }
                }.ToList();
            }
        }
        protected virtual async Task<BaseCatalogueResult> CatalogueAsync(Func<CatalogueStatus, Work, BaseCatalogueResult> catalogueResult)
        {
            var artists = await GetArtistsAsync(artistPerformers);
            var album = GetWork(artists);
            var (result, tracks) = CatalogueTracks(album);
            await UpdateAlbumCover(album);
            await InitiateResampling(album.UID.ToString());
            var cr = catalogueResult(result, album);
            return cr;
        }
        private Work GetWork(IEnumerable<Artist> artists)
        {
            var alphamericName = AlbumName.ToAlphaNumerics();
            var artistIdList = artists.Select(a => a.Id);
            var work = MusicDb.ArtistWorkList
                .Where(aw => aw.Work.AlphamericName == alphamericName && artistIdList.Contains(aw.Artist.Id))
                .Select(aw => aw.Work).SingleOrDefault();
            if (work == null)
            {
                work = new Work
                {
                    StyleId = this.MusicStyle,
                    Name = AlbumName,
                    AlphamericName = alphamericName,
                    Type = OpusType,
                    OriginalName = AlbumName,
                    Mood = string.Empty,
                    PartName = string.Empty,
                    PartNumber = 0,
                    UID = Guid.NewGuid(),
                    LastModified = DateTimeOffset.Now,
                    Year = year
                };
                foreach (var artist in artists)
                {
                    MusicDb.AddWork(artist, work);
                }
            }
            return work;
        }
        protected override string GetName()
        {
            return $"{GetArtistsCSV()}:{AlbumName}";
        }
        protected string GetArtistsCSV()
        {
            return string.Join(", ", artistPerformers.Select(x => x.Name));
        }
        protected (CatalogueStatus status, IEnumerable<Track> tracks) CatalogueTracks(Work album)
        {
            Debug.Assert(MusicDb != null);
            var tracks = new List<Track>();
            var result = CatalogueStatus.Success;
            var filesByPart = MusicFiles.GroupBy(x => x.PartNumber);
            int count = 0;
            foreach (var group in filesByPart.OrderBy(x => x.Key))
            {
                var files = group.Select(x => x).ToArray();
                for (int i = 0; i < files.Count(); ++i)
                {
                    var mf = files[i];
                    var track = GetTrack(album, mf, i, count);
                    tracks.Add(track);
                }
                count += files.Count();
            }
            return (result, tracks);
        }
        protected abstract Track CreateTrackIfRequired(Work album, MusicFile mf, string title);
        protected virtual string GetTitle(MusicFile mf)
        {
            string title = /*fileteo?.TitleTag.GetValue<string>() ??*/ mf.GetTagValue<string>("Title");
            if (title.Contains(':'))
            {
                var parts = title.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts[0].IsEqualIgnoreAccentsAndCase(AlbumName))
                {
                    title = string.Join(":", parts.Skip(1)).Trim();
                    log.Debug($"Title {string.Join(":", parts)} changed to {title}");
                }
            }
            return title;
        }
        protected async Task UpdateAlbumCover(Work album)
        {
            var cover = album.GetMostRecentOpusCoverFile(MusicOptions);
            if (cover != null)
            {
                album.Cover = await cover.GetImage();
                album.LastModified = DateTimeOffset.Now;
            }
        }
        protected async Task InitiateResampling(string taskString)
        {
            if (!MusicOptions.DisableResampling)
            {
                if (this.MusicFiles.All(f => f.Encoding == EncodingType.flac))
                {
                    var now = DateTimeOffset.Now;
                    var resamplingTask = new TaskItem
                    {
                        Status = Core.TaskStatus.Pending,
                        Type = TaskType.ResampleWork,
                        CreatedAt = now,
                        ScheduledAt = now,
                        MusicStyle = this.MusicStyle,
                        TaskString =  taskString //album.UID.ToString()
                    };
                    await MusicDb.TaskItems.AddAsync(resamplingTask);
                }
            }
        }
        private Track GetTrack(/*Artist artist, */ Work album, MusicFile mf, int index, int totalInPreviousParts)
        {
            Debug.Assert(mf.IsGenerated == false);
            try
            {
                var basePath = mf.GetRootPath();
                var relativePath = Path.GetRelativePath(basePath, mf.File);
                var trackNumber = mf.GetTagIntValue("TrackNumber") ?? 0;
                if (trackNumber != (index + totalInPreviousParts + 1))
                {
                    log.Debug($"{GetName()}, track number changing from {trackNumber} to {index + totalInPreviousParts + 1}");
                    trackNumber = index + totalInPreviousParts + 1;
                }
                string title = GetTitle(mf);
                var track = CreateTrackIfRequired(album, mf, title);
                track.Title = title;
                track.AlphamericTitle = title.ToAlphaNumerics();
                track.Number = trackNumber;
                track.LastModified = DateTimeOffset.Now;

                var tmpMf = track.MusicFiles.SingleOrDefault(x => x.File.IsEqualIgnoreAccentsAndCase(mf.File));
                if (tmpMf == null)
                {
                    // this music file is not in a track
                    track.MusicFiles.Add(mf);
                    mf.Track = track; //NB: also set the track becuase I need this later even though SaveChanges will not have been called
                }
                mf.ParsingStage = MusicFileParsingStage.Catalogued;
                mf.LastCataloguedAt = DateTimeOffset.Now;
                return track;
            }
            catch (Exception xe)
            {
                log.Error($"[{taskItem}] {xe.Message}");
                throw;
            }
        }
    }
}
