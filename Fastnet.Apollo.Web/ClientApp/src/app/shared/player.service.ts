import { Injectable, OnDestroy } from "@angular/core";
import { BaseService } from "./base-service.service";
import { HttpClient } from "@angular/common/http";
import { AudioDevice, PlaylistItem, DeviceStatus, PlaylistPosition, PlaylistUpdate } from "./common.types";
import { Track, MusicFile, Work, Performance } from "../shared/catalog.types";

import { LoggingService } from "./logging.service";
import { Subscription, Subject } from "rxjs";
import { MessageService } from "./message.service";
import { PlayerStates, LocalStorageKeys, AudioDeviceType } from "./common.enums";
import { removeLocalStorageValue, setLocalStorageValue, getLocalStorageValue } from "./common.functions";
import { ParameterService } from "./parameter.service";

export function playerServiceFactory(ps: PlayerService) {
   return () => ps.init();
}

@Injectable()
export class PlayerService extends BaseService implements OnDestroy {
   deviceStatusUpdate = new Subject<DeviceStatus>();
   currentDeviceChanged = new Subject<AudioDevice>();
   currentPlaylistChanged = new Subject<PlaylistItem[]>();
   playerServiceStarted = new Subject<boolean>();
   private subscribedDeviceStatus: DeviceStatus | null = null;
   private messageSubscriptions: Subscription[] = [];
   private updateTimer;
   private timerStarted = false;
   private awaitedSequenceNumber = 0;
   private storedDeviceKey: string; // i.e. as stored in local storage
   private currentDeviceKey: string | null = null;
   private devices: AudioDevice[] = [];
   private currentDeviceBeingControlled: AudioDevice | null = null;
   private currentPlaylist: PlaylistItem[] = [];
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
      console.log("PlayerService: init()");
      this.storedDeviceKey = `music:${LocalStorageKeys[LocalStorageKeys.currentDevice]}`;// this.parameterService.getStorageKey(LocalStorageKeys.currentDevice);
      await this.start();
      this.log.information("[PlayerService] started");
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
      return this.currentDeviceBeingControlled !== null;
   }
   public getCurrentDeviceBeingControlled() {
      return this.currentDeviceBeingControlled;
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
   public getCurrentPlaylist() {
      return this.currentPlaylist;
   }
   public async getCurrentDeviceStatus() {
      if (this.currentDeviceBeingControlled) {
         return await this.getAsync<DeviceStatus>(`get/device/${this.currentDeviceBeingControlled.key}/status`);
      }
      throw "no device available";
   }
   public async playFile(mf: MusicFile) {
      if (this.currentDeviceBeingControlled) {
         return await this.getAsync<number>(`play/file/${this.currentDeviceBeingControlled.key}/${mf.id}`);
      }
   }
   public async playTrack(track: Track) {
      if (this.currentDeviceBeingControlled) {
         return await this.getAsync<number>(`play/track/${this.currentDeviceBeingControlled.key}/${track.id}`);
      }
   }
   public async playWork(work: Work) {
      if (this.currentDeviceBeingControlled) {
         return await this.getAsync<number>(`play/work/${this.currentDeviceBeingControlled.key}/${work.id}`);
      }
   }
   public async playPerformance(performance: Performance) {
      if (this.currentDeviceBeingControlled) {
         return await this.getAsync<number>(`play/performance/${this.currentDeviceBeingControlled.key}/${performance.id}`);
      }
   }
   public async playItem(position: PlaylistPosition) {
      if (this.currentDeviceBeingControlled) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`play/item/${this.currentDeviceBeingControlled.key}/${position.major}/${position.minor}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   public async queueFile(mf: MusicFile) {
      if (this.currentDeviceBeingControlled) {
         return await this.getAsync<number>(`queue/file/${this.currentDeviceBeingControlled.key}/${mf.id}`);
      }
   }
   public async queueTrack(track: Track) {
      if (this.currentDeviceBeingControlled) {
         return await this.getAsync<number>(`queue/track/${this.currentDeviceBeingControlled.key}/${track.id}`);
      }
   }
   public async queueWork(work: Work) {
      if (this.currentDeviceBeingControlled) {
         return await this.getAsync<number>(`queue/work/${this.currentDeviceBeingControlled.key}/${work.id}`);
      }
   }
   public async queuePerformance(performance: Performance) {
      if (this.currentDeviceBeingControlled) {
         return await this.getAsync<number>(`queue/performance/${this.currentDeviceBeingControlled.key}/${performance.id}`);
      }
   }
   public async togglePlayPause() {
      if (this.currentDeviceBeingControlled) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`togglePlayPause/${this.currentDeviceBeingControlled.key}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   public async skipForward() {
      if (this.currentDeviceBeingControlled) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`skip/forward/${this.currentDeviceBeingControlled.key}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   public async skipBack() {
      if (this.currentDeviceBeingControlled) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`skip/back/${this.currentDeviceBeingControlled.key}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   public async setVolume(level: number) {
      if (this.currentDeviceBeingControlled) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`set/volume/${this.currentDeviceBeingControlled.key}/${level}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   public async setPosition(position: number) {
      if (this.currentDeviceBeingControlled) {
         this.awaitedSequenceNumber = await this.getAsync<number>(`reposition/${this.currentDeviceBeingControlled.key}/${position}`);
      } else {
         this.awaitedSequenceNumber = 0;
      }
      return this.awaitedSequenceNumber;
   }
   async triggerUpdates() {
      if (this.currentDeviceBeingControlled) {
         this.currentDeviceChanged.next(this.currentDeviceBeingControlled);
         //console.log(`currentDeviceChanged ${this.currentDeviceBeingControlled.displayName}`);

         let ds = await this.getCurrentDeviceStatusInternal();
         this.deviceStatusUpdate.next(this.localiseDeviceStatus(ds));
         //console.log(`deviceStatusUpdate ${this.currentDeviceBeingControlled.displayName} on setting device`);

         this.currentPlaylist = await this.getCurrentPlaylistInternal();
         this.currentPlaylistChanged.next(this.currentPlaylist);
         //console.log(`currentPlaylistChanged ${this.currentDeviceBeingControlled.displayName}, has ${this.currentPlaylist.length} items`);
      } else {
         this.currentDeviceChanged.next();
         this.currentPlaylist = [];
         this.currentPlaylistChanged.next(this.currentPlaylist);
      }
   }
   async setDevice(d: AudioDevice | null) {
      if (d != null) {
         this.currentDeviceBeingControlled = d;
         this.setCurrentDeviceKey(this.currentDeviceBeingControlled.key);
         this.log.information(`[PlayerService] device set to ${this.currentDeviceBeingControlled.toString()}`);
      } else {
         this.currentDeviceBeingControlled = null;
         this.setCurrentDeviceKey(null);
         this.log.information(`[PlayerService] device set to null}`);
      }
      await this.triggerUpdates();
   }
   //
   public async getAllPlaylists() {
      return this.getAsync<string[]>(`get/all/playlists`);
   }
   public async copyPlaylist(from: string, to: string) {
      await this.getAsync<void>(`copy/playlist/${from}/${to}`);
      await this.triggerUpdates();
   }
   public async saveNewPlaylist(device: AudioDevice, name: string) {
      return this.getAsync<void>(`savenew/playlist/${device.key}/${name}`);
   }
   public async replacePlaylist(device: AudioDevice, name: string) {
      return this.getAsync<void>(`replace/playlist/${device.key}/${name}`);
   }
   //
   public async enableWebAudio() {
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
         this.messageSubscriptions.push(this.messageService.deviceDisabled.subscribe(async (d) => {
            await this.onDeviceDisabled(d);
         }));
         this.messageSubscriptions.push(this.messageService.deviceNameChanged.subscribe(async (d) => {
            await this.onDeviceNameChanged(d);
         }));
         this.messageSubscriptions.push(this.messageService.deviceStatusUpdate.subscribe((ds) => {
            this.onReceiveDeviceStatusUpdate(ds);
         }));
         this.messageSubscriptions.push(this.messageService.playlistUpdate.subscribe((pu) => {
            this.onPlaylistUpdate(pu);
         }));
         await this.setInitialDevice();
         this.startUpdate();
      }
   }
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
      if (this.subscribedDeviceStatus && this.awaitedSequenceNumberArrived(this.subscribedDeviceStatus)) {
         let ds = this.localiseDeviceStatus(this.subscribedDeviceStatus);
         this.deviceStatusUpdate.next(ds);
         //console.log(`deviceStatusUpdate for ${ds.key}, state = ${PlayerStates[ds.state]}, major = ${ds.playlistPosition.major}`);
      }
   }
   private awaitedSequenceNumberArrived(ds: DeviceStatus) {
      let r = this.awaitedSequenceNumber === -1
         || this.awaitedSequenceNumber < 1024 && ds.commandSequence >= this.awaitedSequenceNumber
         || ds.commandSequence < this.awaitedSequenceNumber;
      return r;
   }
   private localiseDeviceStatus(ds: DeviceStatus): DeviceStatus {
      let temp = new DeviceStatus();
      temp.copyProperties(ds);
      return temp;
   }
   private async setInitialDevice() {
      this.devices = await this.getActiveDevices();
      this.getCurrentDeviceKey();
      let d: AudioDevice | null = null;
      if (this.currentDeviceKey !== null) {
         let temp = this.devices.find(x => x.key === this.currentDeviceKey);
         d = temp ? temp : null;
      }
      if (d === null && this.devices.length > 0) {
         d = this.devices[0];
      }
      await this.setDevice(d);
   }
   private onReceiveDeviceStatusUpdate(ds: DeviceStatus) {
      if (ds.key === this.getCurrentDeviceKey()) {
         this.subscribedDeviceStatus = ds;
      } else {
         this.subscribedDeviceStatus = null;
      }
   }
   private onPlaylistUpdate(pu: PlaylistUpdate) {
      if (pu.deviceKey === this.getCurrentDeviceKey()) {
         this.currentPlaylist = pu.items;
         this.currentPlaylistChanged.next(this.currentPlaylist);
         //console.log(`currentPlaylistChanged ${this.currentDeviceBeingControlled!.displayName}  [${pu.deviceKey}]`);
      }
   }
   private async onDeviceEnabled(d: AudioDevice) {
      let localAudioKey = this.parameterService.getParameters().browserKey;
      //if (d.type === AudioDeviceType.Browser) {
      //    console.log(`received browser device ${d.key}, local device is ${localAudioKey}`);
      //}
      if (d.type === AudioDeviceType.Browser && d.key != localAudioKey) {
         // we do not add devices from other browsers ..
         return;
      }

      let index = this.devices.findIndex(x => x.key === d.key);
      if (index === -1) {
         this.devices.push(d);
      }
      if (!this.currentDeviceBeingControlled) {
         await this.setInitialDevice();
      }
   }
   private async onDeviceDisabled(d: AudioDevice) {
      //this.log.information(`device ${d.displayName} removed`);
      let index = this.devices.findIndex(x => x.key === d.key);
      if (index > -1) {
         let device = this.devices[index];
         this.devices.splice(index, 1);
         if (!this.currentDeviceBeingControlled || this.currentDeviceBeingControlled.key == device.key) {
            await this.setInitialDevice();
         }
      } else {
         if (d.type !== AudioDeviceType.Browser) {
            this.log.error(`disabled device ${d.displayName} not found in local list`);
         }
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
   private async getCurrentDeviceStatusInternal() {
      if (this.currentDeviceBeingControlled) {
         return await this.getAsync<DeviceStatus>(`get/device/${this.currentDeviceBeingControlled.key}/status`);
      }
      throw "no device available";
   }
   private async getCurrentPlaylistInternal() {
      let list: PlaylistItem[] = [];
      let device = this.currentDeviceBeingControlled;
      if (device) {
         let result = await this.getAsync<PlaylistItem[]>(`get/playlist/${device.key}`);
         return new Promise<PlaylistItem[]>((resolve) => {
            for (let item of result) {
               let pli = new PlaylistItem();
               pli.copyProperties(item);
               list.push(pli);
            }
            resolve(list);
         });
      }
      else {
         return list;
      }
   }
   private getCurrentDeviceKey(): string | null {
      // returns null if the key is not present
      if (this.currentDeviceKey === null) {
         this.currentDeviceKey = localStorage.getItem(this.storedDeviceKey);
      }
      return this.currentDeviceKey;
   }
   private setCurrentDeviceKey(key: string | null) {
      if (key === null) {
         removeLocalStorageValue(this.storedDeviceKey);
         //this.log.debug(`current device removed`);
      } else {
         setLocalStorageValue(this.storedDeviceKey, key);
         this.currentDeviceKey = key;
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
         this.log.debug(`[PlayerService] getActiveDevices() returned ${list.length} devices`);
         resolve(devices);
      });
   }
}
