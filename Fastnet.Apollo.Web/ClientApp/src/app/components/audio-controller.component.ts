import { Component, OnInit,  OnDestroy, ViewChild } from '@angular/core';
import { PlaylistItem, DeviceStatus, AudioDevice,  Playlist } from '../shared/common.types';
import { PlayerStates, PlaylistType, AudioDeviceType } from '../shared/common.enums';
import { PlayerService } from '../shared/player.service';
import { Subscription } from 'rxjs';
import { ParameterService } from '../shared/parameter.service';
import { SliderValue } from '../../fastnet/controls/slider.component';
import { LoggingService } from '../shared/logging.service';
import { WebAudioComponent } from './web-audio.component';

@Component({
   selector: 'audio-controller',
   templateUrl: './audio-controller.component.html',
   styleUrls: [     
      './audio-controller.component.scss'
   ]
})
export class AudioControllerComponent implements OnInit, OnDestroy {
   PlayerStates = PlayerStates;
   AudioDeviceType = AudioDeviceType;
   PlaylistType = PlaylistType;
   @ViewChild(WebAudioComponent, { static: false }) localAudio: WebAudioComponent;
   device: AudioDevice;// | null = null;
   currentItem: PlaylistItem | null = null;
   currentPlaylist: Playlist | null = null;
   deviceStatus: DeviceStatus | null = null;
   volume = 0.0;
   playPosition = 0.0;
   appleAirplayAvailable = false;
   private playlist: PlaylistItem[] = [];

   private subscriptions: Subscription[] = [];
   constructor(private playerService: PlayerService,
      private ps: ParameterService, /*private cdRef: ChangeDetectorRef,*/
      private log: LoggingService) { }

   async ngOnInit() {
      this.subscriptions.push(this.playerService.deviceStatusUpdate.subscribe((ds) => {
         this.onDeviceStatusUpdate(ds);
      }));
      this.subscriptions.push(this.playerService.selectedDeviceChanged.subscribe(async (d) => {
         this.onDeviceChanged(d);
      }));
      this.subscriptions.push(this.playerService.selectedPlaylistChanged.subscribe((pl: Playlist) => {
         this.onPlaylistChanged(pl);
      }));
   }
   ngOnDestroy() {
      for (let sub of this.subscriptions) {
         sub.unsubscribe();
      }
      this.subscriptions = [];
   }
   isLocalAirplayDevice() {
      // **NB** the following assumes that if the device type
      // is a browser thern it is the local broiwser
      // no browser on any other computer is controllable
      // from the current browser
      if (this.device && this.device.type === AudioDeviceType.Browser) {
         if (this.localAudio) {
            return this.localAudio.playbackTargetAvailable();
         }
      }
      return false;
   }
   isMobileDevice() {
      return this.ps.isMobileDevice();
   }
   isPlaying() {
      return this.deviceStatus && this.deviceStatus.state === PlayerStates.Playing;
   }
   canReposition() {
      return this.device && this.device.canReposition;
   }
   async skipBackward() {
      await this.playerService.skipBack();
   }
   async skipForward() {
      console.log(`skipForward()`);
      await this.playerService.skipForward();
   }
   async togglePlayPause() {
      await this.playerService.togglePlayPause();
   }
   async onVolumeChange(sv: SliderValue) {
      if (sv.source === "user-action") {
         await this.playerService.setVolume(sv.value);
      }
   }
   async onPlayPositionChanged(sv: SliderValue) {
      if (sv.source === "user-action") {
         await this.playerService.setPosition(sv.value / 100.0);
      }
   }
   showPlaybackTargetPicker() {
      //video.webkitShowPlaybackTargetPicker()
      (<any>this.localAudio.audio).webkitShowPlaybackTargetPicker();
   }

   private getPercentagePlayed() {
      return this.deviceStatus ? (this.deviceStatus.currentTime / this.deviceStatus.totalTime) * 100.0 : 0.0;
   }

   private onDeviceStatusUpdate(ds: DeviceStatus) {
      if ( this.device && ds && this.device.key === ds.key) {
         this.deviceStatus = ds;
         this.volume = this.deviceStatus.volume;
         this.playPosition = this.getPercentagePlayed();
         this.updateUI();

      }
   }
   private onDeviceChanged(d: AudioDevice) {
      this.device = d;
      this.deviceStatus = null;
      this.playlist = [];
      //if (this.device.playbackTargetAvailable) {
      //   this.log.information(`[audio-controller-component.97] airplay available`);
      //   this.appleAirplayAvailable = true;
      //}
      //else {
      //   this.log.information(`[audio-controller-component.101] airplay not available`);
      //}
      //this.log.information(`AudioControllerComponent: received device change to ${this.device.displayName} [${this.device.key}]`);
      this.updateUI();
   }
   private onPlaylistChanged(pl: Playlist) {
      let device = this.playerService.getDevice(pl.deviceKey);
      let deviceName = device ? device.displayName : "unknown";
      this.currentPlaylist = pl;
      this.playlist = pl.items;
      this.updateUI();
   }
   private updateUI() {
      if (this.deviceStatus && this.playlist.length > 0) {
         let major = this.deviceStatus.playlistPosition.major;
         let minor = this.deviceStatus.playlistPosition.minor;
         if (major !== 0) {
            this.currentItem = this.findCurrentPlaylistItem(major, minor);
         }
      } else {
         this.currentItem = null;
      }

   }
   findCurrentPlaylistItem(major: number, minor: number): PlaylistItem | null {
      let playlistItem = this.currentPlaylist.items.find(x => x.position.major === major)!;
      if (!playlistItem) {
         console.error(`major = ${major}, playlistItem not found, playlist length is ${this.playlist.length}, state = ${PlayerStates[this.deviceStatus!.state]}`);
      }
      else if (minor > 0) {
         playlistItem = playlistItem.subItems.find(x => x.position.minor === minor)!;
         if (!playlistItem) {
            console.error(`major = ${major}, playlistItem not found, playlist length is ${this.playlist.length}, state = ${PlayerStates[this.deviceStatus!.state]}`);
         }
      }
      return playlistItem;
   }

}
