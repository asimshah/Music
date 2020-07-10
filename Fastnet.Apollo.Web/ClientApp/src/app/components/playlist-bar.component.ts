import { Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { AudioDevice, Playlist } from '../shared/common.types';
import { Dictionary } from '../../fastnet/core/dictionary.types';
import { PopupDialogComponent } from '../../fastnet/controls/popup-dialog.component';
import { PlayerService } from '../shared/player.service';
import { LoggingService } from '../shared/logging.service';
import { ValidationMethod } from '../../fastnet/controls/controlbase.type';
import { DialogResult } from '../../fastnet/core/core.types';
import { ValidationContext, ValidationResult } from '../../fastnet/controls/controls.types';
import { ParameterService } from '../shared/parameter.service';
import { Subscription } from 'rxjs';
import { PlaylistType } from '../shared/common.enums';
import { PopupMessageComponent, PopupMessageOptions } from '../../fastnet/controls/popup-message.component';
import { PlaylistManagerComponent } from './playlist-manager.component';

enum PlaylistSaveType {
   New,
   Replace
}

@Component({
   selector: 'playlist-bar',
   templateUrl: './playlist-bar.component.html',
   styleUrls: ['./playlist-bar.component.scss']
})
export class PlaylistBarComponent implements OnInit, OnDestroy {
   DialogResult = DialogResult;
   PlaylistSaveType = PlaylistSaveType;
   PlaylistType = PlaylistType;
   @ViewChild('transferPlaylist', { static: false }) transferPlaylistDialog: PopupDialogComponent;
   @ViewChild('savePlaylist', { static: false }) savePlaylistDialog: PopupDialogComponent;
   @ViewChild('selectPlaylist', { static: false }) selectPlaylistDialog: PopupDialogComponent;
   @ViewChild(PopupMessageComponent, { static: false }) popupMessage: PopupMessageComponent;
   @ViewChild(PlaylistManagerComponent, { static: false}) playlistManager: PlaylistManagerComponent;
   currentPlaylist: Playlist | null = null;
   transferModel: TransferPlaylistModel = new TransferPlaylistModel();
   saveModel: SavePlaylistModel = new SavePlaylistModel();
   selectModel: SelectPlaylistModel = new SelectPlaylistModel();
   private device: AudioDevice;// | null = null;
   private subscriptions: Subscription[] = [];
   constructor(private playerService: PlayerService,
      private ps: ParameterService,
      private log: LoggingService) { }
   ngOnInit() {
      this.subscriptions.push(this.playerService.selectedDeviceChanged.subscribe(async (d) => {
         this.onDeviceChanged(d);
      }));
      this.subscriptions.push(this.playerService.selectedPlaylistChanged.subscribe((pl: Playlist) => {
         this.onPlaylistChanged(pl);
      }));
   }
   ngOnDestroy() {
      //this.stopUpdate();
      for (let sub of this.subscriptions) {
         sub.unsubscribe();
      }
      this.subscriptions = [];
   }
   onPlaylistManager() {
      this.playlistManager.open();
      //this.log.information("[PlaylistBarComponent] onPlaylistManager()");
   }
   onDeletePlaylist() {
      let mdOptions = new PopupMessageOptions();
      mdOptions.allowCancel = true;
      mdOptions.warning = true;
      mdOptions.okLabel = "Confirm Delete"
      let message = [
         `Deleting playlist ${this.currentPlaylist.playlistName} will permanently remove it from the database.`,
         "This action cannot be reversed!"
      ];
      this.popupMessage.open(message, async (dr: DialogResult) => {
         if (dr === DialogResult.ok) {
            await this.playerService.deletePlaylist(this.currentPlaylist.id);
         }
      }, mdOptions);
   }
   getPlaylistName() {
      if (this.currentPlaylist) {
         if (this.currentPlaylist.playlistType !== PlaylistType.DeviceList) {
            return this.currentPlaylist.playlistName;
         }
      }
      return "";// ("(auto)");
   }
   getPlaylistDuration() {
      if (this.currentPlaylist && this.currentPlaylist.items.length > 1) {
         return this.currentPlaylist.formattedTotalTime;
      }
      return ("");
   }
   isDevicePlaylist() {
      if (this.currentPlaylist) {
         return this.currentPlaylist.playlistType === PlaylistType.DeviceList;
      }
      return true; // must be initialising
   }
   isMobileDevice() {
      return this.ps.isMobileDevice();
   }
   private onPlaylistChanged(pl: Playlist) {
      this.currentPlaylist = pl;
   }
   private onDeviceChanged(d: AudioDevice) {
      this.device = d;
   }
   //+++++++ select playlist dialog region
   async onSelectPlaylist() {
      this.log.information("[PlaylistBarComponent] onOpenPlaylist()");
      this.selectModel.ready = false;
      this.selectModel.allPlaylists = [];
      this.selectPlaylistDialog.open((dr: DialogResult) => {
         // on close method
         if (dr === DialogResult.ok) {
            this.playerService.setPlaylist(this.device.key, this.selectModel.selectedPlaylist.id);
         }
      }, async () => {
         // on afteropen method
         this.selectModel.allPlaylists = await this.playerService.getAllPlaylists();
         //console.log(`${this.openModel.allPlaylists.length} loaded from server`);
         this.selectModel.ready = true;
      });
   }
   public onSelectPlaylistClosed(dr: DialogResult) {
      this.selectPlaylistDialog.close(dr);
   }
   //------- select playlist dialog region
   //+++++++ save playlist dialog region
   async onSavePlaylist() {
      this.log.information("[PlaylistBarComponent] onSavePlaylist()");
      this.saveModel.ready = false;
      this.saveModel.existingNames = [];
      this.savePlaylistDialog.open(async (dr: DialogResult) => {
         // on close method
         if (dr === DialogResult.ok) {
            switch (this.saveModel.saveType) {
               case PlaylistSaveType.New:
                  await this.playerService.saveNewPlaylist(this.device, this.saveModel.newName);
                  break;
               case PlaylistSaveType.Replace:
                  await this.playerService.replacePlaylist(this.device, this.saveModel.existingName);
                  break;
            }
         }
      }, async () => {
         // on afteropen method
         this.saveModel.existingNames = await this.playerService.getAllPlaylistNames();
         this.saveModel.ready = true;
      });
   }
   public onSavePlaylistClosed(dr: DialogResult) {
      this.savePlaylistDialog.close(dr);
   }
   //------- save playlist dialog region
   //+++++++ transfer playlist dialog region
   async onTransferPlaylist() {
      // i.e from another device
      this.log.information("[PlaylistBarComponent] onTransferPlaylist()");
      this.transferModel.ready = false;
      this.transferModel.devices = [];
      this.transferPlaylistDialog.open(async (dr: DialogResult) => {
         // on close method
         if (dr === DialogResult.ok) {
            await this.playerService.copyPlaylist(this.transferModel.selectedDevice.key, this.device.key);
         }
      }, async () => {
         // on afteropen method
            this.transferModel.devices = await this.playerService.getAvailableDevices().filter(d => d.key != this.device.key);
            this.transferModel.ready = true;
      });
      //this.transferModel = new TransferPlaylistModel(this.transferPlaylistDialog, this.device, this.playerService, this.ps, this.log);
      //await this.transferModel.open(async (selectedDevice) => {
      //   //console.log(`device ${selectedDevice.displayName} selected`);
      //   await this.playerService.copyPlaylist(selectedDevice.key, this.device.key);
      //   this.transferModel = null;
      //});
   }
   public onTransferPlaylistClosed(dr: DialogResult) {
      this.transferPlaylistDialog.close(dr);
   }
   //------- transfer playlist dialog region
}
class SelectPlaylistModel {
   ready: boolean = false;
   allPlaylists: Playlist[] = [];
   selectedPlaylist: Playlist;
   onSelectedPlaylist(pl: Playlist) {
      this.selectedPlaylist = pl;
   }
   isOKEnabled() {
      return this.selectedPlaylist;
   }
}
class SavePlaylistModel {
   ready: boolean = false;
   saveType: PlaylistSaveType = PlaylistSaveType.New;
   public newName: string = "";
   //private newNameIsValid: boolean = false;
   public existingName: string;
   public existingNames: string[];
   public saveTypeNames: string[] = ["New Playlist", "Replace Existing Playlist"];
   public validators: Dictionary<ValidationMethod>;
   private canSaveNewName = false;
   constructor() {
      this.validators = new Dictionary<ValidationMethod>();
      this.validators.add("newplaylist", (vc, v) => {
         return this.newPlaylistValidatorAsync(vc, v);
      });
   }
   isSaveEnabled() {
      switch (this.saveType) {
         case PlaylistSaveType.New:
            return this.canSaveNewName;
         case PlaylistSaveType.Replace:
            return this.existingName && this.existingName.length > 0;
      }
   }
   private newPlaylistValidatorAsync(cs: ValidationContext, val: string) {
      return new Promise<ValidationResult>(resolve => {
         let vr = new ValidationResult();
         let text = (val || "").trim();
         if (text.length > 0) {
            let r = this.existingNames.find((v, i) => v.toLowerCase() === text.toLowerCase());
            if (r) {
               vr.valid = false;
               vr.message = `a playlist with this name already exists`;
            }
         }
         this.canSaveNewName = vr.valid;
         resolve(vr);
      });
   }
}
class TransferPlaylistModel {
   ready: boolean = false;
   devices: AudioDevice[] = [];
   selectedDevice: AudioDevice;
   constructor() {

   }
   //async open(onDeviceSelected: (d: AudioDevice) => void) {
   //   this.GetDevices();
   //   this.transferPlaylistDialog.open((r: DialogResult) => {
   //      if (r === DialogResult.ok) {
   //         let selectedDevice = this.findSelectedDevice();
   //         onDeviceSelected(selectedDevice.device);
   //      }
   //   });
   //   this.ready = true;
   //}

   //onOK() {
   //   this.transferPlaylistDialog.close(DialogResult.ok);
   //}
   //onCancel() {
   //   this.transferPlaylistDialog.close(DialogResult.cancel);
   //}
   onDeviceSelected(sd: AudioDevice) {
      this.selectedDevice = sd;
   }
   isOKEnabled() {
      return this.ready && this.selectedDevice;

   }
   //private findSelectedDevice(): SelectableAudioDevice | null {
   //   for (let x of this.devices) {
   //      if (x.selected === true) {
   //         return x;
   //      }
   //   }
   //   return null;
   //}
   //private async GetDevices() {
   //   let devices = await this.playerService.getAvailableDevices();
   //   this.devices = [];
   //   for (let d of devices) {
   //      if (d.key !== this.currentDevice.key) {
   //         let sd = new SelectableAudioDevice(d);
   //         this.devices.push(sd);
   //      }
   //   }
   //}
}
