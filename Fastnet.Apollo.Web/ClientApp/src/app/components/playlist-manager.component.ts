import { Component, OnInit, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { PopupDialogComponent } from '../../fastnet/controls/popup-dialog.component';
import { DialogResult } from '../../fastnet/core/core.types';
import { PlayerService } from '../shared/player.service';
import { Playlist, PlaylistItem } from '../shared/common.types';
import { PlaylistItemType, MusicStyles } from '../shared/common.enums';
import TimeSpan from '../../fastnet/core/timespan.types';
import { PopupMessageComponent } from '../../fastnet/controls/popup-message.component';
import { ValidationContext, ValidationResult } from '../../fastnet/controls/controls.types';
import { Dictionary } from '../../fastnet/core/dictionary.types';
import { ValidationMethod } from '../../fastnet/controls/controlbase.type';
import { ListViewItemMoved } from '../../fastnet/controls/list-view.component';



@Component({
   selector: 'playlist-manager',
   templateUrl: './playlist-manager.component.html',
   styleUrls: ['./playlist-manager.component.scss']
})
export class PlaylistManagerComponent /*implements OnInit, AfterViewInit */{
   PlaylistItemType = PlaylistItemType;
   @ViewChild(PopupDialogComponent, { static: false }) popup: PopupDialogComponent;
   @ViewChild(PopupMessageComponent, { static: false }) popupMessage: PopupMessageComponent;
   playlists: Playlist[] = [];
   selectedPlaylist: Playlist = null;
   selectedPlaylistJson: string = "";
   savedPlaylistJson: string = "";
   public validators: Dictionary<ValidationMethod>;
   constructor(private playerService: PlayerService) {
      this.validators = new Dictionary<ValidationMethod>();
      this.validators.add("playlistname", (vc, v) => {
         return this.playlistNameValidatorAsync(vc, v);
      });

   }

   //ngOnInit() {
   //}
   //ngAfterViewInit() {
   //   console.log(`PlaylistManagerComponent popup reference ${this.popup.reference}`);
   //}
   async open() {
      this.selectedPlaylist = null;
      this.playlists = await this.playerService.getAllPlaylists();
      this.popup.open((r: DialogResult) => {

      });
   }
   onNameChange() {
      this.selectedPlaylistJson = JSON.stringify(this.selectedPlaylist);
   }
   onSelectedPlaylist(pl: Playlist) {
      if (!this.hasChanges()) {
         if (this.selectedPlaylist !== pl) {
            this.selectedPlaylist = pl;
            this.selectedPlaylistJson = JSON.stringify(this.selectedPlaylist);
            this.savedPlaylistJson = this.selectedPlaylistJson;
            //console.log("selected playlist changed");
         } else {
            //console.log("selected playlist not changed");
         }
      } else {
         this.popupMessage.open("There are unsaved changes. First save these, or choose \"Undo Changes\"", (r) => {
         }, { caption: "Playlist Manager",  warning: true })
      }
   }
   onPlaylistItemMoved(args: ListViewItemMoved) {
      console.log(`item at ${args.from} to be moved to ${args.to}`);
      //this.move(args.from, args.to - args.from - 1);
      this.move2(args.from, args.to);
   }
   onClose() {
      if (this.hasChanges()) {
         this.popupMessage.open("There are unsaved changes which will be lost if you press OK", (r) => {
            if (r === DialogResult.ok) {
               {
                  this.popup.close();
               }
            }
         }, { caption: "Playlist Manager", allowCancel: true, warning: true})
      } else {
         this.popup.close();
      }

   }
   async onSave() {
      let sequence = 1;
      for (let pl of this.selectedPlaylist.items) {
         pl.position.major = sequence++;
      }
      await this.playerService.updatePlaylist(this.selectedPlaylist);
      this.selectedPlaylistJson = JSON.stringify(this.selectedPlaylist);
      this.savedPlaylistJson = this.selectedPlaylistJson;
   }
   getPlaylistDuration(playlist: Playlist) {
      let currentTotal = this.calcDuration(playlist);
      let ts = TimeSpan.fromMilliSeconds(currentTotal);
      return ts.toString();
   }

   getSubitemCount(pli: PlaylistItem) {
      switch (pli.musicStyle) {
         case MusicStyles.WesternClassical:
         case MusicStyles.IndianClassical:
            return `${pli.subItems.length} movement(s)`;
         default:
            return `${pli.subItems.length} track(s)`;
      }
   }
   canSave() {
      return this.hasChanges() && this.popup && this.popup.isValid();
   }
   removeItem(pl: PlaylistItem) {
      let index = this.selectedPlaylist.items.findIndex(x => x === pl);
      this.selectedPlaylist.items.splice(index, 1);
      this.selectedPlaylistJson = JSON.stringify(this.selectedPlaylist)
   }
   undoChanges() {
      let index = this.playlists.findIndex(x => x === this.selectedPlaylist);
      let temp: Playlist = JSON.parse(this.savedPlaylistJson);
      let pl = new Playlist();
      pl.copyProperties(temp);
      this.playlists[index] = pl;
      this.selectedPlaylist = this.playlists[index];
      this.selectedPlaylistJson = JSON.stringify(this.selectedPlaylist);
      this.savedPlaylistJson = this.selectedPlaylistJson;
   }
   move(index: number, offset: number) {
      let itemToMove = this.selectedPlaylist.items.splice(index, 1);
      //let offset = 1;
      this.selectedPlaylist.items.splice(index + offset, 0, itemToMove[0]);
      this.selectedPlaylistJson = JSON.stringify(this.selectedPlaylist);
   }
   move2(from: number, to: number) {
      let itemToMove = this.selectedPlaylist.items.splice(from, 1);
      let offset = to > from ? -1 : 0;
      this.selectedPlaylist.items.splice(to + offset, 0, itemToMove[0]);
      this.selectedPlaylistJson = JSON.stringify(this.selectedPlaylist);
   }
   hasChanges() {
      return this.selectedPlaylist && this.savedPlaylistJson !== this.selectedPlaylistJson;
   }
   //test() {
   //   this.popup.listDescendents();
   //}
   private calcDuration(pl: Playlist) {
      let total = 0;
      for (let item of pl.items) {
         total += item.totalTime;
      }
      return total;
   }
   private playlistNameValidatorAsync(cs: ValidationContext, val: string) {
      return new Promise<ValidationResult>(resolve => {
         let vr = new ValidationResult();
         let text = (val || "").trim();
         if (text.length > 0) {
            let existingNames = this.playlists.filter(x => x.id !== this.selectedPlaylist.id).map(x => x.playlistName);
            let r = existingNames.find((v, i) => v.toLowerCase() === text.toLowerCase());
            if (r) {
               vr.valid = false;
               vr.message = `a playlist with this name already exists`;
            }
         }
         resolve(vr);
      });
   }
}
