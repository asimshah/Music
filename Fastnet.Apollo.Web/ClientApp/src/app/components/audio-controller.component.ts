import { Component, OnInit, Input, ViewChild, ElementRef, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { PlaylistItem, DeviceStatus, AudioDevice, Parameters, Playlist } from '../shared/common.types';
import { PlayerStates, PlaylistType } from '../shared/common.enums';
import { PlayerService } from '../shared/player.service';
import { Subscription } from 'rxjs';
import { LoggingService } from '../shared/logging.service';
import { ParameterService } from '../shared/parameter.service';
//import { PopupDialogComponent, PopupCloseHandler } from '../../fastnet/controls/popup-dialog.component';
//import { DialogResult, EnumValue, ListItem } from '../../fastnet/core/core.types';
//import { ValidationMethod } from '../../fastnet/controls/controlbase.type';
//import { Dictionary } from '../../fastnet/core/dictionary.types';
//import { ValidationContext, ValidationResult } from '../../fastnet/controls/controls.types';

//enum PlaylistSaveType {
//   New,
//   Replace
//}
//class SelectableAudioDevice {
//   selected: boolean = false;
//   constructor( public device: AudioDevice) { }
//   public get displayName() {
//      return this.device.displayName;
//   }

//}
//class SavePlaylist {
//   ready: boolean = false;
//   saveType: PlaylistSaveType = PlaylistSaveType.New;
//   public newName: string = "";
//   public existingName: string;
//   public existingNames: string[];
//   public saveTypeNames: string[] = ["New Playlist", "Replace Existing Playlist"];
//   public validators: Dictionary<ValidationMethod>;
//   private canSaveNewName = false;
//   constructor(private savePlaylistDialog: PopupDialogComponent,
//      private playerService: PlayerService,
//      //private currentDevice: AudioDevice,
//      //private cdRef: ChangeDetectorRef,
//      private log: LoggingService) { }
//   async open(onClose: (dr: DialogResult, saveType?: PlaylistSaveType, name?: string) => void) {
//      this.existingNames = await this.playerService.getAllPlaylists();
//      this.validators = new Dictionary<ValidationMethod>();
//      this.validators.add("newplaylist", (vc, v) => {
//         return this.newPlaylistValidatorAsync(vc, v);
//      });
//      this.savePlaylistDialog.open((r: DialogResult) => {
//         if (r === DialogResult.ok) {
//            onClose(r, this.saveType, this.saveType === PlaylistSaveType.New ? this.newName : this.existingName);
//         } else {
//            onClose(r);
//         }
//      });
//      this.ready = true;
//   }
//   onSave() {
//      this.savePlaylistDialog.close(DialogResult.ok);
//   }
//   onCancel() {
//      this.savePlaylistDialog.close(DialogResult.cancel);
//   }
//   async onNewNameTextChanged() {
//      let r = false;
//      if (this.newName && this.newName.length > 0) {
//         r = await this.savePlaylistDialog.validateAll();
//      }
//      this.canSaveNewName = r;
//   }
//   isSaveEnabled() {
//      switch (this.saveType) {
//         case PlaylistSaveType.New:
//            return this.canSaveNewName;
//         case PlaylistSaveType.Replace:
//            return this.existingName && this.existingName.length > 0;
//            break;
//      }
//   }
//   private newPlaylistValidatorAsync(cs: ValidationContext, val: string) {
//      return new Promise<ValidationResult>(resolve => {
//         let vr = new ValidationResult();
//         let text = (val || "").trim();
//         if (text.length > 0) {
//            let r = this.existingNames.find((v, i) => v.toLowerCase() === text.toLowerCase());
//            if (r) {
//               vr.valid = false;
//               vr.message = `a playlist with this name already exists`;
//            }
//         }
//         resolve(vr);
//      });
//   }
//}
//class PlaylistTransfer {
//   ready: boolean = false;
//   devices: SelectableAudioDevice[] = [];
//   constructor(private transferPlaylistDialog: PopupDialogComponent, private currentDevice: AudioDevice, private playerService: PlayerService,
//      private ps: ParameterService,
//      private log: LoggingService) {

//   }
//   async open(onDeviceSelected: (d: AudioDevice) => void) {
//      this.GetDevices();
//      this.transferPlaylistDialog.open((r: DialogResult) => {
//         if (r === DialogResult.ok) {
//            let selectedDevice = this.findSelectedDevice();
//            onDeviceSelected(selectedDevice.device);
//         }
//      });
//      this.ready = true;
//   }

//   onOK() {
//      this.transferPlaylistDialog.close(DialogResult.ok);
//   }
//   onCancel() {
//      this.transferPlaylistDialog.close(DialogResult.cancel);
//   }
//   onDeviceSelected(sd: SelectableAudioDevice) {
//      for (let x of this.devices) {
//         if (x === sd) {
//            x.selected = true;
//         } else {
//            x.selected = false;
//         }
//      }
//   }
//   isOKEnabled() {
//      if (this.ready && this.devices.length > 0) {
//         let result = this.findSelectedDevice();
//         return result !== null;
//      }
//      return false;
//   }
//   private findSelectedDevice(): SelectableAudioDevice | null {
//      for (let x of this.devices) {
//         if (x.selected === true) {
//            return x;
//         }
//      }
//      return null;
//   }
//   private async GetDevices() {
//      let devices = await this.playerService.getAvailableDevices();
//      this.devices = [];
//      for (let d of devices) {
//         if (d.key !== this.currentDevice.key) {
//            let sd = new SelectableAudioDevice(d);
//            this.devices.push(sd);
//         }
//      }
//   }
//}
@Component({
   selector: 'audio-controller',
   templateUrl: './audio-controller.component.html',
   styleUrls: ['./audio-controller.component.scss']
})
export class AudioControllerComponent implements OnInit, OnDestroy {
   PlayerStates = PlayerStates;
   //PlaylistSaveType = PlaylistSaveType;
   PlaylistType = PlaylistType;
   @ViewChild('playSlider', { static: false }) playSliderRef: ElementRef;
   @ViewChild('volumeSlider', { static: false }) volumeSliderRef: ElementRef;
   @ViewChild('playBead', { static: false }) playBeadRef: ElementRef;
   @ViewChild('volumeBead', { static: false }) volumeBeadRef: ElementRef;
   //@ViewChild('transferPlaylist', { static: false }) transferPlaylistDialog: PopupDialogComponent;
   //@ViewChild('savePlaylist', { static: false }) savePlaylistDialog: PopupDialogComponent;
   //
   //transferModel: PlaylistTransfer | null = null;
   //saveModel: SavePlaylist | null = null;
   //
   currentItem: PlaylistItem | null = null;
   currentPlaylist: Playlist | null = null;
   deviceStatus: DeviceStatus | null = null;
   volumeBeadPosition: string = "0px";
   playBeadPosition: string = "0px";
   private playlist: PlaylistItem[] = [];
   private device: AudioDevice;// | null = null;
   private subscriptions: Subscription[] = [];
   constructor(private playerService: PlayerService,
      private ps: ParameterService, private cdRef: ChangeDetectorRef,
      private log: LoggingService) { }

   async ngOnInit() {
      this.subscriptions.push(this.playerService.deviceStatusUpdate.subscribe((ds) => {
         this.onDeviceStatusUpdate(ds);
      }));
      this.subscriptions.push(this.playerService.selectedDeviceChanged.subscribe(async (d) => {
         this.onDeviceChanged(d);
      }));
      //this.subscriptions.push(this.playerService.currentPlaylistChanged.subscribe((pl) => {
      //   //this.playlist = pl;
      //   //this.updateUI();
      //}));
      this.subscriptions.push(this.playerService.selectedPlaylistChanged.subscribe((pl: Playlist) => {
         this.onPlaylistChanged(pl);
      }));
      //await this.playerService.triggerUpdates();
   }
   ngOnDestroy() {
      //this.stopUpdate();
      for (let sub of this.subscriptions) {
         sub.unsubscribe();
      }
      this.subscriptions = [];
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
      //console.log(`togglePlayPause()`);
      await this.playerService.togglePlayPause();
   }
   async volumeUp() {
      let level = this.getVolumeLevel();
      level = level > 0.9 ? 1.0 : Math.min(level + 0.1, 1.0);
      await this.setVolume(level);

   }
   async volumeDown() {
      let level = this.getVolumeLevel();
      level = level < 0.1 ? 0.0 : Math.max(level - 0.1, 0.0);
      await this.setVolume(level);
   }
   async onVolumeSliderMouseUp(ev: MouseEvent) {
      let volumeTrack: HTMLElement = this.volumeSliderRef.nativeElement;
      let trackLength = volumeTrack.offsetWidth;
      let offset = ev.clientX - volumeTrack.offsetLeft;
      //console.log(`click at ${offset} =  ${(offset / trackLength) * 100.0}`);
      await this.setVolume((offset / trackLength));
   }
   async onPlayingSliderMouseUp(ev: MouseEvent) {
      let playTrack: HTMLElement = this.playSliderRef.nativeElement;
      let trackLength = playTrack.offsetWidth;
      let offset = (ev.clientX - playTrack.offsetLeft) / trackLength;
      let isForward = (offset * 100.0) > this.getPercentagePlayed();
      //console.log(`click at ${offset}, forward = ${isForward}`);
      await this.playerService.setPosition(offset);
   }
   //async onSavePlaylist() {
   //   //this.log.trace("[AudioControllerComponent] onSavePlaylist()");
   //   this.saveModel = new SavePlaylist(this.savePlaylistDialog, this.playerService, this.log);
   //   await this.saveModel.open(async (dr, type, name) => {
   //      this.saveModel = null;
   //      if (dr === DialogResult.ok) {
   //         console.log(`${PlaylistSaveType[type]} using name ${name}`);
   //         switch (type) {
   //            case PlaylistSaveType.New:
   //               await this.playerService.saveNewPlaylist(this.device, name);
   //               break;
   //            case PlaylistSaveType.Replace:
   //               await this.playerService.replacePlaylist(this.device, name);
   //               break;
   //         }
   //      }
   //   });
   //}
   //async onTransferPlaylist() {
   //   // i.e from another device
   //   this.log.trace("[AudioControllerComponent] onTransferPlaylist()");
   //   this.transferModel = new PlaylistTransfer(this.transferPlaylistDialog, this.device, this.playerService, this.ps, this.log);
   //   await this.transferModel.open(async (selectedDevice) => {
   //      //console.log(`device ${selectedDevice.displayName} selected`);
   //      await this.playerService.copyPlaylist(selectedDevice.key, this.device.key);
   //      this.transferModel = null;
   //   });
   //}
   //onLoadSavedPlayList() {
   //   this.log.trace("[AudioControllerComponent] onLoadSavedPlayList()");
   //}
   //getPlaylistName() {
   //   if (this.currentPlaylist) {
   //      if (this.currentPlaylist.playlistType !== PlaylistType.DeviceList) {
   //         return this.currentPlaylist.playlistName;
   //      }
   //   }
   //   return ("(auto)");
   //}
   //getPlaylistDuration() {
   //   if (this.currentPlaylist && this.currentPlaylist.items.length > 1) {
   //      return this.currentPlaylist.formattedTotalTime;
   //   }
   //   return ("");
   //}
   private async setVolume(level: number) {
      await this.playerService.setVolume(level);
   }
   private getPercentagePlayed() {
      return this.deviceStatus ? (this.deviceStatus.currentTime / this.deviceStatus.totalTime) * 100.0 : 0.0;
   }
   private getVolumeLevel() {
      return this.deviceStatus ? this.deviceStatus.volume * 100.0 : 0.0;
   }
   private onDeviceStatusUpdate(ds: DeviceStatus) {
      if (this.device && ds && this.device.key === ds.key) {
         this.deviceStatus = ds;
         this.updateUI();
      }
   }
   private onDeviceChanged(d: AudioDevice) {
      this.device = d;
      this.deviceStatus = null;
      this.playlist = [];
      //this.log.information(`AudioControllerComponent: received device change to ${this.device.displayName} [${this.device.key}]`);
      this.updateUI();
   }
   private onPlaylistChanged(pl: Playlist) {
      let device = this.playerService.getDevice(pl.deviceKey);
      let deviceName = device ? device.displayName : "unknown";
      //console.log(`recd: ${pl.toString()} for ${deviceName}`);
      this.currentPlaylist = pl;
      this.playlist = pl.items;
      this.updateUI();
   }
   private updateUI() {
      this.positionPlaybead();
      this.positionVolumebead();
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
   private positionPlaybead() {
      if (this.playSliderRef) {
         let playTrack: HTMLElement = this.playSliderRef.nativeElement;
         let trackLength = playTrack.offsetWidth;
         let beadLeft = (trackLength * this.getPercentagePlayed()) / 100.0;
         let bead: HTMLElement = this.playBeadRef.nativeElement;
         this.playBeadPosition = `${beadLeft - (bead.offsetWidth / 2)}px`;
      }
   }
   private positionVolumebead() {
      if (this.volumeSliderRef) {
         let volumeTrack: HTMLElement = this.volumeSliderRef.nativeElement;
         let trackLength = volumeTrack.offsetWidth;
         let beadLeft = (trackLength * this.getVolumeLevel()) / 100.0;
         let bead: HTMLElement = this.volumeBeadRef.nativeElement;
         this.volumeBeadPosition = `${beadLeft - (bead.offsetWidth / 2)}px`;
      }
   }
}
