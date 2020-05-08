using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fastnet.Music.Metatools
{
    public abstract class BaseMusicSetCollection<ASET, PSET> : BaseMusicSetCollection<ASET> where ASET : BaseAlbumSet where PSET : BasePerformanceSet
    {
        internal BaseMusicSetCollection(MusicOptions musicOptions, MusicDb musicDb, OpusFolder musicFolder, List<MusicFile> files, TaskItem taskItem) : base(musicOptions, musicDb, musicFolder, files, taskItem)
        {
        }
        protected abstract (string firstLevel, string secondLevel) GetPartitioningKeys(MusicFile mf);
        protected abstract PSET CreatePerformanceSet(IEnumerable<MusicFile> files);
        protected override IEnumerable<BaseMusicSet> CreateAlbumSets()
        {
            var albumSets = base.CreateAlbumSets();
            var uSets = CreatePerformanceSets();
            return albumSets.Union(uSets);
        }
        private IEnumerable<BaseMusicSet> CreatePerformanceSets()
        {
            var result = new List<BaseMusicSet>();
            var fileset = PartitionFiles();
            return fileset.Select(fs => CreatePerformanceSet(fs));
        }
        private IEnumerable<IEnumerable<MusicFile>> PartitionFiles()
        {
            var fileSets = new List<IEnumerable<MusicFile>>();
            var artistGroups = files.Select(f => new { file = f, p = GetPartitioningKeys(f) })
                .GroupBy(gb => new { gb.p.firstLevel, gb.p.secondLevel });
            foreach (var group in artistGroups)
            {
                fileSets.Add(group.Select(g => g.file).OrderBy(f => f.GetTagIntValue("TrackNumber")));
            }
            return fileSets;
        }
        //private PSET CreatePerformanceSet(IEnumerable<MusicFile> files)
        //{
        //    var type = typeof(PSET);
        //    switch (type)
        //    {
        //        case Type t when (t == typeof(IndianClassicalRagaSet)):
        //            return new IndianClassicalRagaSet(musicDb, musicOptions, null, files, taskItem) as PSET;
        //        case Type t when (t == typeof(WesternClassicalCompositionSet)):
        //            return new WesternClassicalCompositionSet(musicDb, musicOptions, files, taskItem) as PSET;
        //    };
        //    throw new Exception($"{typeof(PSET)} not supported");
        //}
    }
    public abstract class BaseMusicSetCollection<T> : IEnumerable<BaseMusicSet>  where T: BaseAlbumSet
    {
        protected MusicStyles musicStyle;
        protected readonly ILogger log;
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
        internal BaseMusicSetCollection(MusicOptions musicOptions, MusicDb musicDb, OpusFolder musicFolder, List<MusicFile> files, TaskItem taskItem)
        {
            this.log = ApplicationLoggerFactory.CreateLogger(this.GetType());
            this.musicOptions = musicOptions;
            this.musicDb = musicDb;
            this.musicFolder = musicFolder;
            this.files = files;
            this.taskItem = taskItem;
            var firstFile = files.First();
            musicStyle = firstFile.Style;
        }
        /// <summary>
        /// return one or more album sets - normally one but multiple sets if dealing with a collection
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<BaseMusicSet> CreateAlbumSets()
        {
            var result = new List<BaseMusicSet>();
            var fileset =  musicFolder.IsCollection ?
                PartitionCollection() : new List<IEnumerable<MusicFile>>() { files };
            return fileset.Select(fs => CreateAlbumSet(fs));
        }
        protected abstract (string firstLevel, string secondLevel) GetKeysForCollectionPartitioning(MusicFile mf);
        private IEnumerable<IEnumerable<MusicFile>> PartitionCollection()
        {
            var fileSets = new List<IEnumerable<MusicFile>>();
            var artistGroups = files.Select(f => new { file = f, p = GetKeysForCollectionPartitioning(f) })
                .GroupBy(gb => new { gb.p.firstLevel });
            foreach (var group in artistGroups)
            {
                fileSets.Add(group.Select(g => g.file).OrderBy(f => f.GetTagIntValue("TrackNumber")));
            }
            return fileSets;
        }
        private T CreateAlbumSet(IEnumerable<MusicFile> files) 
        {
            var allPerformers = files.GetAllPerformers(musicOptions);
            var artists = allPerformers.Where(p => p.Type == PerformerType.Artist);
            var otherPerformers = allPerformers.Where(p => p.Type != PerformerType.Artist);
            var type = typeof(T);
            switch (type)
            {
                case Type t when (t == typeof(IndianClassicalAlbumSet)) :
                    return new IndianClassicalAlbumSet(musicDb, musicOptions, files, taskItem) as T;
                case Type t when (t == typeof(WesternClassicalAlbumSet)):
                    return new WesternClassicalAlbumSet(musicDb, musicOptions,  files, taskItem) as T;
                case Type t when (t == typeof(PopularMusicAlbumSet)):
                    return new PopularMusicAlbumSet(musicDb, musicOptions, files, taskItem) as T;

            };
            throw new Exception($"{typeof(T)} not supported");
        }       
        public IEnumerator<BaseMusicSet> GetEnumerator() 
        {
            var result = CreateAlbumSets();
            foreach (var set in result)
            {
                yield return set as BaseMusicSet;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

}
