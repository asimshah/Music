using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Fastnet.Music.Metatools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public static class tagExtensions
    {
        public static int ToNumber(this string text)
        {
            return Int32.Parse(text);
        }
        //public static async Task SaveCustomTags(this IEnumerable<PerformanceTEO> list)
        //{
        //    Debug.Assert(list.Count() != 0);
        //    //Debug.Assert(list.Select(x => x.PathToMusicFiles).Distinct(StringComparer.CurrentCultureIgnoreCase).Count() == 1);
        //    var tagFile = "";// Path.Combine(list.First().PathToMusicFiles, MusicTags.TagFile);
        //    var customList = new List<WesternClassicalMusicTags>();
        //    foreach (var item in list)
        //    {
        //        var performers = new List<string>();
        //        foreach (var performer in item.PerformerTag.Values.Where(x => x.Selected))
        //        {
        //            performers.Add(performer.Value);
        //        }
        //        foreach (var file in item.MovementList)
        //        {
        //            var cpt = new WesternClassicalMusicTags
        //            {
        //                //Album = item.AlbumTag.GetValue<string>(),// .GetStringValue(),
        //                Composer = item.ComposerTag.GetValue<string>(),//.GetStringValue(),
        //                Composition = item.CompositionTag.GetValue<string>(),//.GetStringValue(),
        //                Orchestra = item.OrchestraTag.GetValue<string>(),//.GetStringValue(),
        //                Conductor = item.ConductorTag.GetValue<string>(),//.GetStringValue(),
        //                //Year = item.YearTag.GetValue<int>(),//.GetNumericValue(),
        //                Performers = performers,
        //                Filename = file.File,
        //                TrackNumber = file.TrackNumberTag.GetValue<int>(),//.GetNumericValue(),// file.TrackNumber,
        //                Title = file.TitleTag.GetValue<string>(),//.GetStringValue(), // file.Title,
        //            };
        //            customList.Add(cpt);
        //        }
        //    }
        //    var text = customList.ToJson(true);
        //    await System.IO.File.WriteAllTextAsync(tagFile, text);
        //}

        public static async Task<PopularAlbumTEO> ToPopularAlbumTEO(this Work work, MusicOptions musicOptions)
        {
            var teo = new PopularAlbumTEO(musicOptions);
            await teo.Load(work);
            return teo;
        }
        public static async Task<WesternClassicalAlbumTEO> ToWesternClassicalAlbumTEO(this Work work, MusicOptions musicOptions)
        {
            var teo = new WesternClassicalAlbumTEO(musicOptions);
            await teo.Load(work);
            return teo;
        }
        public static async Task<WesternClassicalAlbumTEO> ToWesternClassicalAlbumTEO(this Performance performance, MusicOptions musicOptions)
        {
            await Task.Delay(0);
            var works = performance.Movements.Select(x => x.Work).Distinct();
            var work = works.First();
            return await work.ToWesternClassicalAlbumTEO(musicOptions);
        }
    }

}
