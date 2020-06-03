import { Injectable, Inject } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { DOCUMENT } from '@angular/common';
import { Subject, BehaviorSubject } from 'rxjs';
import { AudioDevice, DeviceStatus, PlaylistItem, PlaylistUpdate, PlayerCommand } from './common.types';
import { LoggingService } from './logging.service';
import { delay } from '../../fastnet/core/common.functions';
import { ParameterService } from './parameter.service';
import { LocalStorageKeys } from './common.enums';
import { getLocalStorageValue, setLocalStorageValue } from './common.functions';



export function messageFactory(ms: MessageService) {
   return () => ms.init();
}

@Injectable()
export class MessageService {
   deviceEnabled = new Subject<AudioDevice>();
   deviceDisabled = new Subject<AudioDevice>();
   deviceNameChanged = new Subject<AudioDevice>();
   deviceStatusUpdate = new Subject<DeviceStatus>();
   playlistUpdate = new Subject<PlaylistUpdate>();
   connectedState = new Subject<boolean>();
   playerCommand = new Subject<PlayerCommand>();
   newOrModifiedArtist = new Subject<number>();
   deletedArtist = new Subject<number>();
   private hubConnection: HubConnection;
   public ready$ = new BehaviorSubject<boolean>(false);
   //private webAudioConnected = false;
   constructor(@Inject(DOCUMENT) private document: Document,
      private parameterService: ParameterService,
      private log: LoggingService) {
      this.parameterService.ready$.subscribe(async (v) => {
         if (v === true) {
            this.init();
         }
      });
   }
   async init() {
      console.log("MessageService: init()");
      let url = this.document.location === null ? "" : this.document.location.href;
      this.hubConnection = new HubConnectionBuilder()
         .withUrl(`${url}playhub`)
         .build();
      this.hubConnection.onclose(async (err) => {
         if (err) {
            this.log.warning(`hub connection closed, error ${err.name}, ${err.message}`);
         }
         else {
            this.log.warning("hub connection closed");
         }
         this.connectedState.next(false);
         await this.restartConnection();
      });
      this.registerMessages();
      await this.start();
      this.log.information("messageService: ready");
      this.ready$.next(true);
   }

   public async start() {
      try {
         await this.hubConnection.start();
         console.log(`[${new Date().toISOString()}] hub connection (re)started`);
         await this.connectBrowser();
         this.connectedState.next(true);
      } catch (err) {
         console.log(`[${new Date().toISOString()}] hub start() failed: ${err}`);
         await this.restartConnection();
      }
   }
   private async restartConnection() {
      let interval = Math.random() * 10 * 1000;
      console.log(`restarting connection after ${interval}`);
      await delay(interval);
      await this.start();
   }
   public async connectBrowser() {
      let parameters = this.parameterService.getParameters();
      //let key = this.getBrowserKey();
      await this.hubConnection.invoke("ConnectBrowser", parameters.browserKey, parameters.clientIPAddress, parameters.browser);
      //if (key != confirmedKey) {
      //    setLocalStorageValue(this.parameterService.getStorageKey(LocalStorageKeys.browserKey), confirmedKey);
      //    console.log(`browser key changed from ${key} to ${confirmedKey}`);
      //}
      //this.browserKey = confirmedKey;
   }
   public async connectWebAudio() {
      await this.hubConnection.invoke("ConnectWebAudio");
   }
   private registerMessages() {

      this.hubConnection.on("SendDeviceEnabled", (d: AudioDevice) => {
         this.deviceEnabled.next(d);
      });
      this.hubConnection.on("SendDeviceNameChanged", (d: AudioDevice) => {
         this.deviceNameChanged.next(d);
      });
      this.hubConnection.on("SendDeviceDisabled", (d: AudioDevice) => {
         this.deviceDisabled.next(d);
      });
      this.hubConnection.on("SendDeviceStatus", (ds: DeviceStatus) => {
         this.deviceStatusUpdate.next(ds);
      });
      this.hubConnection.on("SendPlaylist", (update: PlaylistUpdate) => {
         this.log.debug(`signalr recd: SendPlaylist ${JSON.stringify(update)}`);
         let plu = new PlaylistUpdate();
         plu.copyProperties(update);
         this.playlistUpdate.next(plu);
      });
      this.hubConnection.on("SendCommand", (pc: PlayerCommand) => {
         if (pc.deviceKey === this.parameterService.getBrowserKey()) {
            this.log.debug(`signalr recd: SendCommand ${JSON.stringify(pc)}`);
            this.playerCommand.next(pc);
         }
      });
      this.hubConnection.on("SendArtistNewOrModified", (artistId: number) => {
         console.log(`recd SendArtistNewOrModified for ${artistId}`);
         this.newOrModifiedArtist.next(artistId);
      });
      this.hubConnection.on("SendArtistDeleted", (artistId: number) => {
         console.log(`recd SendArtistDeleted for ${artistId}`);
         this.deletedArtist.next(artistId);
      });
   }
   //private getBrowserKey() {
   //    return getLocalStorageValue(this.parameterService.getStorageKey(LocalStorageKeys.browserKey), "");
   //}
}
