using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public class IndianClassicalAlbumSet : BaseAlbumSet // PopularMusicAlbumSet
    {
        static string[] honourifics = new string[] { "Pt.", "Pt", "Pandit", "Ustad", "Ustaad", "Shri", "Shrimati" };
        internal IndianClassicalAlbumSet(MusicDb db, MusicOptions musicOptions,  IEnumerable<MusicFile> musicFiles, TaskItem taskItem)
            : base(db, musicOptions, MusicStyles.IndianClassical, musicFiles, taskItem)
        {

            //this.ArtistName = OpusType == OpusType.Collection ?
            //    "Various Artists"
            //    : MusicOptions.ReplaceAlias(FirstFile.GetArtistName() ?? FirstFile.Musician);
            //this.AlbumName = FirstFile.GetAlbumName() ?? FirstFile.OpusName;

        }

        public override Task<BaseCatalogueResult> CatalogueAsync()
        {
            throw new System.NotImplementedException();
        }

        protected override string GetName()
        {
            throw new System.NotImplementedException();
        }
        protected override Track CreateTrackIfRequired(Work album, MusicFile mf, string title)
        {
            var alphamericTitle = title.ToAlphaNumerics();
            //var track = album.Tracks.SingleOrDefault(x => x.Title == alphamericTitle && x.CompositionName.IsEqualIgnoreAccentsAndCase(mf.GetWorkName()));
            var tracks = album.Tracks.Where(x => x.Title == alphamericTitle /*&& x.CompositionName.IsEqualIgnoreAccentsAndCase(mf.GetWorkName())*/);
            if (tracks.Count() > 1)
            {
                Debugger.Break();
            }
            var track = tracks.FirstOrDefault();
            if (track == null)
            {
                track = new Track
                {
                    Work = album,
                    //CompositionName = mf.GetRagaName(),
                    OriginalTitle = mf.Title,
                    UID = Guid.NewGuid(),
                };
                album.Tracks.Add(track);
            }
            return track;
        }

        //private IEnumerable<string> GetArtistNames()
        //{
            
        //    IEnumerable<string> extractArtistNames(MusicFile mf)
        //    {
        //        IEnumerable<string> parseValue(string text)
        //        {
        //            var names = new List<string>();
        //            var parts = text.Split(new string[] { "&", "|", ",", ";" }, System.StringSplitOptions.RemoveEmptyEntries)
        //                .Select(x => x.Trim());
        //            foreach(var part in parts)
        //            {
        //                IEnumerable<string> nameParts = part.Split(" ", System.StringSplitOptions.RemoveEmptyEntries);
        //                if (honourifics.Any(x => string.Compare(x, nameParts.First(), true) == 0))
        //                {
        //                    nameParts = nameParts.Skip(1);
        //                }
        //                names.Add(MusicOptions.ReplaceAlias(string.Join(" ", nameParts)));
        //            }
        //            return names;
        //        }
        //        var values = new string[] { mf.GetTagValue<string>("Artist"), mf.GetTagValue<string>("Artists"), };
        //        var names = values.SelectMany(x => parseValue(x));
        //        return names;
        //    }
        //    return MusicFiles.SelectMany(mf => extractArtistNames(mf));
        //}
    }
}
