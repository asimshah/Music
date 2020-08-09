import { Component, ViewChild } from '@angular/core';
import { Performance, Track, MusicFile, Work, Composition, isWork, isTrack, isPerformance, Raga, isMusicFile, Movement, isMovement } from "../../shared/catalog.types";
import { PlayerService } from '../../shared/player.service';
import { ParameterService } from '../../shared/parameter.service';
import { PopupDialogComponent } from '../../../fastnet/controls/popup-dialog.component';


// *** DO NOT use (tap) events for the meni-items
// as Hammerjs fails to work with stopPropagation()

export enum SelectedCommand {
   Cancel,
   Play,
   Queue,
   TagEditor,
   Reset,
   Resample
}
export class CommandPanelResult {
   selectedCommand: SelectedCommand;
   entity: Work | Track | Performance
}
@Component({
   selector: 'command-panel',
   templateUrl: './command-panel.component.html',
   styleUrls: ['./command-panel.component.scss']
})
export class CommandPanelComponent {
   @ViewChild(PopupDialogComponent, { static: false }) popup: PopupDialogComponent;
   audioDeviceAvailable: boolean = false;
   private target: MusicFile | Track| Movement | Work | Performance;
   private coverArtUrl: string;
   constructor(private parameterService: ParameterService, private playerService: PlayerService) { }

   isTouchDevice() {
      return this.parameterService.isTouchDevice();
   }
   open2(entity: MusicFile | Track | Movement | Work | Performance, onClose: (r: CommandPanelResult) => void) {
      this.target = entity;
      this.audioDeviceAvailable = this.playerService.hasSelectedDevice();
      this.popup.open((r: CommandPanelResult) => {
         onClose(r);
      });
   }

   close() {
      let cpr = new CommandPanelResult();
      cpr.selectedCommand = SelectedCommand.Cancel;
      this.popup.close(cpr);
   }
   onResample() {
      let cpr = new CommandPanelResult();
      cpr.selectedCommand = SelectedCommand.Resample;
      if (isWork(this.target)) {
         cpr.entity = this.target;
      } else if (isPerformance(this.target)) {
         cpr.entity = this.target;
      }
      this.popup.close(cpr);
   }
   async onPlay(e: Event) {
      e.stopPropagation();
      await this.onPlayOrQueue(SelectedCommand.Play);
   }
   async onQueue(e: Event) {
      e.stopPropagation();
      await this.onPlayOrQueue(SelectedCommand.Queue);
   }

   private async onPlayOrQueue(cmd: SelectedCommand) {
      let cpr = new CommandPanelResult();
      cpr.selectedCommand = cmd;
      if (isMusicFile(this.target)) {

      } else if (isTrack(this.target) || isMovement(this.target)) {
         cpr.entity = this.target;
      } else if (isWork(this.target)) {
         cpr.entity = this.target;
      } else if (isPerformance(this.target)) {
         cpr.entity = this.target;
      }
      this.popup.close(cpr);
   }
   async onCancel(e: Event) {
      e.stopPropagation();
      let cpr = new CommandPanelResult();
      cpr.selectedCommand = SelectedCommand.Cancel;
      this.popup.close(cpr);
   }
   onTest() {

   }

   getImageUrl() {
      if (this.target) {
         if (isTrack(this.target) || isMovement(this.target)) {
            return this.target.coverArtUrl;
         } else if (isWork(this.target)) {
            return this.target.coverArtUrl;
         } else if (isPerformance(this.target)) {
            return this.target.albumCoverArt;
         }
      }
      return this.coverArtUrl;
   }
   getDescription() {
      if (this.target) {
         if (isTrack(this.target)) {
            return [this.target.artistName, this.target.albumName, (<Track>this.target).title];
         } if (isMovement(this.target)) {
            return [this.target.artistName, this.target.albumName, (<Track>this.target).title];         
         } else if (isWork(this.target)) {
            return [this.target.artistName, this.target.name];
         } else if (isPerformance(this.target)) {
            return [this.target.artistName, this.target.workName];
         }
      }
      return [];
   }
   getPerformanceSource(c: Composition | Raga, p: Performance): string {
      let text: string[] = [];
      text.push(c.name);
      text.push(` from &ldquo;${p.albumName}&rdquo;`);
      if (p.highlightedName && p.highlightedName.trim().length > 0) {
         text.push(p.highlightedName);
      }
      if (p.year > 1900) {
         text.push(p.year.toString());
      }
      return text.join(", ");
   }

}
