import { Component, OnInit, OnDestroy } from '@angular/core';
import { PlaylistItem, DeviceStatus, AudioDevice, Playlist } from '../shared/common.types';
import { Subscription } from 'rxjs';
import { PlayerService } from '../shared/player.service';
import { PlaylistItemType } from '../shared/common.enums';
import { LoggingService } from '../shared/logging.service';
import { ParameterService } from '../shared/parameter.service';

@Component({
   selector: 'device-playlist',
   templateUrl: './device-playlist.component.html',
   styleUrls: ['./device-playlist.component.scss']
})
export class DevicePlaylistComponent implements OnInit, OnDestroy {
   PlaylistItemType = PlaylistItemType;
   playlist: PlaylistItem[] = []; // ********* change this to use Playlist

   private deviceStatus: DeviceStatus;// | null = null;
   private device: AudioDevice;// | null = null;
   private subscriptions: Subscription[] = [];
   constructor(private playerService: PlayerService,
      private ps: ParameterService,
      private log: LoggingService) {
      this.log.isMobileDevice = this.isMobileDevice();
   }
   async ngOnInit() {
      this.subscriptions.push(this.playerService.deviceStatusUpdate.subscribe(async (ds) => {
         await this.onDeviceStatusUpdate(ds);
      }));
      this.subscriptions.push(this.playerService.selectedDeviceChanged.subscribe(async (d) => {
         this.device = d;
         this.playlist = [];
      }));
      //this.subscriptions.push(this.playerService.currentPlaylistChanged.subscribe((pl) => {
      //   //this.playlist = pl;
      //   //this.log.information(`DevicePlaylistComponent: received play list of ${this.playlist.length} items for [${this.device.key}]`);
      //}));
      this.subscriptions.push(this.playerService.selectedPlaylistChanged.subscribe((pl: Playlist) => {
         this.playlist = pl.items;
      }));
      //await this.playerService.triggerUpdates();
   }
   ngOnDestroy() {
      for (let sub of this.subscriptions) {
         sub.unsubscribe();
      }
      this.subscriptions = [];
   }

   async playItem(item: PlaylistItem) {

      await this.playerService.playItem(item.position);
   }
   async toggleMultipleItems(item: PlaylistItem) {
      item.isOpen = !item.isOpen;
   }
   isMobileDevice() {
      return this.ps.isMobileDevice();
   }
   isPlayable(item: PlaylistItem) {
      return !item.notPlayableOnCurrentDevice;
      //return this.device && (this.device.capability.maxSampleRate === 0 || item.sampleRate <= this.device.capability.maxSampleRate);
   }
   //getTitle(item: PlaylistItem) {
   //   return item.titles[item.titles.length - 1];
   //}
   //getFullTitle(item: PlaylistItem) {
   //   let text = `<div>${item.titles[0]}</div><div>${item.titles[1]}</div><div>${item.titles[2]}</div>`;
   //   return text;
   //}
   isPlayingIconVisible(item: PlaylistItem) {
      let r = false;
      if (this.deviceStatus) {
         if (item.position.minor > 0) {
            if (item.position.major === this.deviceStatus.playlistPosition.major
               && item.position.minor === this.deviceStatus.playlistPosition.minor) {
               r = true;
            }
         }
         else if (item.position.major === this.deviceStatus.playlistPosition.major) {
            r = true;
         }
      }
      return r;
   }
   private async onDeviceStatusUpdate(ds: DeviceStatus) {      
      if (this.device && this.device.key === ds.key) {
         this.deviceStatus = ds;
         //let device = this.playerService.getDevice(ds.key);
         //console.warn(`received status update for ${device.displayName} - not the current device!`);
      } 
   }

}
