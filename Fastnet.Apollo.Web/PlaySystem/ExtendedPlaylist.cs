using Fastnet.Core;
using Fastnet.Music.Core;
using Fastnet.Music.Data;
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
        //private List<ExtendedPlaylistItem> InnerItems { get; set; } = new List<ExtendedPlaylistItem>();
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
            //item.SetPosition(new PlaylistPosition(this.InnerItems.Count() + 1, 0));
            //this.InnerItems.Add(item);
            switch (item.Type)
            {
                case PlaylistItemType.MusicFile:
                    AddItem(new PlaylistMusicFileItem(item, libraryService));
                    break;
                case PlaylistItemType.Track:
                    AddItem(new PlaylistTrackItem(item, libraryService));
                    break;
                case PlaylistItemType.Work:
                    AddItem(new PlaylistWorkItem(item, libraryService));
                    break;
                case PlaylistItemType.Performance:
                    AddItem(new PlaylistPerformanceItem(item, libraryService));
                    break;
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
                //switch (item.Type)
                //{
                //    case PlaylistItemType.MusicFile:
                //        ep.AddItem(new PlaylistMusicFileItem(item, libraryService));
                //        break;
                //    case PlaylistItemType.Track:
                //        ep.AddItem(new PlaylistTrackItem(item, libraryService));
                //        break;
                //    case PlaylistItemType.Work:
                //        ep.AddItem(new PlaylistWorkItem(item, libraryService));
                //        break;
                //    case PlaylistItemType.Performance:
                //        ep.AddItem(new PlaylistPerformanceItem(item, libraryService));
                //        break;
                //}
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
        public long PlaylistItemId { get; set; } // can be zero for PlaylistTrackItem when they are SubItems
        /// <summary>
        /// Titles is an array because it can include multiple lines depending on the type of ExtendedPlaylistItem:
        /// (1) for a track or a music file there are 3 entries: artist, work/performance name, track/movement title
        /// (2) for a work or a performance at position (x, 0) there is one entry which is the work/performance name
        /// </summary>
        public IEnumerable<string> Titles { get; private set; }
        public PlaylistPosition Position { get; private set; }
        public TimeSpan Duration => GetDuration();
        public string CoverArtUrl => $"lib/get/work/coverart/{coverArtId}";
        public ExtendedPlaylistItem(PlaylistItem pli)
        {
            //this.libraryService = ls;
            this.PlaylistItemId = pli.Id;
            //this.Title = pli.Title;
            //this.ItemId = pli.ItemId;
        }
        public ExtendedPlaylistItem()
        {

        }
        public void SetPosition(PlaylistPosition position)
        {
            this.Position = position;
            AfterPositionSet();
        }
        protected void SetTitles(IEnumerable<string> titles)
        {
            this.Titles = titles;
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
        public long MusicFileId => musicFile.Id;
        public SingleTrackPlaylistItem()
        {
        }

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
    }
    public abstract class MultiTrackPlaylistItem : ExtendedPlaylistItem
    {
        public IEnumerable<PlaylistTrackItem> SubItems => this.InnerSubItems;
        private List<PlaylistTrackItem> InnerSubItems { get; set; }
        public MultiTrackPlaylistItem(PlaylistItem pli, LibraryService ls) : base(pli)
        {
            this.InnerSubItems = new List<PlaylistTrackItem>();
            var artistName = string.Empty;
            switch(pli.Type)
            {
                case PlaylistItemType.Performance:
                    var performance = ls.GetEntityAsync<Performance>(pli.ItemId).Result;
                    var names = performance.GetNames();
                    artistName = names.artistNames;// performance.Composition.Artist.Name;
                    break;
                case PlaylistItemType.Work:
                    var work = ls.GetEntityAsync<Work>(pli.ItemId).Result;
                    artistName = work.GetArtistNames();// work.Artists.Select(x => x.Name).ToCSV();
                    break;
            }
            SetTitles(new string[] { artistName,  pli.Title });
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
    }
    public class PlaylistMusicFileItem : SingleTrackPlaylistItem
    {

        public PlaylistMusicFileItem(PlaylistItem pli, LibraryService ls) : base(pli)
        {
            var mf = ls.GetEntityAsync<MusicFile>(pli.MusicFileId).Result;
            SetMusicFile(mf);
            var titles = new string[] {
                                musicFile.Track.Performance?.GetParentArtistsName() ?? musicFile.Track.Work.GetArtistNames(),//.Artists.Select(a => a.Name).ToCSV(),
                                musicFile.Track.Performance?.GetParentEntityDisplayName() ?? musicFile.Track.Work.Name,
                                musicFile.Track.Title
                            };
            SetTitles(titles);
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
        public PlaylistTrackItem(PlaylistItem pli, LibraryService ls) : base(pli)
        {
            this.track = ls.GetEntityAsync<Track>(pli.ItemId).Result;
        }
        public PlaylistTrackItem(Track track) //: base(track.Title)
        {
            this.track = track;
            
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
            SetTitlesAndCoverArt();
        }
        private void SetTitlesAndCoverArt()
        {
            var title = track.Title;
            if (Position.Minor > 0 && title.Contains(':'))
            {
                title = title.Split(':').Skip(1).First();
            }
            var titles = new string[]
            {
                track.Performance?.GetParentArtistsName() ?? track.Work.GetArtistNames(),//.Artists.Select(a => a.Name).ToCSV(),
                track.Performance?.GetParentEntityDisplayName() ?? track.Work.Name,
                title
            };
            SetTitles(titles);
            SetCoverArtId(this.track.Work.Id);
        }

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
            var titles = new string[]
            {
                work.GetArtistNames(),//work.Artists.Select(a => a.Name).ToCSV(),
                work.Name
            };
            SetTitles(titles);
            SetCoverArtId(this.work.Id);
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

            SetCoverArtId(this.performance.Movements.First().Work.Id);
        }
        protected override void AfterPositionSet()
        {
            base.AfterPositionSet();
            LoadSubItems(this.performance.Movements.OrderBy(x => x.MovementNumber));
        }
        public override string ToString()
        {
            return $"{Position} performance {Titles.ToCSV()} {Duration.ToDuration()} {SubItems.Count()} movements, {CoverArtUrl}";
        }
    }
}
