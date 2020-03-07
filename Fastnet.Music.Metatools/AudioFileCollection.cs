using Fastnet.Music.Core;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public class AudioFileCollection : /*BaseMusicMetaDataOld, */IEnumerable<AudioFile>
    {
        private bool sourceFoldersAreForArtists;
        private bool isSinglesFolder;
        private readonly OpusFolder opusFolder;
        private readonly IEnumerable<PathData> pathDataList;
        internal AudioFileCollection(OpusFolder folder) //: base(folder.musicOptions, folder.musicStyle)
        {
            this.opusFolder = folder;
        }
        public IEnumerator<AudioFile> GetEnumerator()
        {
            var list = opusFolder.GetFilesOnDisk();
            foreach (var (fi, part) in list.OrderBy(f => f.part?.Number ?? 0))
            {
                AudioFile af = null;
                switch (fi.Extension.ToLower())
                {
                    case ".mp3":
                        af = new Mp3File(this.opusFolder.MusicOptions, this.opusFolder.MusicStyle, fi);
                        break;
                    case ".flac":
                        af = new FlacFile(this.opusFolder.MusicOptions, this.opusFolder.MusicStyle, fi);
                        break;
                }
                af.Part = part;
                if (sourceFoldersAreForArtists)
                {
                    af.SetAsSingle();
                    //af.IsSingle = true;
                }
                yield return af;
            }
        }
        //private List<(FileInfo fi, OpusPart part)> GetMusicFiles()
        //{
        //    var list = new List<(FileInfo fi, OpusPart part)>();
        //    switch (opusFolder)
        //    {
        //        case OpusFolderOld folder when (folder.HasParts()):
        //            foreach (var path in pathDataList.Select(x => sourceFoldersAreForArtists ? x.GetFullArtistPath() : x.GetFullOpusPath()))
        //            {
        //                foreach (var part in folder.Parts)
        //                {
        //                    var combinedPath = Path.Combine(path, part.Name);
        //                    list.AddRange(this.musicOptions.GetMusicFiles(combinedPath).Select<FileInfo, (FileInfo, OpusPart)>(f => (f, part)));
        //                }
        //            }
        //            break;
        //        default:
        //            foreach (var path in pathDataList.Select(x => (sourceFoldersAreForArtists || isSinglesFolder) ? x.GetFullArtistPath() : x.GetFullOpusPath()))
        //            {
        //                list.AddRange(this.musicOptions.GetMusicFiles(path).Select<FileInfo, (FileInfo, OpusPart)>(f => (f, null)));
        //            }
        //            break;
        //    }
        //    return list;
        //}
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
