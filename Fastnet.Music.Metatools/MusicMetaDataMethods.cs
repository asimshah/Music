using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    //public static partial class Extensions
    //{

    //}

    public static class MusicMetaDataMethods
    {
        public static IEnumerable<string> GetPortraitPaths(MusicOptions musicOptions, MusicStyles style)
        {
            var pathList = new List<string>();
            var styleInfo = GetStyleInfo(musicOptions, style);
            if (styleInfo != null)
            {
                foreach (var source in new MusicSources(musicOptions))
                {
                    if (!source.IsGenerated)
                    {
                        foreach (var path in styleInfo.Settings.Select(s => s.Path))
                        {
                            var portraitPath = Path.Combine(source.DiskRoot, path, "$Portraits");
                            if (Directory.Exists(portraitPath))
                            {
                                pathList.Add(portraitPath);
                            }
                        }
                    }
                }
            }
            return pathList;
        }
        /// <summary>
        /// Gets Pathdata instance for the given disk path. The path is parsed to establish source, style, artist and, if present, opus
        /// and the result is checked for existence otherwise returns null
        /// 
        /// </summary>
        /// <param name="musicOptions"></param>
        /// <param name="diskPath"></param>
        /// <returns></returns>
        public static PathData GetPathData(MusicOptions musicOptions, string diskPath, bool allowDeleted = false)
        {
            PathData pd = null;
            var sources = new MusicSources(musicOptions);
            var matches = sources.Where(s => diskPath.StartsWith(s.DiskRoot, StringComparison.CurrentCultureIgnoreCase));
            MusicSource source = null;
            switch (matches.Count())
            {
                case int count when count == 1:
                    source = matches.First();
                    break;
                case int count when count > 1:
                    source = matches.OrderByDescending(x => x.DiskRoot.Length).First();
                    break;
                default:
                    break;
            }
            if (source != null)
            {
                var relativeDiskPath = Path.GetRelativePath(source.DiskRoot, diskPath);
                var styleInfo = musicOptions.Styles.Where(s => s.Enabled).SingleOrDefault(s => s.Settings.Select(t => t.Path).Any(p => relativeDiskPath.StartsWith(p, StringComparison.CurrentCultureIgnoreCase)));
                if (styleInfo != null)
                {
                    var ss = styleInfo.Settings.Single(s => relativeDiskPath.StartsWith(s.Path, StringComparison.CurrentCultureIgnoreCase));
                    var remainingParts = Path.GetRelativePath(ss.Path, relativeDiskPath).Split(Path.DirectorySeparatorChar);
                    if (remainingParts.Length == 1 && remainingParts[0] == ".")
                    {
                        pd = new PathData
                        {
                            DiskRoot = source.DiskRoot,
                            MusicStyle = styleInfo.Style,
                            StylePath = ss.Path,
                            IsGenerated = source.IsGenerated
                        };
                    }
                    else
                    {
                        var artistName = remainingParts.First();
                        if (!artistName.StartsWith("$"))
                        {
                            if (!styleInfo.Filter || styleInfo.IncludeArtists.Any(x => x.IsEqualIgnoreAccentsAndCase(artistName)))
                            {
                                switch (remainingParts.Length)
                                {
                                    case int l when l == 1:
                                        var t1 = Path.Combine(source.DiskRoot, ss.Path, artistName);
                                        if (allowDeleted == true || (Directory.Exists(t1) || Directory.Exists(t1.RemoveDiacritics())))
                                        {
                                            pd = new PathData
                                            {
                                                DiskRoot = source.DiskRoot,
                                                MusicStyle = styleInfo.Style,
                                                StylePath = ss.Path,
                                                ArtistPath = artistName,
                                                IsGenerated = source.IsGenerated,
                                                IsCollections = string.Compare(artistName, "collections", true) == 0
                                            };
                                        }
                                        break;
                                    case int l when l > 1:
                                        var t2 = Path.Combine(source.DiskRoot, ss.Path, artistName, remainingParts.Skip(1).First());
                                        if (allowDeleted == true || (Directory.Exists(t2) || Directory.Exists(t2.RemoveDiacritics())))
                                        {
                                            pd = new PathData
                                            {
                                                DiskRoot = source.DiskRoot,
                                                MusicStyle = styleInfo.Style,
                                                StylePath = ss.Path,
                                                ArtistPath = artistName,
                                                OpusPath = remainingParts.Skip(1).First(),
                                                IsGenerated = source.IsGenerated,
                                                IsCollections = string.Compare(artistName, "collections", true) == 0
                                            };
                                        }
                                        break;
                                }
                            }
                        }
                        else
                        {
                            // for the moment only $portraits
                            pd = new PathData
                            {
                                DiskRoot = source.DiskRoot,
                                MusicStyle = styleInfo.Style,
                                StylePath = ss.Path,
                                IsGenerated = source.IsGenerated,
                                IsPortraits = true
                            };
                        }
                    }
                }
            }
            return pd;
        }
        /// <summary>
        /// Get a list of PathData instances using music style and artist name
        /// returns an item for each disk source where the style matches and a folder for the artist exists
        /// </summary>
        /// <param name="musicOptions"></param>
        /// <param name="musicStyle"></param>
        /// <param name="artistName"></param>
        /// <returns></returns>
        public static PathData[] GetPathDataList(MusicOptions musicOptions, MusicStyles musicStyle, string artistName)
        {
            var list = new List<PathData>();
            var style = GetStyleInfo(musicOptions, musicStyle);
            if (style != null)
            {
                //foreach (var rootFolder in new MusicSources(musicOptions).OrderBy(s => s.IsGenerated).ThenBy(s => s.DiskRoot))
                foreach (var rootFolder in new MusicSources(musicOptions))
                {
                    foreach (var setting in style.Settings)
                    {
                        if (!style.Filter || style.IncludeArtists.Any(x => x.IsEqualIgnoreAccentsAndCase(artistName)))
                        {
                            if (PathExists(rootFolder, setting, artistName))
                            {
                                var pd = new PathData
                                {
                                    DiskRoot = rootFolder.DiskRoot,
                                    MusicStyle = musicStyle,
                                    StylePath = setting.Path,
                                    ArtistPath = artistName,
                                    IsGenerated = rootFolder.IsGenerated,
                                    IsCollections = string.Compare(artistName, "collections", true) == 0
                                };
                                list.Add(pd);
                            }
                        }
                    }
                }
            }
            return list.ToArray();
        }
        public static StyleInformation GetStyleInfo(MusicOptions musicOptions, MusicStyles musicStyle)
        {
            //StyleInfo si;
            return musicOptions.Styles.SingleOrDefault(s => s.Style == musicStyle && s.Enabled);
        }
        private static bool PathExists(MusicSource rootFolder, StyleSetting setting, string name)
        {
            var path = Path.Combine(rootFolder.DiskRoot, setting.Path);
            return (Directory.Exists(path) && Directory.EnumerateDirectories(path).SingleOrDefault(d => Path.GetFileName(d).IsEqualIgnoreAccentsAndCase(name)) != null);
        }

    }
    public class MusicFolderInformation
    {
        public MusicOptions MusicOptions { get; set; }
        public MusicStyles MusicStyle { get; set; }
        public PathData[] Paths { get; set; }
        public bool IncludeSingles { get; set; }
        /// <summary>
        /// if not null then used as prefix filter for folders
        /// </summary>
        public string RequiredPrefix { get; set; }
    }


}
