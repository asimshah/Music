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
    public abstract class MusicSetWithPerformances : MusicSet
    {
        public MusicSetWithPerformances(MusicDb db, MusicOptions musicOptions, MusicStyles musicStyle, IEnumerable<MusicFile> musicFiles, TaskItem taskItem) : base(db, musicOptions, musicStyle, musicFiles, taskItem)
        {
        }

        /// <summary>
        /// removes any performances specified in current set of music files
        /// only zero or one performance is expected in current set of music files
        /// </summary>
        protected void RemoveCurrentPerformance()
        {
            // find the performance containing the current music files, if any
            var tracks = this.MusicFiles.Where(x => x.Track != null).Select(x => x.Track);
            var performances = tracks.Where(t => t.Performance != null).Select(t => t.Performance);
            if (performances.Count() > 1)
            {
                var idlist = string.Join(", ", this.MusicFiles.Select(x => x.Id));
                log.Warning($"Music files {idlist} have more than one performance - this is unexpected!");
                foreach (var performance in performances)
                {
                    log.Warning($"  {performance.ToIdent()}");
                }
            }
            foreach (var performance in performances.ToArray())
            {
                var performers = performance.GetAllPerformersCSV();
                MusicDb.PerformancePerformers.RemoveRange(performance.PerformancePerformers);
                MusicDb.Performances.Remove(performance);
                log.Information($"{performance.ToIdent()} removed");
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
            return MusicDb.GetPerformer(mp);
        }
        internal IEnumerable<Performer> GetPerformers(IEnumerable<MetaPerformer> list)
        {
            return MusicDb.GetPerformers(list);
        }
        internal async Task<Artist> GetArtistAsync(MetaPerformer ap)
        {
            Artist artist = FindArtist(ap.Name);
            if (artist == null)
            {
                // look for a performer of the same name
                var performer = await MusicDb.Performers.SingleOrDefaultAsync(x => x.AlphamericName == ap.Name.ToAlphaNumerics());
                if (performer != null)
                {
                    artist = await PromotePerformerToArtist(performer);
                }
                else
                {
                    artist = await CreateNewArtist(ap.Name);
                }
            }
            return artist;
        }

        private async Task<Artist> PromotePerformerToArtist(Performer performer)
        {
            //** NB* for the present this is done for IndianClassical music only
            // There may be a Performer created previously who is found to be an artist now.
            // We create an artist with this performer's name but we also remove the old performer.
            // This requires going back to the performances for this performer and changing them to add
            // this performer as an artist remembering to change the performance.AlphamericPerformers accordingly

            var artist = await CreateNewArtist(performer.Name);
            if (MusicStyle == MusicStyles.IndianClassical)
            {
                log.Information($"promoting {performer} to an artist");
                var pplist = performer.PerformancePerformers.AsEnumerable();//.Select(x => x.Performance);
                foreach(var item in pplist)
                {
                    var performance = item.Performance;
                    var list = MusicDb.RagaPerformances.Where(x => x.Performance == performance).AsEnumerable();
                    foreach(var raga in list.Select(x => x.Raga))
                    {
                        var rp = new RagaPerformance { Artist = artist, Performance = performance, Raga = raga };
                        MusicDb.RagaPerformances.Add(rp);
                        log.Information($"new ragaperformance entry created for {artist.ToIdent()}, {performance.ToIdent()}, {raga.ToIdent()}");
                    }
                    performance.PerformancePerformers.Remove(item);
                    MusicDb.PerformancePerformers.Remove(item);
                    log.Information($"{item.ToIdent()} removed");
                    var remainingPerformers = performance.PerformancePerformers.Select(x => x.Performer);
                    var old = performance.AlphamericPerformers;
                    performance.AlphamericPerformers = string.Join(string.Empty, remainingPerformers.Select(x => x.Name))
                        .ToAlphaNumerics();
                    log.Information($"{performance.ToIdent()} AlphamericPerformers chnaged from {old} to {performance.AlphamericPerformers}");
                }
            }
            return artist;
        }
    }
}
