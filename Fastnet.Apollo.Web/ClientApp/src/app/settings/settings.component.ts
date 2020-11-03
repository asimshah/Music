import { Component, OnInit, ViewChild } from '@angular/core';
import { PopupDialogComponent, PopupCloseHandler } from '../../fastnet/controls/popup-dialog.component';
import { EditorResult } from '../shared/catalog.types';
import { LibraryService } from '../shared/library.service';
import { PlayerService } from '../shared/player.service';
import { Parameters, AudioDevice } from '../shared/common.types';
import { ParameterService } from '../shared/parameter.service';
import { TabComponent } from '../../fastnet/controls/tab.component';
import { AudioDeviceType, MusicStyles } from '../shared/common.enums';

@Component({
   selector: 'app-settings',
   templateUrl: './settings.component.html',
   styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
   MusicStyles = MusicStyles;
   AudioDeviceType = AudioDeviceType;
   @ViewChild('settingsdialog', { static: false }) popup: PopupDialogComponent;
   version: string = "(unknown)";
   parameters: Parameters;
   devices: AudioDevice[] = [];
   get showGeneratedMusic(): boolean {
      return this.parameterService.showGeneratedMusic;
   }
   set showGeneratedMusic(val: boolean) {
      this.parameterService.showGeneratedMusic = val;
      //console.log(`showGeneratedMusic = ${this.parameterService.showGeneratedMusic}`);
   }
   private closeHandler: (r: EditorResult) => void;
   constructor(private parameterService: ParameterService, private ls: LibraryService, private playerService: PlayerService) {
      //this.popularSettings = this.parameterService.getPopularSettings();
   }

   ngOnInit() {
      this.parameters = this.parameterService.getParameters();
      this.version = this.parameters.version;
   }
   open(onClose: PopupCloseHandler) {
      //this.currentSettings = SettingsSelector.Popular;
      this.closeHandler = onClose;
      this.popup.open((r: EditorResult) => { this.popupClosed(r) });
   }
   popupClosed(r: EditorResult): void {
      this.closeHandler(r);
   }
   onClose() {
      this.popup.close(EditorResult.cancel);
   }
   isStyleEnabled(ms: MusicStyles) {
      return this.parameters.styles.find(x => x.id === ms).enabled;
   }
   async onResetDatabase() {
      await this.ls.resetDatabase();
   }
   async onStartMusicScanner() {
      await this.ls.startMusicScanner();
   }
   async onRescanStyle(ms: MusicStyles) {
      await this.ls.rescanStyle(ms);
   }
   async onTabChanged(tab: TabComponent) {
      if (tab && tab.title === "Audio Devices") {
         if (this.devices.length === 0) {
            this.devices = await this.playerService.getDevices(true);
         }
      }
   }
   async onDeviceUpdate(d: AudioDevice) {
      await this.playerService.updateDevice(d);
   }
}
