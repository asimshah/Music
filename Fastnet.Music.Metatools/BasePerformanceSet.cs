using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Music.Metatools
{
    public abstract class BasePerformanceSet : BaseMusicSet
    {
        public BasePerformanceSet(EntityHelper entityHelper,/*MusicDb db,*/ MusicOptions musicOptions, MusicStyles musicStyle, IEnumerable<MusicFile> musicFiles, TaskItem taskItem) : base(entityHelper, musicOptions, musicStyle, musicFiles, taskItem)
        {
            log.Debug($"Building performance set using {musicFiles.Count()} music files");
            foreach(var mf in musicFiles)
            {
                log.Debug($"{taskItem} --> {mf.File}");
            }
        }

        /// <summary>
        /// removes any performances specified in current set of music files
        /// only zero or one performance is expected in current set of music files
        /// </summary>
        protected void RemoveCurrentPerformance()
        {
            // find the performance containing the current music files, if any
            var tracks = this.MusicFiles.Where(x => x.Track != null).Select(x => x.Track);
            var performances = tracks.Where(t => t.Performance != null).Select(t => t.Performance)
                .Distinct();
            if (performances.Count() > 1)
            {
                var idlist = string.Join(", ", this.MusicFiles.Select(x => x.Id));
                log.Warning($"Music files {idlist} have more than one performance - this is unexpected!");
                foreach (var performance in performances)
                {
                    log.Warning($"  {performance.ToIdent()}");
                }
            }
            //var eh = new EntityHelperOld(MusicDb, taskItem);
            foreach (var performance in performances.ToArray())
            {
                entityHelper.Delete(performance);
            }
        }
        protected Performance GetPerformance(IEnumerable<Performer> performers)
        {
            var performance =  new Performance
            {
                StyleId = this.MusicStyle,
                AlphamericPerformers = string.Join(string.Empty, performers.Select(x => x.Name))
                    .ToAlphaNumerics(),
                Year = year
            };
            performance.PerformancePerformers.AddRange(performers.Select(p => new PerformancePerformer { Performer = p, Performance = performance, Selected = true }));
            var movementNumber = 0;
            foreach (var track in MusicFiles.Select(mf => mf.Track).OrderBy(x => x.Number))
            {
                track.MovementNumber = ++movementNumber;
                performance.Movements.Add(track);
            }
            Debug.Assert(performance.Movements.Count > 0);
            return performance;
        }
        internal Performer GetPerformer(MetaPerformer mp)
        {
            return entityHelper.GetPerformer(mp);
           // return MusicDb.GetPerformer(mp);
        }
        internal IEnumerable<Performer> GetPerformers(IEnumerable<MetaPerformer> list)
        {
            return entityHelper.GetPerformers(list);
            //return MusicDb.GetPerformers(list, taskItem);
        }
    }
}
