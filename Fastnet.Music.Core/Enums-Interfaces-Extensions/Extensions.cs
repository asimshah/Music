using Fastnet.Core;
using Fastnet.Core.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fastnet.Music.Core
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class Extensions
    {
        private static readonly ILogger log = ApplicationLoggerFactory.CreateLogger("Fastnet.Music.Core.Extensions");
        /// <summary>
        /// return a list of valid style paths that exists, i.e. where the style is enabled and for each source that is enabled
        /// (compare with MusicSet version which does test for enabled in either case - should this replace that?)
        /// </summary>
        /// <param name="musicStyle"></param>
        /// <param name="musicOptions"></param>
        /// <param name="includeGenerated">default is false</param>
        /// <param name="includeDisabledStyles">default is false</param>
        /// <returns></returns>
        public static IEnumerable<string> GetPaths(this MusicStyles musicStyle, MusicOptions musicOptions,
            bool includeGenerated, bool includeDisabledStyles)
        {
            var list = new List<string>();
            if (musicOptions.Styles.Single(x => x.Style == musicStyle).Enabled || includeDisabledStyles == true)
            {
                foreach (var source in musicOptions.Sources.Where(x => x.Enabled && (includeGenerated == true || !x.IsGenerated)))
                {
                    list.AddRange(musicOptions.Styles.Single(x => x.Style == musicStyle)
                        .Settings.Select(x => Path.Combine(source.DiskRoot, x.Path))
                        .Where(x => Directory.Exists(x))
                        );
                }
            }
            return list;
        }
        /// <summary>
        /// Finds all opus folders of this name for this artist across all sources
        /// for artists of type "various", artistName is replaced with "Collections"
        /// </summary>
        /// <param name="musicStyle"></param>
        /// <param name="musicOptions"></param>
        /// <param name="type"></param>
        /// <param name="artistName"></param>
        /// <param name="opusPath"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetOpusFolders(this MusicStyles musicStyle, MusicOptions musicOptions, ArtistType type, string artistName, string opusPath)
        {
            var list = new List<string>();
            void AddToList(string opusPath, string artistFolder)
            {
                if (opusPath == null)
                {
                    list.Add(artistFolder);
                }
                else
                {
                    var path2 = Path.Combine(artistFolder, opusPath);
                    var workFolder = Directory.EnumerateDirectories(artistFolder).SingleOrDefault(ap => ap.IsEqualIgnoreAccentsAndCase(path2));
                    if (workFolder != null)
                    {
                        list.Add(workFolder);
                    }
                }
            }
            string getArtistFolder(string stylePath, string name)
            {
                var path1 = Path.Combine(stylePath, type == ArtistType.Various ? "Collections" : name);
                return Directory.EnumerateDirectories(stylePath).SingleOrDefault(ap => ap.IsEqualIgnoreAccentsAndCase(path1));
            }
            foreach (var stylePath in musicStyle.GetPaths(musicOptions, false, false))
            {
                //var path1 = Path.Combine(stylePath, type == ArtistType.Various ? "Collections" : artistName);
                //var artistFolder = Directory.EnumerateDirectories(stylePath).SingleOrDefault(ap => ap.IsEqualIgnoreAccentsAndCase(path1));
                var artistFolder = getArtistFolder(stylePath, artistName);
                if (artistFolder != null)
                {
                    AddToList(opusPath, artistFolder);
                }
                else if (type != ArtistType.Various)
                {
                    if (artistName.StartsWith("The ", StringComparison.CurrentCultureIgnoreCase))
                    {
                        artistFolder = getArtistFolder(stylePath, artistName.Substring(4));
                    }
                    else
                    {
                        artistFolder = getArtistFolder(stylePath, $"The {artistName}");
                    }
                    if (artistFolder != null)
                    {
                        AddToList(opusPath, artistFolder);
                    }
                }
            }
            return list;
        }
        public static IEnumerable<FileInfo> GetMusicFiles(this MusicOptions mo, string srcPath, bool deep = false)
        {
            var search = mo.MusicFileExtensions;// new string[] { ".mp3", ".flac", ".m4a" };
            List<FileInfo> list = new List<FileInfo>();
            foreach (var ext in search)
            {
                var files = Directory.EnumerateFiles(srcPath, "*" + ext, deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                        .AsParallel()
                        .Select(x => new FileInfo(x));
                list.AddRange(files);
            }
            return list.OrderBy(fi => fi.Name);
        }
        public static string ReplaceAlias(this MusicOptions mo, string name)
        {
            var result = name;
            foreach (var list in mo.Aliases)
            {
                var targetName = list.First();

                if (name.IsEqualIgnoreAccentsAndCase(targetName) || list.Any(x => x.IsEqualIgnoreAccentsAndCase(name)))
                {
                    name = targetName;// list.First();
                    break;
                }
            }
            return name;
        }
        // rename this to ToDurationFormat()
        public static string ToDuration(this TimeSpan ts)
        {
            if (ts.Hours > 0)
            {
                return ts.ToString(@"h\:mm\:ss");
            }
            else
            {
                return ts.ToString(@"mm\:ss");
            }
        }
        public static string FormatDuration(this double d)
        {
            return TimeSpan.FromMilliseconds(d).ToDuration();
        }
        public static string FormatDuration(this double? d)
        {
            if (d.HasValue)
            {
                return FormatDuration(d.Value);
            }
            return "";
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
