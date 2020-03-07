using Fastnet.Music.Data;
using System;
using System.Collections.Generic;

namespace Fastnet.Music.Metatools
{
    public class CatalogueResult
    {
        public IMusicSet MusicSet { get; set; }
        public Type MusicSetType /*{ get; set; }*/ => MusicSet.GetType();
        public CatalogueStatus Status { get; set; }
        public Artist Artist { get; set; }
        public Work Work { get; set; }
        public IEnumerable<Track> Tracks { get; set; }
        public Composition Composition { get; set; }
        public Performance Performance { get; set; }
        /// <summary>
        /// Possibly non null for PopularMusicAlbumSet or WesternClassicalAlbumSet
        /// (when resampling task is required)
        /// </summary>
        public TaskItem TaskItem { get; set; }
    }
}
