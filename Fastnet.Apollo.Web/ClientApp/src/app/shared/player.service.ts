import { Injectable, OnDestroy } from "@angular/core";
import { BaseService } from "./base-service.service";
import { HttpClient } from "@angular/common/http";
import { AudioDevice, PlaylistItem, DeviceStatus, PlaylistPosition, Playlist } from "./common.types";
import { Track, MusicFile, Work, Performance } from "../shared/catalog.types";

import { LoggingService } from "./logging.service";
import { Subscription, Subject } from "rxjs";
import { MessageService } from "./message.service";
import { PlayerStates, LocalStorageKeys, AudioDeviceType } from "./common.enums";
import { removeLocalStorageValue, setLocalStorageValue, getLocalStorageValue } from "./common.functions";
import { ParameterService } from "./parameter.service";
import { promise } from "protractor";

export function playerServiceFactory(ps: PlayerService) {
   return () => ps.init();
}

@Injectable()
export class PlayerService extends BaseService implements OnDestroy {
   //currentPlaylistChanged = new Subject<PlaylistItem[]>();
   deviceStatusUpdate = new Subject<DeviceStatus>();
   selectedDeviceChanged = new Subject<AudioDevice>();
   selectedPlaylistChanged = new Subject<Playlist>();
   playerServiceStarted = new Subject<boolean>();

