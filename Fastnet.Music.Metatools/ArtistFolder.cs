using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public interface ITopFolder
    {
        /// <summary>
        /// Finds music files in the db that match the path for this folder
        /// The path  for this folder can be overridden using the 'pathToUse' parameter (useful for deleted paths)
        /// </summary>
        /// <param name="eh"></param>
        /// <param name="pathToUse">use this path in place of the normal path</param>
        /// <returns></returns>
        IEnumerable<MusicFile> GetFilesInDb(EntityHelper eh, string pathToUse = null);
    }
    public class ArtistFolder : ITopFolder
    {
        public static IEnumerable<ArtistFolder> GetArtistFolders(Artist artist, MusicOptions musicOptions)
        {
            var allMusicRoots = artist.ArtistStyles.SelectMany(x => MusicRoot.GetMusicRoots(musicOptions, x.StyleId));
            return allMusicRoots
                .Where(mr => ArtistFolder.FindMatchingFolder(mr, artist.Name) != null)
                .Select(mr => new ArtistFolder(mr, artist.Name));
        }
        public string ArtistName => artistName;
        /// <summary>
        /// return the full path for this folder as matched on disk
        /// Note that a match occurs
        /// (1) for a folder that is equal the the artist name regardless of accents
        /// or (2) for a folder that matches with or without a leading 'The ' regardless of accents
        /// </summary>
        public string Fullpath => GetFullPath();
        private readonly string artistName;
        protected readonly MusicOptions musicOptions;
        protected readonly MusicStyles musicStyle;
        protected readonly ILogger log;
        protected readonly MusicRoot musicRoot = null;
        public ArtistFolder(MusicRoot mr, string artistName)
        {
            this.musicRoot = mr;
            this.artistName = artistName;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="style"></param>
        /// <param name="artistName">This name will be matched to disk folders ignoring accents and case</param>
        public ArtistFolder(MusicOptions options, MusicStyles style, string artistName) //: base(options, style)
        {

            this.artistName = artistName;
            musicOptions = options;
            musicStyle = style;
            log = ApplicationLoggerFactory.CreateLogger<ArtistFolder>();
        }
        /// <summary>
        /// Finds music files in the db that match the path for this folder
        /// The path  for this folder can be overridden using the 'pathToUse' parameter (useful for deleted paths)
        /// </summary>
        /// <param name="eh"></param>
        /// <param name="pathToUse">use this path in place of the normal path</param>
        /// <returns></returns>
        public IEnumerable<MusicFile> GetFilesInDb(EntityHelper eh, string pathToUse = null)
        {
            pathToUse = pathToUse ?? Fullpath;
            return eh.FindMatchingFiles(pathToUse);
        }
        public IEnumerable<AlbumFolder> GetAlbumFolders()
        {
            var path = Fullpath;
            var folders = Directory.EnumerateDirectories(path).Select(f => AlbumFolder.ForArtist(musicRoot, ArtistName, Path.GetFileName(f))).ToList();
            if (ContainsSingles())
            {
                folders.Add(AlbumFolder.ForArtistSingles(musicRoot, ArtistName));
            }
            return folders;
        }
        //[Obsolete]
        //public OpusFolderCollection GetOpusFolders(string requiredPrefix = null)
        //{
        //    return new OpusFolderCollection(new MusicFolderInformation
        //    {
        //        //IsCollection = false,
        //        MusicOptions = musicOptions,
        //        MusicStyle = musicStyle,
        //        Paths = MusicMetaDataMethods.GetPathDataList(musicOptions, musicStyle, artistName),
        //        IncludeSingles = musicStyle == MusicStyles.Popular, // causes the collection to include singles
        //        RequiredPrefix = requiredPrefix
        //    });
        //}
        private bool ContainsSingles()
        {
            return Directory.EnumerateFiles(Fullpath, "*.*").Any(f => StringConstants.MusicFileExtensions.Contains(Path.GetExtension(f), StringComparer.CurrentCultureIgnoreCase));
        }
        private static string FindMatchingFolder(MusicRoot mr, string artistName)
        {
            var possiblefolderNames = new string[]
            {
                    artistName,
                    //artist.Name.RemoveDiacritics(),
                    artistName.StartsWith("The ", StringComparison.CurrentCultureIgnoreCase) ?
                        artistName.Substring(4) : $"The {artistName}"
            };
            var possibleFolderPaths = possiblefolderNames.Select(pf => Path.Combine(mr.GetPath(), pf));
            return Directory.EnumerateDirectories(mr.GetPath())
                .Where(p => possibleFolderPaths.Any(x => x.IsEqualIgnoreAccentsAndCase(p))).FirstOrDefault();

        }
        private string GetFullPath()
        {
            return FindMatchingFolder(musicRoot, artistName);
            //return Path.Combine(musicRoot.GetPath(), this.artistName);
        }
        public override string ToString()
        {
            return $"{musicRoot.MusicStyle} ArtistFolder: {ArtistName} in {Fullpath}";
        }


    }


}
