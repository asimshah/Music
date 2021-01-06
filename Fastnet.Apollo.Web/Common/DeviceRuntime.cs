using Fastnet.Core;
using Fastnet.Core.Logging;
using Fastnet.Music.Core;
using Fastnet.Music.Messages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace Fastnet.Apollo.Web
{
    /// <summary>
    /// run time device info - i.e. not dependent on MusicDb instance
    /// </summary>
    public class DeviceRuntime
    {
        #region Private Fields

        private static ILogger log = ApplicationLoggerFactory.CreateLogger<DeviceRuntime>();

        #endregion Private Fields

        #region Public Properties

        public int CommandSequenceNumber { get; set; }
        public PlaylistPosition CurrentPosition { get; set; }
        public string DisplayName { get; set; }
        public ExtendedPlaylist ExtendedPlaylist { get; private set; }
        public string Key { get; set; }
        public int MaxSampleRate { get; set; }
        public PlayerCommand MostRecentCommand { get; set; }
        public string PlayerUrl { get; set; }
        public DeviceStatus Status { get; set; }
        public AudioDeviceType Type { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public DeviceRuntime()
        {
            CurrentPosition = new PlaylistPosition();
        }

        #endregion Public Constructors

        #region Public Methods

        public void ClearPlaylist()
        {
            ExtendedPlaylist.ClearItems();
            CurrentPosition = PlaylistPosition.ZeroPosition;
        }
        public SingleTrackPlaylistItem GetItemAtPosition(PlaylistPosition position)
        {
            var item = FindPlaylistItem(position);
            if (item is MultiTrackPlaylistItem)
            {
                item = FindPlaylistItem(position.GetNextMinor());
            }
            var newPosition = item?.Position ?? PlaylistPosition.ZeroPosition;
            log.Information($"current position changed from {CurrentPosition} to {newPosition}, next item is {item.ToString() ?? ""}");
            CurrentPosition = newPosition;
            var stpli = item as SingleTrackPlaylistItem;
            if (stpli.NotPlayableOnCurrentDevice)
            {
                log.Information($"{stpli} is not playable on {DisplayName} [{Key}]");
                return GetNextItem();
            }
            return stpli;
        }
        public SingleTrackPlaylistItem GetNextItem()
        {
            ExtendedPlaylistItem result = null;
            if (CurrentPosition.IsZero())
            {
                result = ExtendedPlaylist.Items.FirstOrDefault();
            }
            else
            {
                var currentMajorItem = FindPlaylistItem(CurrentPosition.Major, 0);
                switch (currentMajorItem)
                {
                    case SingleTrackPlaylistItem sti:
                        // current major is single track, so next will be next major
                        result = FindPlaylistItem(CurrentPosition.GetNextMajor());
                        break;
                    case MultiTrackPlaylistItem mti:
                        // current major is multi track, so next will be either the next minor or the next major
                        result = FindPlaylistItem(CurrentPosition.GetNextMinor());
                        if (result == null)
                        {
                            result = FindPlaylistItem(CurrentPosition.GetNextMajor());
                        }
                        break;
                }
            }
            if (result != null)
            {
                if (result is MultiTrackPlaylistItem)
                {
                    result = FindFirstSubItem(result.Position.Major);
                }
            }
            var newPosition = result?.Position ?? PlaylistPosition.ZeroPosition;
            log.Debug($"current position changed from {CurrentPosition} to {newPosition}, next item is {result?.ToString() ?? ""}");
            CurrentPosition = newPosition;
            if (CurrentPosition.Major == 0 && CurrentPosition.Major == 0)
            {
                return null;
            }
            var stpli = result as SingleTrackPlaylistItem;
            if (stpli.NotPlayableOnCurrentDevice)
            {
                log.Information($"{stpli} is not playable on {DisplayName} [{Key}]");
                return GetNextItem();
            }
            return stpli;
        }
        public SingleTrackPlaylistItem GetPreviousItem()
        {
            ExtendedPlaylistItem result = null;
            if (CurrentPosition.IsZero())
            {
                result = ExtendedPlaylist.Items.Last();
            }
            else
            {
                var currentMajorItem = FindPlaylistItem(CurrentPosition.Major, 0);
                switch (currentMajorItem)
                {
                    case SingleTrackPlaylistItem sti:
                        // current major is single track, so next will be next major
                        result = FindPlaylistItem(CurrentPosition.GetPreviousMajor());
                        break;
                    case MultiTrackPlaylistItem mti:
                        // current major is multi track, so next will be either the next minor or the next major
                        var previousMinorPosition = CurrentPosition.GetPreviousMinor();
                        if (previousMinorPosition.Minor > 0)
                        {
                            result = FindPlaylistItem(previousMinorPosition);

                        }
                        if (result == null)
                        {
                            result = FindPlaylistItem(CurrentPosition.GetPreviousMajor());
                        }
                        break;
                }
            }
            if (result != null)
            {
                if (result is MultiTrackPlaylistItem)
                {
                    result = FindLastSubItem(result.Position.Major);
                }
            }
            var newPosition = result?.Position ?? PlaylistPosition.ZeroPosition;
            //log.Information($"current position changed from {CurrentPosition} to {newPosition}, next item is {result?.ToString() ?? ""}");
            CurrentPosition = newPosition;
            var stpli = result as SingleTrackPlaylistItem;
            if (stpli.NotPlayableOnCurrentDevice)
            {
                log.Information($"{stpli} is not playable on {DisplayName} [{Key}]");
                return GetPreviousItem();
            }
            return stpli;
        }
        public void SetPlaylist(ExtendedPlaylist epl)
        {
            var resettingExistingPlaylist = false;
            var savedPosition = PlaylistPosition.ZeroPosition;
            long? currentPlaylistItemId = null;
            if (this.ExtendedPlaylist != null)
            {
                resettingExistingPlaylist = this.ExtendedPlaylist.PlaylistId == epl.PlaylistId;
                if(resettingExistingPlaylist)
                {
                    if(this.CurrentPosition.IsZero() == false)
                    {
                        savedPosition = this.CurrentPosition.Clone();
                        var currentItem = this.ExtendedPlaylist.Items.Skip(savedPosition.Major - 1).First();
                        currentPlaylistItemId = currentItem.PlaylistItemId;
                    }
                }
                DetachPlaylist();
            }
            epl.DeviceKey = this.Key;
            foreach(var item in epl.Items)
            {
                AddRuntimeInformation(item);
            }
            this.ExtendedPlaylist = epl;
            if(resettingExistingPlaylist && currentPlaylistItemId.HasValue)
            {
                var matchingItem = this.ExtendedPlaylist.Items.SingleOrDefault((x) => x.PlaylistItemId == currentPlaylistItemId.Value);
                if (matchingItem != null)
                {
                    var index = this.ExtendedPlaylist.Items.IndexOf(matchingItem);
                    savedPosition.SetMajor(index + 1);
                }
                else
                {
                    savedPosition = PlaylistPosition.ZeroPosition;
                }
            }
            if (resettingExistingPlaylist)
            {
                log.Debug($"{this}, setting current position from {CurrentPosition} to {savedPosition}");
            }
            this.CurrentPosition = savedPosition.Clone();// PlaylistPosition.ZeroPosition;
            this.ExtendedPlaylist.AttachItemsChangedHandler(PlaylistItems_CollectionChanged);
        }
        public override string ToString()
        {
            return $"{DisplayName} (key:{Key}) [{Type}]";
        }

        #endregion Public Methods

        #region Private Methods

        private void AddRuntimeInformation(ExtendedPlaylistItem extendedPlaylistItem)
        {
            switch (extendedPlaylistItem)
            {
                case PlaylistMusicFileItem mfi:
                    mfi.NotPlayableOnCurrentDevice = !(MaxSampleRate == 0 || mfi.SampleRate == 0 || mfi.SampleRate <= MaxSampleRate);
                    break;
                case PlaylistTrackItem ti:
                    ti.SelectMusicFile(this);
                    break;
                case MultiTrackPlaylistItem mti:
                    foreach (var si in mti.SubItems)
                    {
                        AddRuntimeInformation(si);
                    }
                    break;
            }
        }
        private void DetachPlaylist()
        {
            if (this.ExtendedPlaylist != null)
            {
                this.ExtendedPlaylist.DeviceKey = null;
                this.ExtendedPlaylist.DetachItemsChangedHandler(PlaylistItems_CollectionChanged);
            }
        }
        private ExtendedPlaylistItem FindFirstSubItem(int major)
        {
            return FindPlaylistItem(major, 1);
        }
        private ExtendedPlaylistItem FindLastSubItem(int major)
        {
            var result = ExtendedPlaylist.Items.SingleOrDefault(x => x.Position.Major == major && x.Position.Minor == 0);
            switch (result)
            {
                case MultiTrackPlaylistItem mti:
                    result = mti.SubItems.Last();
                    break;
            }
            return result;
        }
        private ExtendedPlaylistItem FindPlaylistItem(PlaylistPosition position)
        {
            return FindPlaylistItem(position.Major, position.Minor);

        }
        private ExtendedPlaylistItem FindPlaylistItem(int major, int minor)
        {
            var result = ExtendedPlaylist.Items.SingleOrDefault(x => x.Position.Major == major && x.Position.Minor == 0);
            switch (result)
            {
                case MultiTrackPlaylistItem mti:
                    if (minor != 0)
                    {
                        result = mti.SubItems.SingleOrDefault(x => x.Position.Minor == minor);
                    }
                    break;
            }
            return result;
        }
        private void OnPlaylistItemAdded(ExtendedPlaylistItem extendedPlaylistItem)
        {
            AddRuntimeInformation(extendedPlaylistItem);
        }
        private void PlaylistItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            log.Debug($"Device [{Key}] {DisplayName} items collection received action : {e.Action}");
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach(var item in e.NewItems)
                    {
                        OnPlaylistItemAdded(item as ExtendedPlaylistItem);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    //break;
                case NotifyCollectionChangedAction.Replace:
                    //break;
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    //Debug.WriteLine($"Device [{Key}] {DisplayName} items collection received action : {e.Action}");
                    break;
            }
        }

        #endregion Private Methods
    }
}
