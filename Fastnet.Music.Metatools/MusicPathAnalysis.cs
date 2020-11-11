using System.Diagnostics;
using System.IO;

namespace Fastnet.Music.Metatools
{
    public class MusicPathAnalysis
    {
        internal MusicPathAnalysis()
        {
        }
        public MusicRoot MusicRoot { get; set; }
        /// <summary>
        /// true if given path is a portraits folder (or a file in the portraits folder)
        /// </summary>
        public bool IsPortraitsFolder { get; set; }
        /// <summary>
        /// true if the given path is a collections folder (or a folder or file in the collections folder)
        /// </summary>
        public bool IsCollection { get; set; }
        /// <summary>
        /// If this is a collection then it is the album name
        /// or it the film folder name for hindi films
        /// else this is the artist folder name 
        /// NB: null if the given path did not extend to the top level folder
        /// </summary>
        public string ToplevelName { get; set; }
        /// <summary>
        /// does not exist for Popular if IsSingles
        /// does not exist for Hindi films if not a collection
        /// NB: not present if the given path did not extend to the second level
        /// </summary>
        public string SecondlevelName { get; set; }
        /// <summary>
        /// only set by the music folder change monitor
        /// </summary>
        public bool IsDeletion { get; set; }
        /// <summary>
        /// returns the full path to the deepest available level, i.e. second, top, or just the root in that order
        /// </summary>
        /// <returns></returns>
        public string GetPath()
        {
            return GetSecondlevelPath() ?? GetToplevelPath() ?? MusicRoot.GetPath();
        }
        public ITopFolder GetFolder()
        {
            ITopFolder f = null;
            if (IsCollection)
            {
                // always AlbumFolder
                f = AlbumFolder.ForCollection(MusicRoot, ToplevelName);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(SecondlevelName))
                {
                    // albumfolder
                    f = AlbumFolder.ForArtist(MusicRoot, ToplevelName, SecondlevelName);
                }
                else if(!string.IsNullOrWhiteSpace(ToplevelName))
                {
                    // hindi film or artist name
                    if(MusicRoot.MusicStyle == Core.MusicStyles.HindiFilms)
                    {
                        f = new HindiFilmFolder(MusicRoot, ToplevelName);
                    }
                    else
                    {
                        f = AlbumFolder.ForArtistSingles(MusicRoot, ToplevelName);
                        //f = new ArtistFolder(MusicRoot, ToplevelName);
                    }
                }
                else
                {
                    
                }
            }
            return f;
        }
        private string GetSecondlevelPath()
        {
            if (!string.IsNullOrWhiteSpace(SecondlevelName))
            {
                Debug.Assert(IsCollection == false);
                if (IsCollection)
                {
                    return Path.Combine(MusicRoot.GetPath(), StringConstants.Collections, SecondlevelName);
                }
                return Path.Combine(MusicRoot.GetPath(), ToplevelName, SecondlevelName);
            }
            return null;
        }
        private string GetToplevelPath()
        {
            if (!string.IsNullOrWhiteSpace(ToplevelName))
            {
                if (IsCollection)
                {
                    return Path.Combine(MusicRoot.GetPath(), StringConstants.Collections, ToplevelName);
                }
                return Path.Combine(MusicRoot.GetPath(), ToplevelName);
            }
            return null;
        }
        public override string ToString()
        {
            if (IsPortraitsFolder)
            {
                return $"{MusicRoot}, portraits folder{(IsDeletion ? " (deletion)" : "")}";
            }
            if (SecondlevelName == null)
            {
                return $"{MusicRoot}, {ToplevelName ?? ""}{(IsCollection ? "(collection)" : "")}{(IsDeletion ? " (deletion)" : "")} ";
            }
            return $"{MusicRoot}, {ToplevelName ?? ""} {(IsCollection ? "(collection)" : "")} [{SecondlevelName}]{(IsDeletion ? " (deletion)" : "")} ";
        }
    }
}