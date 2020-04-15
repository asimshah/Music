using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public abstract class MusicSetCollection<MT> : IEnumerable<IMusicSet> where MT : MusicTags
    {
        protected abstract List<IMusicSet> CreateSets();
        protected ILogger log { get; set; }
        protected MusicStyles musicStyle;
        private readonly bool isCollection;
        protected readonly List<MusicFile> files;
        protected readonly MusicOptions musicOptions;
        protected readonly MusicDb musicDb;
        protected readonly OpusFolder musicFolder;
        protected readonly TaskItem taskItem;
        /// <summary>
        /// files is a set of music files fom the same opus (ie. originalyl from the same disk folder)
        /// </summary>
        /// <param name="musicOptions"></param>
        /// <param name="musicDb"></param>
        /// <param name="musicFolder"></param>
        /// <param name="files"></param>
        /// <param name="taskItem"></param>
        internal MusicSetCollection(MusicOptions musicOptions, MusicDb musicDb, OpusFolder musicFolder, List<MusicFile> files, TaskItem taskItem)
        {
            this.musicOptions = musicOptions;
            this.musicDb = musicDb;
            this.musicFolder = musicFolder;
            this.files = files;
            this.taskItem = taskItem;
            //Debug.Assert(ValidateMusicFileSet());
            var firstFile = files.First();
            isCollection = firstFile.OpusType == OpusType.Collection;
            musicStyle = firstFile.Style;
        }
        public IEnumerator<IMusicSet> GetEnumerator()
        {
            var result = CreateSets();
            foreach (var set in result)
            {
                yield return set;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

}
