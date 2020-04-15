using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public class PopularMusicAlbumSet : MusicSet 
    {
        public string ArtistName { get; protected internal set; }
        public string AlbumName { get; protected internal set; }
        //public int YearNumber { get; protected internal set; }
        /// <summary>
        /// internal use by WesternClassicalAlbumSet only
        /// </summary>
        /// <param name="db"></param>
        /// <param name="musicOptions"></param>
        /// <param name="musicStyle"></param>
        /// <param name="musicFiles"></param>
        /// <param name="taskItem"></param>
        internal PopularMusicAlbumSet(MusicDb db, MusicOptions musicOptions, MusicStyles musicStyle,
            IEnumerable<MusicFile> musicFiles, TaskItem taskItem) : base(db, musicOptions, musicStyle, musicFiles, taskItem)
        {

        }
        internal PopularMusicAlbumSet(MusicDb db, MusicOptions musicOptions, string artistName, string albumName,
            IEnumerable<MusicFile> musicFiles, TaskItem taskItem) : base(db, musicOptions, MusicStyles.Popular, musicFiles, taskItem)
        {
            this.ArtistName = OpusType == OpusType.Collection ?
                "Various Artists"
                : MusicOptions.ReplaceAlias(FirstFile.GetArtistName() ?? artistName);
            this.AlbumName = FirstFile.GetAlbumName() ?? albumName;
            //this.YearNumber = FirstFile.GetYear() ?? 0;

            if (string.Compare(this.AlbumName, "Greatest Hits", true) == 0)
            {
                // prefix with artist name as there are so many called "Greatest Hits"
                this.AlbumName = $"{ArtistName} Greatest Hits";
            }
        }
        protected override string GetName()
        {
            return $"{ArtistName}:{AlbumName}";
        }
        /// <summary>
        /// Create a catalogue of tracks using the artist and album names 
        /// </summary>
        /// <returns></returns>
        public override async Task<CatalogueResultBase> CatalogueAsync()
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(ArtistName));
            Debug.Assert(!string.IsNullOrWhiteSpace(AlbumName));
            var artist = await GetArtistAsync(ArtistName);
            var album = GetWork(artist, AlbumName, year);
            var (result, tracks) = CatalogueTracks(artist, album);
            var cover = album.GetMostRecentOpusCoverFile(MusicOptions);
            if (cover != null)
            {
                album.Cover = await cover.GetImage();
                album.LastModified = DateTimeOffset.Now;
            }
            TaskItem resamplingTask = null;
            if (!MusicOptions.DisableResampling)
            {
                if (this.MusicFiles.All(f => f.Encoding == EncodingType.flac))
                {
                    var now = DateTimeOffset.Now;
                    resamplingTask = new TaskItem
                    {
                        Status = Core.TaskStatus.Pending,
                        Type = TaskType.ResampleWork,
                        CreatedAt = now,
                        ScheduledAt = now,
                        MusicStyle = this.MusicStyle,
                        TaskString = album.UID.ToString()
                    };
                    await MusicDb.TaskItems.AddAsync(resamplingTask);
                }
            }
            var cr = new PopularCatalogueResult(this, result, album, resamplingTask);
            //var cr = CatalogueResult.Create(this, result, album);// { MusicSet = this, Status = result, Artist = artist, Work = album, Tracks = tracks };
            //if (!MusicOptions.DisableResampling)
            //{
            //    if (this.MusicFiles.All(f => f.Encoding == EncodingType.flac))
            //    {
            //        var now = DateTimeOffset.Now;
            //        cr.TaskItem = new TaskItem
            //        {
            //            Status = Core.TaskStatus.Pending,
            //            Type = TaskType.ResampleWork,
            //            CreatedAt = now,
            //            ScheduledAt = now,
            //            MusicStyle = this.MusicStyle,
            //            TaskString = album.UID.ToString()
            //        };
            //        await MusicDb.TaskItems.AddAsync(cr.TaskItem);
            //    } 
            //}
            return cr;
        }
        public override string ToString()
        {
            return $"{this.GetType().Name}::{ArtistName}::{AlbumName}::{MusicFiles.Count()} files";
        }
    }
}