   private messageSubscriptions: Subscription[] = [];
   private updateTimer;
   private timerStarted = false;
   private awaitedSequenceNumber = 0;
   private storageKeyForCurrentDevice: string; // i.e. as stored in local storage
   private devices: AudioDevice[] = [];
   private selectedDevice: AudioDevice | null = null;
   private selectedDevicePlaylist: Playlist | null = null;
   private selectedDeviceStatus: DeviceStatus | null = null;
   private volumeUpdateSuspended = false;
   constructor(http: HttpClient, private messageService: MessageService,
      private parameterService: ParameterService,
      private log: LoggingService) {
      super(http, "player");
      this.parameterService.ready$.subscribe(async (v) => {
         if (v === true) {
            this.init();
         }
      });
   }
   async init() {
      //console.log("PlayerService: init()");
      this.storageKeyForCurrentDevice = `music:${LocalStorageKeys[LocalStorageKeys.currentDevice]}`;// this.parameterService.getStorageKey(LocalStorageKeys.currentDevice);
      await this.start();
      //this.log.information("[PlayerService] started");
      this.playerServiceStarted.next(true);
   }
   ngOnDestroy() {
      this.stopUpdate();
      for (let sub of this.messageSubscriptions) {
         sub.unsubscribe();
      }
      this.messageSubscriptions = [];
   }
   public hasSelectedDevice() {
      return this.selectedDevice !== null;
   }
   public getDevice(key: string) {
      return this.devices.find(x => x.key === key);
   }
   public getSelectedDevice() {
      return this.selectedDevice;
   }
   public getAvailableDevices() {
      return this.devices;
   }
   public async getDevices(all: boolean = false): Promise<AudioDevice[]> {
      let list = await this.getAsync<AudioDevice[]>(`get/devices/${all}`);
      return new Promise<AudioDevice[]>(resolve => {
         let devices: AudioDevice[] = [];
         for (let item of list) {
            let device = new AudioDevice();
            device.copyProperties(item);
            devices.push(device);
         }
         resolve(devices);
      });
   }
   public async updateDevice(d: AudioDevice) {
      return this.postAsync("update/device", d);
   }
   //public getCurrentPlaylist() {
   //   return this.currentPlaylist;
   //}
   public async getCurrentDeviceStatus() {
      if (this.selectedDevice) {
         return await this.getAsync<DeviceStatus>(`get/device/${this.selectedDevice.key}/status`);
      }
      throw "no device available";
   }
   public async playFile(mf: MusicFile) {
      if (this.selectedDevice) {
         return await this.getAsync<number>(`play/file/${this.selectedDevice.key}/${mf.id}`);
      }
   }
   public async playTrack(track: Track) {
      if (this.selectedDevice) {
         return await this.getAsync<number>(`play/track/${this.selectedDevice.key}/${track.id}`);
      }
   }
   public async playWork(work: Work) {
      if (this.selectedDevice) {
         return await this.getAsync<number>(`play/work/${this.selectedDevice.key}/${work.id}`);
      }
   }
   public async playPerformance(performance: Performance) {
      if (this.selectedDevice) {
         return await this.getAsync<number>(`play/performance/${this.selectedDevice.key}/${performance.id}`);
      }
   }
   public async playItem(position: PlaylistPosition) {
      if (this.selectedDevice) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`play/item/${this.selectedDevice.key}/${position.major}/${position.minor}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   public async queueFile(mf: MusicFile) {
      if (this.selectedDevice) {
         return await this.getAsync<number>(`queue/file/${this.selectedDevice.key}/${mf.id}`);
      }
   }
   public async queueTrack(track: Track) {
      if (this.selectedDevice) {
         return await this.getAsync<number>(`queue/track/${this.selectedDevice.key}/${track.id}`);
      }
   }
   public async queueWork(work: Work) {
      if (this.selectedDevice) {
         return await this.getAsync<number>(`queue/work/${this.selectedDevice.key}/${work.id}`);
      }
   }
   public async queuePerformance(performance: Performance) {
      if (this.selectedDevice) {
         return await this.getAsync<number>(`queue/performance/${this.selectedDevice.key}/${performance.id}`);
      }
   }
   public async togglePlayPause() {
      if (this.selectedDevice) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`togglePlayPause/${this.selectedDevice.key}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   public async skipForward() {
      if (this.selectedDevice) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`skip/forward/${this.selectedDevice.key}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   public async skipBack() {
      if (this.selectedDevice) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`skip/back/${this.selectedDevice.key}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   public async setVolume(level: number) {
      if (this.selectedDevice) {
         this.volumeUpdateSuspended = true;
         if (this.selectedDeviceStatus) {
            this.selectedDeviceStatus.volume = level;
         }
         this.awaitedSequenceNumber = await this.getAsync<number>(`set/volume/${this.selectedDevice.key}/${level}`);
         setTimeout(() => { this.volumeUpdateSuspended = false;}, 2000);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   public async setPosition(position: number) {
      if (this.selectedDevice) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`reposition/${this.selectedDevice.key}/${position}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   async setDevice(d: AudioDevice | null) {
      this.selectedDevice = d;
      this.selectedDeviceStatus = null;
      this.selectedDevicePlaylist = null;
      this.saveDeviceKeyToStorage(this.selectedDevice);
      this.selectedDeviceChanged.next(this.selectedDevice);
      // now we ask for an initial playlist and then a devicestatus
      await this.requestSelectedPlaylist();
      await this.requestSelectedDeviceStatus();
   }
   //
   public async getAllPlaylistNames() {
      return this.getAsync<string[]>(`get/all/playlist/names`);
   }
   public async getAllPlaylists() {
      return new Promise<Playlist[]>(async (resolve) => { 
         let list = await this.getAsync<Playlist[]>(`get/all/playlists`);
         let result: Playlist[] = [];
         for (let item of list) {
            let pl = new Playlist();
            pl.copyProperties(item);
            result.push(pl);
         }
         resolve(result);
      });
   }
   //
   public async setPlaylist(devicekey: string, playlistId: number) {
      await this.getAsync<void>(`select/playlist/${devicekey}/${playlistId}`);
   }
   public async copyPlaylist(from: string, to: string) {
      await this.getAsync<void>(`copy/playlist/${from}/${to}`);
   }
   public async saveNewPlaylist(device: AudioDevice, name: string) {
      return this.getAsync<void>(`savenew/playlist/${device.key}/${name}`);
   }
   public async replacePlaylist(device: AudioDevice, name: string) {
      return this.getAsync<void>(`replace/playlist/${device.key}/${name}`);
   }
   public async updatePlaylist(pl: Playlist) {
      await this.postAsync<Playlist, void>(`update/playlist`, pl);
   }
   public async deletePlaylist(playlistId: number) {
      await this.getAsync(`delete/playlist/${playlistId}`);
   }
   //
   public async enableWebAudio() {
      console.log(`enableWebAudio(): webaudio/start/${this.parameterService.getBrowserKey()}`);
      let d = await this.getAsync<AudioDevice>(`webaudio/start/${this.parameterService.getBrowserKey()}`);
      let device = new AudioDevice();
      device.copyProperties(d);
      await this.messageService.connectWebAudio();
      return device;
   }
   public async playNextPlaylistItem(key: string) {
      await this.getAsync(`play/next/${key}`);
   }
   public async sendWebAudioDeviceStatus(ds: DeviceStatus) {
      return this.postAsync("webaudio/device/status", ds);
   }

   //
   private async start() {
      //console.log(`player service start(), timerStarted = ${this.timerStarted}`);
      if (this.timerStarted === false) {
         this.messageSubscriptions.push(this.messageService.deviceEnabled.subscribe(async (d) => {
            await this.onDeviceEnabled(d);
         }));
         this.messageSubscriptions.push(this.messageService.deviceDisabled.subscribe(async (key) => {
            await this.onDeviceDisabled(key);
         }));
         this.messageSubscriptions.push(this.messageService.deviceNameChanged.subscribe(async (d) => {
            await this.onDeviceNameChanged(d);
         }));
         this.messageSubscriptions.push(this.messageService.deviceStatusUpdate.subscribe((ds) => {
            this.onDeviceStatusUpdate(ds);
         }));
         this.messageSubscriptions.push(this.messageService.playlistUpdate.subscribe((update) => {
            //this.onPlaylistUpdate(pu);
            let pl = new Playlist();
            pl.copyProperties(update);
            this.onPlaylist(pl);
         }));
         this.startUpdate();
      }
   }
   //+++++ message subscriptions
   private async onDeviceEnabled(d: AudioDevice) {
      let localAudioKey = this.parameterService.getParameters().browserKey;
      if (d.type === AudioDeviceType.Browser && d.key != localAudioKey) {
         // we do not add devices from other browsers ..
         return;
      }

      let index = this.devices.findIndex(x => x.key === d.key);
      if (index === -1) {
         this.devices.push(d);
      }
      if (!this.selectedDevice) {
         await this.setInitialDevice();
      }
   }
   private async onDeviceDisabled(key: string) {
      //this.log.information(`device ${d.displayName} removed`);
      let index = this.devices.findIndex(x => x.key === key);
      if (index > -1) {
         let device = this.devices[index];
         this.devices.splice(index, 1);
         if (!this.selectedDevice || this.selectedDevice.key == device.key) {
            await this.setInitialDevice();
         }
      } else {
         //if (d.type !== AudioDeviceType.Browser) {
         //   this.log.error(`disabled device ${key} not found in local list`);
         //}
      }
   }
   private async onDeviceNameChanged(d: AudioDevice) {
      //this.log.information(`device ${d.displayName} removed`);
      let index = this.devices.findIndex(x => x.key === d.key);
      if (index > -1) {
         let device = this.devices[index];
         device.displayName = d.displayName;
      } else {
         if (d.type !== AudioDeviceType.Browser) {
            this.log.error(`device ${d.displayName} not found in local list`);
         }
      }
   }
   private onDeviceStatusUpdate(ds: DeviceStatus) {
      let device = this.getDevice(ds.key);
      if (device && ds.key === device.key) {
         let cs = this.selectedDeviceStatus;
         if (this.volumeUpdateSuspended) {
            ds.volume = this.selectedDeviceStatus ? this.selectedDeviceStatus.volume : 0;
         }
         this.selectedDeviceStatus = ds;
      } else {
         //this.selectedDeviceStatus = null;
      }
   }
   private onPlaylist(pu: Playlist) {
      //console.log(`recd playlist for ${pu.deviceKey}`);
      if (pu.deviceKey === this.getDeviceKeyFromStorage()) {
         this.selectedDevicePlaylist = pu;
         this.selectedPlaylistChanged.next(this.selectedDevicePlaylist);
      }
   }
   // obsolete
   //private onPlaylistUpdate(pu: PlaylistUpdate) {
   //   if (pu.deviceKey === this.getDeviceKeyFromStorage()) {
   //      this.currentPlaylist = pu.items;
   //      this.currentPlaylistChanged.next(this.currentPlaylist);
   //   }
   //}
   //----- message subscriptions

   private startUpdate() {
      //console.log(`startUpdate()`);
      this.updateTimer = setInterval(async () => {
         this.localDeviceStatusUpdate();
      }, 1000);
      this.timerStarted = true;
   }
   private stopUpdate() {
      if (this.updateTimer) {
         clearInterval(this.updateTimer);
      }
   }
   private localDeviceStatusUpdate(): any {
      if (this.selectedDeviceStatus && this.awaitedSequenceNumberArrived(this.selectedDeviceStatus)) {
         this.deviceStatusUpdate.next(this.selectedDeviceStatus);
         //console.log(`deviceStatusUpdate for ${ds.key}, state = ${PlayerStates[ds.state]}, major = ${ds.playlistPosition.major}`);
      }
   }
   private awaitedSequenceNumberArrived(ds: DeviceStatus) {
      let r = this.awaitedSequenceNumber === -1
         || this.awaitedSequenceNumber < 1024 && ds.commandSequence >= this.awaitedSequenceNumber
         || ds.commandSequence < this.awaitedSequenceNumber;
      return r;
   }
   private async setInitialDevice() {
      this.devices = await this.getActiveDevices();
      let savedkey = this.getDeviceKeyFromStorage();
      let d: AudioDevice | null = null;
      if (savedkey !== null) {
         let temp = this.getDevice(savedkey);// this.devices.find(x => x.key === this.currentDeviceKey);
         d = temp ? temp : null;
      }
      if (d === null && this.devices.length > 0) {
         d = this.devices[0];
      }
      await this.setDevice(d);
   }
   private async requestSelectedDeviceStatus() {
      if (this.selectedDevice) {
         await this.getAsync<DeviceStatus>(`send/device/${this.selectedDevice.key}/status`);
         return;
      }
      throw "no device available";
   }
   private async requestSelectedPlaylist() {
      if (this.selectedDevice) {
         await this.getAsync<Playlist>(`send/device/${this.selectedDevice.key}/playlist`);
         return;
      }
      throw "no device available";
   }
   private getDeviceKeyFromStorage(): string | null {
      return  localStorage.getItem(this.storageKeyForCurrentDevice);
      //if (this.currentDeviceKey === null) {
         
      //}
      //return this.currentDeviceKey;
   }
   private saveDeviceKeyToStorage(device: AudioDevice | null) {
      if (device === null) {
         removeLocalStorageValue(this.storageKeyForCurrentDevice);
         //this.log.debug(`current device removed`);
      } else {
         setLocalStorageValue(this.storageKeyForCurrentDevice, device.key);
         //this.currentDeviceKey = device.key;
         //this.log.debug(`current device is now ${key}`);
      }
   }
   private async getActiveDevices(): Promise<AudioDevice[]> {
      let query = `get/devices/active/${this.parameterService.getBrowserKey()}`;
      let list = await this.getAsync<AudioDevice[]>(query);
      return new Promise<AudioDevice[]>(resolve => {
         let devices: AudioDevice[] = [];
         for (let item of list) {
            let device = new AudioDevice();
            device.copyProperties(item);
            devices.push(device);
         }
         //this.log.debug(`[PlayerService] getActiveDevices() returned ${list.length} devices`);
         resolve(devices);
      });
   }
}
