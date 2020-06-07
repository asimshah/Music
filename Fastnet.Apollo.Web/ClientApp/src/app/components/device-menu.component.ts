import { Component, OnInit, ViewChild, OnDestroy, AfterViewInit } from '@angular/core';
import { AudioDevice } from '../shared/common.types';
import { PlayerService } from '../shared/player.service';
import { Subscription } from 'rxjs';
import { MessageService } from '../shared/message.service';
import { LoggingService } from '../shared/logging.service';
import { PopupPanelComponent } from '../../fastnet/controls/popup-panel.component';
import { ParameterService } from '../shared/parameter.service';
import { getLocalStorageValue } from '../shared/common.functions';
import { LocalStorageKeys } from '../shared/common.enums';
import { PlaylistManagerComponent } from './playlist-manager.component';

@Component({
   selector: 'device-menu',
   templateUrl: './device-menu.component.html',
   styleUrls: ['./device-menu.component.scss']
})
export class DeviceMenuComponent implements OnInit, OnDestroy {
   @ViewChild(PopupPanelComponent, { static: false }) menu: PopupPanelComponent;
   @ViewChild(PlaylistManagerComponent, { static: false }) plManager: PlaylistManagerComponent;
   devices: AudioDevice[] = [];
   currentDevice: AudioDevice | null = null;

   private subscriptions: Subscription[] = [];
   constructor(private playerService: PlayerService, private messageService: MessageService,
      private log: LoggingService) {
      
   }
   ngOnDestroy() {
      for (let sub of this.subscriptions) {
         sub.unsubscribe();
      }
      this.subscriptions = [];
   }

   async ngOnInit() {

      //this.subscriptions.push(this.playerService.currentDeviceChanged.subscribe((d) => {
      //   this.currentDevice = d;
      //}));
      this.subscriptions.push(this.messageService.deviceDisabled.subscribe((d) => {
         this.updateDevices();
         //console.log(`device ${d.displayName} disabled, list is now ${this.devices.length}`);
      }));
      this.subscriptions.push(this.messageService.deviceEnabled.subscribe((d) => {
         this.updateDevices();
         //console.log(`device ${d.displayName} enabled, list is now ${this.devices.length}`);
      }));
      this.subscriptions.push(this.playerService.playerServiceStarted.subscribe((d) => {
         this.onPlayerServiceStarted();
         //this.updateDevices();
         //console.log(`list is now ${this.devices.length}`);
      }));
      //this.updateDevices();
      //console.log(`current device is ${!this.currentDevice ? "null" : this.currentDevice.displayName}`);
   }
   private onPlayerServiceStarted() {
      this.subscriptions.push(this.playerService.currentDeviceChanged.subscribe((d) => {
         this.currentDevice = d;
      }));
      this.updateDevices();
      console.log(`current device is ${!this.currentDevice ? "null" : this.currentDevice.displayName}`);
   }
   private updateDevices() {
      this.devices = this.playerService.getAvailableDevices();
      this.currentDevice = this.playerService.getCurrentDeviceBeingControlled();
   }
   onDeviceNameClick(e) {
      this.updateDevices();
      if (this.devices.length > 1) {
         this.menu.open(e);
      }
   }
   onPlaylistManagerRequested() {
      this.plManager.open();
   }
   async setDevice(d: AudioDevice) {
      this.playerService.setDevice(d);
   }
}
