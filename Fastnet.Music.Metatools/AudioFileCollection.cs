using Fastnet.Music.Core;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    internal class AudioFileCollection : IEnumerable<AudioFile>
    {
        // private bool sourceFoldersAreForArtists;
        //private bool isSinglesFolder;
        private readonly WorkFolder workFolder;
        //private readonly IEnumerable<PathData> pathDataList;
        internal AudioFileCollection(WorkFolder folder) //: base(folder.musicOptions, folder.musicStyle)
        {
            this.workFolder = folder;
        }
        public IEnumerator<AudioFile> GetEnumerator()
        {
            var list = workFolder.GetFilesOnDisk();
            foreach (var (fi, part) in list.OrderBy(f => f.part?.Number ?? 0))
            {
                AudioFile af = null;
                switch (fi.Extension.ToLower())
                {
                    case ".mp3":
                        af = new Mp3File(fi);
                        break;
                    case ".flac":
                        af = new FlacFile(fi);
                        break;
                }
                af.Part = part;
                //if (sourceFoldersAreForArtists)
                //{
                //    af.SetAsSingle();
                //    //af.IsSingle = true;
                //}
                yield return af;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
