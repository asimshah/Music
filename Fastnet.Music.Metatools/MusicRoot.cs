using Fastnet.Music.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    internal static class StringConstants
    {
        internal const string Portraits = "$Portraits";
        internal const string Collections = "Collections";
        internal static string[] MusicFileExtensions = new string[] { ".mp3", ".flac" };
        internal static string[] ImageFileExtensions = new string[] { ".jpg", ".jpeg", ".png" };
    }
    public class MusicRoot
    {
        public static MusicPathAnalysis AnalysePath(MusicOptions musicOptions, string path)
        {
            var mr = ParsePathForMusicRoot(musicOptions, path);
            MusicPathAnalysis mpa = new MusicPathAnalysis { MusicRoot = mr };

            if (path.StartsWith(mr.GetPortraitsPath(), StringComparison.InvariantCultureIgnoreCase))
            {
                mpa.IsPortraitsFolder = true;
            }
            else if (path.StartsWith(mr.GetCollectionsPath(), StringComparison.InvariantCultureIgnoreCase))
            {
                mpa.IsCollection = true;
            }
            if (mpa.IsPortraitsFolder == false)
            {
                var mrPath = mr.GetPath();
                if ((!mr.IsReservedPath(path)) && path.StartsWith(mrPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    var temp = path.Substring(mrPath.Length + 1);
                    var parts = temp.Split(Path.DirectorySeparatorChar);
                    if (!mpa.IsCollection)
                    {
                        mpa.ToplevelName = parts[0];
                        if (parts.Length > 1)
                        {
                            mpa.SecondlevelName = parts[1];
                        }
                    }
                    else
                    {
                        if (parts.Length > 1)
                        {
                            mpa.ToplevelName = parts[1];
                            if (parts.Length > 2)
                            {
                                mpa.SecondlevelName = parts[2];
                            }
                        }
                    }
                }
            }
            return mpa;
        }
        public static MusicRoot ParsePathForMusicRoot(MusicOptions musicOptions, string path)
        {
            var sources = new MusicSources(musicOptions);
            var styles = new MusicStyleCollection(musicOptions);
            var found = false;
            MusicRoot musicRoot = null;
            foreach (var source in sources)
            {
                foreach (var style in styles)
                {
                    foreach (var sp in style.Settings.Select(s => s.Path))
                    {
                        var temp = Path.Combine(source.DiskRoot, sp);
                        if (path.StartsWith(temp, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // path matches source, style and sp
                            musicRoot = new MusicRoot(style.Style, source.DiskRoot, sp, source.IsGenerated);
                            if (style.Filter)
                            {
                                musicRoot.IsFiltered = true;
                                musicRoot.AllowedNames = style.IncludeNames;
                            }
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
                if (found)
                {
                    break;
                }
            }
            return musicRoot;
        }
        public static IEnumerable<MusicRoot> GetMusicRoots(MusicOptions musicOptions, MusicStyles ms, bool includeGenerated = false)
        {
            // for each enabled source, there is a distinct style path
            // for each path in the style settings provided it is found on disk
            // NB: stylepaths are full disk paths
            var stylePaths = GetRootStyleTuples(musicOptions, ms, includeGenerated);
            var list = new List<MusicRoot>();
            foreach (var item in stylePaths)
            {
                list.Add(new MusicRoot(ms, item.diskRoot, item.stylePathFragment, item.isGenerated));
            }
            return list;
        }
        //
        public MusicStyles MusicStyle { get; private set; }
        public string DiskRoot { get; private set; }
        public bool IsGenerated { get; set; }
        public string StylePathFragment { get; private set; }
        public bool IsFiltered { get; private set; }
        public string[] AllowedNames { get; private set; } = new string[0];
        public MusicRoot(MusicStyles ms, string dr, string spf, bool isGenerated = false)
        {
            this.IsGenerated = IsGenerated;
            this.MusicStyle = ms;
            DiskRoot = dr;
            StylePathFragment = spf;
        }
        /// <summary>
        /// returns a union of TopFolders and collection albums
        /// Topfolders are HindiFilmFolders or ArtistFolders according to music style
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ITopFolder> GetAllTopFolders()
        {
            var list = GetTopFoldersExcludingCollections();
            return list.Union(GetCollectionAlbumFolders());
        }
        /// <summary>
        /// returns HindiFilmFolders or ArtistFolders according to music style
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ITopFolder> GetTopFoldersExcludingCollections()
        {
            var collectionsPath = GetCollectionsPath();
            ITopFolder getTF(string name)
            {
                switch (MusicStyle)
                {
                    case MusicStyles.HindiFilms:
                        return new HindiFilmFolder(this, name);

                    default:
                        return new ArtistFolder(this, name);
                }
            }
            var path = GetPath();
            var folders = Directory.EnumerateDirectories(path)
                .Where(f => !IsReservedPath(f));
            var r1 = folders
                .Where(f => !f.StartsWith(collectionsPath, StringComparison.InvariantCultureIgnoreCase))
                .Select(f => Path.GetFileName(f));
            if (IsFiltered)
            {
                r1 = r1.Except(AllowedNames);
            }
            return r1.Select(x => getTF(x));//.ToList();

        }
        /// <summary>
        /// returns AlbumFolders for collections albums
        /// </summary>
        /// <returns></returns>
        public IEnumerable<AlbumFolder> GetCollectionAlbumFolders()
        {
            var collectionsPath = GetCollectionsPath();
            IEnumerable<AlbumFolder> list = Enumerable.Empty<AlbumFolder>();
            if (Directory.Exists(collectionsPath))
            {
                var r2 = Directory.EnumerateDirectories(collectionsPath)
                    .Select(f => Path.GetFileName(f));
                //list.AddRange(r2.Select(x => new AlbumFolder(this, x, true) as ITopFolder));
                list = list.Union(r2.Select(x => AlbumFolder.ForCollection(this, x)));
            }
            return list;
        }
        public string GetPath()
        {
            return Path.Combine(DiskRoot, StylePathFragment);
        }
        public string GetPortraitsPath()
        {
            return Path.Combine(GetPath(), StringConstants.Portraits);
        }
        public string GetCollectionsPath()
        {
            return Path.Combine(GetPath(), StringConstants.Collections);
        }
        //public bool IsReservedName(string name)
        //{
        //    return name.StartsWith("$") || name.IsEqual(_collections);
        //}
        public bool IsReservedPath(string path)
        {
            return path.StartsWith(GetPortraitsPath(), StringComparison.InvariantCultureIgnoreCase);
            //return path.StartsWith(GetPortraitsPath(), StringComparison.InvariantCultureIgnoreCase)
            //    || path.StartsWith(GetCollectionsPath(), StringComparison.InvariantCultureIgnoreCase);
        }
        private static IEnumerable<(string diskRoot, string stylePathFragment, bool isGenerated)> GetRootStyleTuples(MusicOptions musicOptions, MusicStyles musicStyle, bool includeGenerated = false)
        {
            var list = new List<(string diskRoot, string path, bool isGenerated)>();
            var sources = new MusicSources(musicOptions, includeGenerated);
            foreach (var source in sources)
            {
                list.AddRange(musicOptions.Styles.Single(x => x.Style == musicStyle)
                    .Settings.Select(x => (diskRoot: source.DiskRoot, stylePathFragment: x.Path, isGenerated: source.IsGenerated))
                    .Where(x => Directory.Exists(Path.Combine(x.diskRoot, x.stylePathFragment)))
                    );
            }

            return list;
        }
        public override string ToString()
        {
            return $"[{MusicStyle}, {DiskRoot}, {StylePathFragment}{(IsFiltered ? " (F)" : "")}]";
        }
    }
}