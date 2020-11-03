using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Core.Web.Controllers;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Fastnet.Apollo.Web
{
    /// <summary>
    /// an extended version of Playlist which includes extended playlist items
    /// (and subitems, if appropriate) plus indiviadual and total durations
    /// Note: subitems are not always required (as in the case of playlist editing)
    /// but need to be loaded in order to compute durations - so they are always included
    /// </summary>
    public class ExtendedPlaylist
    {
        private ILogger log = ApplicationLoggerFactory.CreateLogger<ExtendedPlaylist>();
        private ObservableCollection<ExtendedPlaylistItem> InnerItems { get; set; } = new ObservableCollection<ExtendedPlaylistItem>();
        public string DeviceKey { get; set; }
        public long PlaylistId { get; private set; }
        public PlaylistType Type { get; set; }
        public string Name { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public ReadOnlyObservableCollection<ExtendedPlaylistItem> Items => new ReadOnlyObservableCollection<ExtendedPlaylistItem>(InnerItems);
        public TimeSpan Duration => TimeSpan.FromMilliseconds(this.InnerItems.Sum(x => x.Duration.TotalMilliseconds));

        /// <summary>
        /// adds an ExtendedPlaylistItem to Items.
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(ExtendedPlaylistItem item)
        {
            item.SetPosition(new PlaylistPosition(this.InnerItems.Count() + 1, 0));
            this.InnerItems.Add(item);
        }
        public void AddItem(PlaylistItem item, LibraryService libraryService)
        {
            ExtendedPlaylistItem epli = null;
            switch (item.Type)
            {
                case PlaylistItemType.MusicFile:
                    epli = new PlaylistMusicFileItem(item, libraryService);
                    //AddItem(new PlaylistMusicFileItem(item, libraryService));
                    break;
                case PlaylistItemType.Track:
                    epli = new PlaylistTrackItem(item, libraryService);
                    //AddItem(new PlaylistTrackItem(item, libraryService));
                    break;
                case PlaylistItemType.Work:
                    epli = new PlaylistWorkItem(item, libraryService);
                    //AddItem(new PlaylistWorkItem(item, libraryService));
                    break;
                case PlaylistItemType.Performance:
                    epli = new PlaylistPerformanceItem(item, libraryService);
                    //AddItem(new PlaylistPerformanceItem(item, libraryService));
                    break;
            }
            if (epli.ItemFound)
            {
                AddItem(epli);
            }
            else
            {
                log.Warning($"{item} skipped as entity {item.Type} id {item.ItemId} was not found");
            }
        }
        public void AttachItemsChangedHandler(NotifyCollectionChangedEventHandler handler)
        {
            this.InnerItems.CollectionChanged += handler;
        }
        public void DetachItemsChangedHandler(NotifyCollectionChangedEventHandler handler)
        {
            this.InnerItems.CollectionChanged -= handler;
        }
        public static ExtendedPlaylist Create(Playlist playlist, LibraryService libraryService)
        {
            var ep = new ExtendedPlaylist { PlaylistId = playlist.Id, Name = playlist.Name, Type = playlist.Type, LastModified = playlist.LastModified };
            //var items = new List<ExtendedPlaylistItem>();
            foreach (var item in playlist.Items.OrderBy(x => x.Sequence))
            {
                ep.AddItem(item, libraryService);
            }
            return ep;
        }
        public override string ToString()
        {
            return $"[plist-{PlaylistId}] {Type} {Name} {Items.Count()} items {Duration.ToDuration()}";
        }

        internal void ClearItems()
        {
            this.InnerItems.Clear();
        }
    }
    public abstract class ExtendedPlaylistItem
    {
        private long coverArtId;
        private bool itemFound = true;
        public MusicStyles MusicStyle;
        public long PlaylistItemId { get; set; } // can be zero for PlaylistTrackItem when they are SubItems
        public string ArtistName { get; set; }
        /// <summary>
        /// can be album name, composition name, or raga name
        /// </summary>
        public string CollectionName { get; set; }
        public IEnumerable<string> Titles => GetTitles();// { get; private set; }
        public PlaylistPosition Position { get; private set; }
        public TimeSpan Duration => GetDuration();
        public string CoverArtUrl => $"lib/get/work/coverart/{coverArtId}";

        public bool ItemFound { get => itemFound; set => itemFound = value; }

        public ExtendedPlaylistItem(PlaylistItem pli)
        {
            if (pli != null)
            {
                this.PlaylistItemId = pli.Id;
            }

        }
        protected abstract IEnumerable<string> GetTitles();
        public void SetPosition(PlaylistPosition position)
        {
            this.Position = position;
            AfterPositionSet();
        }
        protected void SetCoverArtId(long id)
        {
            coverArtId = id;
        }
        protected abstract TimeSpan GetDuration();
        protected virtual void AfterPositionSet() { }

    }
    public abstract class SingleTrackPlaylistItem : ExtendedPlaylistItem
    {
        protected MusicFile musicFile;
        public string Title { get; set; }
        public long MusicFileId => musicFile.Id;
        public SingleTrackPlaylistItem(PlaylistItem pli) : base(pli)
        {
        }

        public bool NotPlayableOnCurrentDevice { get; set; }
        public string AudioProperties { get; set; }
        public int SampleRate { get; set; }
        protected void SetMusicFile(MusicFile mf)
        {
            this.musicFile = mf;
            AudioProperties = musicFile.GetAudioProperties();
            SampleRate = musicFile.SampleRate ?? 0;
        }
        protected override IEnumerable<string> GetTitles()
        {
            return new string[] { this.ArtistName, this.CollectionName, this.Title };
        }
        protected void SetNames(Track t)
        {
            this.ArtistName = this.MusicStyle switch
            {
                MusicStyles.WesternClassical => t.Performance.Composition.Artist.Name,
                MusicStyles.IndianClassical => t.Performance.GetNames().artistNames,
                _ => t.Work.GetArtistNames()
            };
            this.CollectionName = this.MusicStyle switch
            {
                MusicStyles.WesternClassical => t.Performance.Composition.Name,
                MusicStyles.IndianClassical => t.Performance.GetRaga().Name,
                _ => t.Work.Name
            };
        }
        protected void SetNames(MusicFile mf)
        {
            SetNames(mf.Track);
        }
        //public override string ToString()
        //{
        //    return new string[] { this.ArtistName, this.CollectionName, this.Title}
        //}
    }
    public abstract class MultiTrackPlaylistItem : ExtendedPlaylistItem
    {
        public IEnumerable<PlaylistTrackItem> SubItems => this.InnerSubItems;
        private List<PlaylistTrackItem> InnerSubItems { get; set; }
        public MultiTrackPlaylistItem(PlaylistItem pli, LibraryService ls) : base(pli)
        {
            this.InnerSubItems = new List<PlaylistTrackItem>();
            //var artistName = string.Empty;
            switch(pli.Type)
            {
                case PlaylistItemType.Performance:

                    var performance = ls.GetEntityAsync<Performance>(pli.ItemId).Result;
                    this.MusicStyle = performance.StyleId;
                    var names = performance.GetNames();
                    this.ArtistName = names.artistNames;// performance.Composition.Artist.Name;
                    this.CollectionName = this.MusicStyle switch
                    {
                        MusicStyles.WesternClassical => performance.Composition.Name,
                        MusicStyles.IndianClassical => performance.GetRaga().Name,
                        _ => throw new Exception($"unexpected style {MusicStyle}")
                    };
                    break;
                case PlaylistItemType.Work:
                    var work = ls.GetEntityAsync<Work>(pli.ItemId).Result;
                    this.MusicStyle = work.StyleId;
                    this.ArtistName = work.GetArtistNames();// work.Artists.Select(x => x.Name).ToCSV();
                    this.CollectionName = this.MusicStyle switch
                    {
                        MusicStyles.Popular => work.Name,
                        _ => throw new Exception($"unexpected style {MusicStyle}")
                    };
                    break;
            }
            //this.ArtistName = artistName;
            //this.CollectionName =  this.MusicStyle switch {
            //    MusicStyles.Popular => 
            //    _ => ""
            //};

            //SetTitles(new string[] { artistName,  pli.Title });
        }
        public void AddSubItem(PlaylistTrackItem subItem)
        {
            subItem.SetPosition( new PlaylistPosition(Position.Major, this.InnerSubItems.Count() + 1));
            InnerSubItems.Add(subItem);
        }
        protected void LoadSubItems(IOrderedEnumerable<Track> tracks)
        {
            this.InnerSubItems.Clear();
            foreach (var track in tracks)
            {
                var plti = new PlaylistTrackItem(track);
                AddSubItem(plti);
            }
        }
        protected override TimeSpan GetDuration()
        {

            return TimeSpan.FromMilliseconds(this.InnerSubItems.Sum(x => x.Duration.TotalMilliseconds));
        }
        protected override IEnumerable<string> GetTitles()
        {
            return new string [] {  this.ArtistName, this.CollectionName};
        }
    }
    public class PlaylistMusicFileItem : SingleTrackPlaylistItem
    {

        public PlaylistMusicFileItem(PlaylistItem pli, LibraryService ls) : base(pli)
        {
            var mf = ls.GetEntityAsync<MusicFile>(pli.MusicFileId).Result;
            SetMusicFile(mf);
            MusicStyle = mf.Style;
            SetNames(mf);
            this.Title = mf.Track.Title;
            //var titles = new string[] {
            //                    musicFile.Track.Performance?.GetParentArtistsName() ?? musicFile.Track.Work.GetArtistNames(),//.Artists.Select(a => a.Name).ToCSV(),
            //                    musicFile.Track.Performance?.GetParentEntityDisplayName() ?? musicFile.Track.Work.Name,
            //                    musicFile.Track.Title
            //                };
            //SetTitles(titles);
            SetCoverArtId(this.musicFile.Track.Work.Id);

        }

        protected override TimeSpan GetDuration()
        {
            return TimeSpan.FromMilliseconds(this.musicFile.Duration ?? 0);
        }
        public override string ToString()
        {
            return $"{Position} music file {Titles.ToCSV()} {AudioProperties} {SampleRate} {Duration.ToDuration()} {CoverArtUrl}";
        }
    }
    public class PlaylistTrackItem : SingleTrackPlaylistItem
    {
        private readonly Track track;
        public PlaylistTrackItem(PlaylistItem pli, LibraryService ls) : this(ls.GetEntityAsync<Track>(pli.ItemId).Result, pli)// : base(pli)
        {
            //this.track = ls.GetEntityAsync<Track>(pli.ItemId).Result;
        }
        public PlaylistTrackItem(Track track, PlaylistItem pli = null) : base(pli)
        {
            if (track != null)
            {
                this.track = track;
                this.MusicStyle = track.Work.StyleId;
                SetNames(this.track);
                this.Title = track.Title;
            }
            else
            {
                ItemFound = false;
            }
        }
        public void SelectMusicFile(DeviceRuntime dr)
        {
            var mf = track.GetBestMusicFile(dr);
            SetMusicFile(mf);
            NotPlayableOnCurrentDevice = mf == null;
        }
        protected override TimeSpan GetDuration()
        {
            return TimeSpan.FromMilliseconds(this.track.GetBestQuality().Duration ?? 0);
        }
        protected override void AfterPositionSet()
        {
            SetCoverArtId(this.track.Work.Id);
        }
        //private void SetTitlesAndCoverArt()
        //{
        //    var title = track.Title;
        //    if (Position.Minor > 0 && title.Contains(':'))
        //    {
        //        title = title.Split(':').Skip(1).First();
        //    }
        //    var titles = new string[]
        //    {
        //        track.Performance?.GetParentArtistsName() ?? track.Work.GetArtistNames(),//.Artists.Select(a => a.Name).ToCSV(),
        //        track.Performance?.GetParentEntityDisplayName() ?? track.Work.Name,
        //        title
        //    };
        //    SetTitles(titles);

        //}

        public override string ToString()
        {
            return $"{Position} track {Titles.ToCSV()}  {AudioProperties} {SampleRate}  {Duration.ToDuration()} {CoverArtUrl}";
        }


    }
    public class PlaylistWorkItem : MultiTrackPlaylistItem
    {
        private readonly Work work;
        public PlaylistWorkItem(PlaylistItem pli, LibraryService ls) : base(pli, ls)
        {
            this.work = ls.GetEntityAsync<Work>(pli.ItemId).Result;
            if (this.work != null)
            {
                var titles = new string[]
                {
                work.GetArtistNames(),
                work.Name
                };
                SetCoverArtId(this.work.Id);
            }
            else
            {
                this.ItemFound = false;
            }
        }
        public override string ToString()
        {
            return $"{Position} work {Titles.ToCSV()} {Duration.ToDuration()}  {SubItems.Count()} tracks, {CoverArtUrl}";
        }
        protected override void AfterPositionSet()
        {
            base.AfterPositionSet();
            LoadSubItems(this.work.Tracks.OrderBy(x => x.Number));
        }
    }
    public class PlaylistPerformanceItem : MultiTrackPlaylistItem
    {
        private readonly Performance performance;
        public PlaylistPerformanceItem(PlaylistItem pli, LibraryService ls) : base(pli, ls)
        {
            this.performance = ls.GetEntityAsync<Performance>(pli.ItemId).Result;
            if (this.performance != null)
            {
                SetCoverArtId(this.performance.Movements.First().Work.Id);
            }
            else
            {
                this.ItemFound = false;
            }
        }
        protected override void AfterPositionSet()
        {
            base.AfterPositionSet();
            LoadSubItems(this.performance.Movements.OrderBy(x => x.MovementNumber));
            foreach(var si in this.SubItems)
            {
                si.Title = CollapseTitle(si);
            }
        }

        private string CollapseTitle(PlaylistTrackItem si)
        {
            string stripPrefix(string text, string separator)
            {
                var parts = text.Split(separator);
                if (parts.Count() > 1 && parts[1].Trim().Length > 0)
                {
                    if (parts[0].IsEqualIgnoreAccentsAndCase(si.CollectionName))
                    {
                        return parts[1].Trim();
                    }
                }

                return text;
            }
            return this.MusicStyle switch
            {
                MusicStyles.WesternClassical => stripPrefix(si.Title, ":"),
                MusicStyles.IndianClassical => stripPrefix(si.Title, ": "),
                _ => throw new Exception($"unexpected music style {this.MusicStyle}")
            };
        }

        public override string ToString()
        {
            return $"{Position} performance {Titles.ToCSV()} {Duration.ToDuration()} {SubItems.Count()} movements, {CoverArtUrl}";
        }
    }
}
