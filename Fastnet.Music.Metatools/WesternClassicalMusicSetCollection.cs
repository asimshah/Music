using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// Western classsical music files are grouped by composition within artist
    /// </summary>
    public class WesternClassicalMusicSetCollection : BaseMusicSetCollection<WesternClassicalAlbumSet, WesternClassicalCompositionSet>
    {
        public WesternClassicalMusicSetCollection(MusicOptions musicOptions, MusicDb musicDb,
            OpusFolder musicFolder, List<MusicFile> files, TaskItem taskItem) : base(musicOptions, musicDb, musicFolder, files, taskItem)
        {

        }
        protected override (string firstLevel, string secondLevel) GetPartitioningKeys(MusicFile mf)
        {
            var allPerformers = mf.GetAllPerformers(musicOptions);
            var composerPerformers = allPerformers.Where(x => x.Type == PerformerType.Composer);
            Debug.Assert(composerPerformers.Count() <= 1, $"{taskItem} more than 1 composer name: {composerPerformers.Select(x => x.Name).ToCSV()}");
             var composer = composerPerformers.FirstOrDefault();
            if(composer == null)
            {
                var artistPerformers = allPerformers.Where(x => x.Type == PerformerType.Artist);
                if (artistPerformers.Count() == 0)
                {
                    log.Error($"{taskItem} neither composer nor artist found");
                }
                else
                {
                    composer = artistPerformers.First();
                    log.Debug($"{taskItem} no composer found, using {composer.Name}");
                }
            }
            var composition = mf.GetWorkName().ToAlphaNumerics();
            return (composer.Name, composition);
        }
        protected override (string firstLevel, string secondLevel) GetKeysForCollectionPartitioning(MusicFile mf)
        {
            // western classical music collections are kept as a single "album" by "Various Composers"
            // becuase they have further breakdown into compositions by composer
            return ("Various Composers", mf.OpusName);
        }
    }
}
