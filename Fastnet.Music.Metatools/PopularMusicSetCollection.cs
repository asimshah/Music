using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    /// <summary>
    /// Popular music files are grouped in one of 3 ways: (1) if the MusicFolder is an ArtistFolder then music files in this folder are
    /// grouped into a 'singles' album; (2) if it is a Collection fodler then music files are grouped by artist into 'singles' folders;
    /// and (3) if it is an opus folder then all music files in the folder are collected into an album
    /// </summary>
    public class PopularMusicSetCollection : BaseMusicSetCollection<PopularMusicAlbumSet>
    {
        public PopularMusicSetCollection(MusicOptions musicOptions, MusicDb musicDb,
            OpusFolder musicFolder, List<MusicFile> files, TaskItem taskItem) : base(musicOptions, musicDb, musicFolder, files, taskItem)
        {

        }
        protected override (string firstLevel, string secondLevel) GetKeysForCollectionPartitioning(MusicFile mf)
        {
            //Popular music collections are broken into separate "Singles" albums per artist
            var artist = mf.GetAllPerformers(musicOptions)
                .Where(x => x.Type == PerformerType.Artist).Select(x => x.Name.ToAlphaNumerics()).ToCSV();
            var work = $"{artist} Singles"; // worry about this??? could this be string.empty???
            return (artist, work);
        }
    }
}
