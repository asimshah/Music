import { Component, ViewChild } from '@angular/core';
import { Performance, Track, MusicFile, Work, Composition, isWork, isTrack, isComposition, isPerformance } from "../../shared/catalog.types";
import { PlayerService } from '../../shared/player.service';
import { LoggingService } from '../../shared/logging.service';
import { ParameterService } from '../../shared/parameter.service';
import { MusicStyles } from '../../shared/common.enums';
import { PopupDialogComponent } from '../../../fastnet/controls/popup-dialog.component';

export enum TargetEntity {
   MusicFile,
   Track,
   Work,
   Performance
}
export enum SelectedCommand {
   Cancel,
   Play,
   Queue,
   //Details,
   TagEditor,
   Reset
}
export class CommandPanelResult {
   selectedCommand: SelectedCommand;
   targetEntity: TargetEntity;
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
   description = "";
   private targetEntity: TargetEntity = TargetEntity.MusicFile;
   private track: Track; // movement is a track
   private musicFile: MusicFile;
   private work: Work;
   private performance: Performance;
   private composition: Composition;
   private currentStyle: MusicStyles;

   constructor(private parameterService: ParameterService, private playerService: PlayerService,
      private log: LoggingService) { }
   //isMobile() {
   //    return this.parameterService.isMobileDevice();
   //}
   //isIpad() {
   //    return this.parameterService.isIpad();
   //}
   isTouchDevice() {
      return this.parameterService.isTouchDevice();// this.isMobile() || this.isIpad();
   }
   open(style: MusicStyles,
      firstEntity: Work | Track | Composition,
      secondEntity: Track | Performance | null,
      thirdEntity: Track | null,
      onClose: (r: CommandPanelResult) => void) {
      if (isWork(firstEntity) && secondEntity == null) {
         this.targetEntity = TargetEntity.Work;
         this.work = firstEntity;
      } else if (isTrack(secondEntity) && isWork(firstEntity)) {
         this.targetEntity = TargetEntity.Track;
         this.track = secondEntity;
         this.work = firstEntity;
      } else if (isComposition(firstEntity) && secondEntity !== null && isPerformance(secondEntity) && thirdEntity !== null && isTrack(thirdEntity)) {
         this.targetEntity = TargetEntity.Track;
         this.composition = firstEntity;
         this.performance = secondEntity;
         this.track = thirdEntity;
      } else if (isComposition(firstEntity) && secondEntity !== null && isPerformance(secondEntity)) {
         this.targetEntity = TargetEntity.Performance;
         this.composition = firstEntity;
         this.performance = secondEntity;
      } else {
         console.error(`CommandPanel open() called with incorrect parameters`);
      }
      this.currentStyle = style;
      this.audioDeviceAvailable = this.playerService.hasSelectedDevice();
      this.setDescription();
      this.popup.open((r: CommandPanelResult) => {
         onClose(r);
      });

   }
   close() {
      let cpr = new CommandPanelResult();
      cpr.selectedCommand = SelectedCommand.Cancel;
      this.popup.close(cpr);
   }
   onReset() {
      let cpr = new CommandPanelResult();
      switch (this.targetEntity) {
         case TargetEntity.Work:
            cpr.entity = this.work;
            cpr.targetEntity = TargetEntity.Work;
            cpr.selectedCommand = SelectedCommand.Reset;
            break;
         case TargetEntity.Performance:
            cpr.entity = this.performance;
            cpr.targetEntity = TargetEntity.Performance;
            cpr.selectedCommand = SelectedCommand.Reset;
            break;
      }
      this.popup.close(cpr);
   }
   async onPlay() {
      let cpr = new CommandPanelResult();
      cpr.selectedCommand = SelectedCommand.Play;
      switch (this.targetEntity) {
         //case TargetEntity.MusicFile:
         //    cpr.entity = this.musicFile;
         //    cpr.targetEntity = TargetEntity.MusicFile;
         //    cpr.selectedCommand = SelectedCommand.Play;
         //    //await this.playerService.playFile(this.musicFile);
         //    break;
         case TargetEntity.Track:
            cpr.entity = this.track;
            cpr.targetEntity = TargetEntity.Track;
            break;
         case TargetEntity.Work:
            cpr.entity = this.work;
            cpr.targetEntity = TargetEntity.Work;
            break;
         case TargetEntity.Performance:
            cpr.entity = this.performance;
            cpr.targetEntity = TargetEntity.Performance;
            break;
      }
      this.popup.close(cpr);
   }

   async onQueue() {
      let cpr = new CommandPanelResult();
      cpr.selectedCommand = SelectedCommand.Queue;
      switch (this.targetEntity) {
         case TargetEntity.Track:
            cpr.entity = this.track;
            cpr.targetEntity = TargetEntity.Track;
            break;
         case TargetEntity.Work:
            cpr.entity = this.work;
            cpr.targetEntity = TargetEntity.Work;
            break;
         case TargetEntity.Performance:
            cpr.entity = this.performance;
            cpr.targetEntity = TargetEntity.Performance;
            break;
      }
      this.popup.close(cpr);

   }

   async onCancel() {
      let cpr = new CommandPanelResult();
      cpr.selectedCommand = SelectedCommand.Cancel;
      this.popup.close(cpr);
   }
   onTest() {

   }
   isTagEditorAvailable() {
      return (innerHeight / screen.height) * 100.0 > 80.0;
   }
   onTagEditor() {
      this.log.information(`tag editor, target entity ${TargetEntity[this.targetEntity]}`);
      let cpr = new CommandPanelResult();
      switch (this.targetEntity) {
         case TargetEntity.Performance:
            cpr.entity = this.performance;
            cpr.targetEntity = TargetEntity.Performance;
            cpr.selectedCommand = SelectedCommand.TagEditor;
            this.popup.close(cpr);
            break;
         case TargetEntity.Work:
            cpr.entity = this.work;
            cpr.targetEntity = TargetEntity.Work;
            cpr.selectedCommand = SelectedCommand.TagEditor;
            this.popup.close(cpr);
            break;
         default:
            this.log.information(`tag editor not implemented`);
            cpr.selectedCommand = SelectedCommand.Cancel;
            this.popup.close(cpr);
            break;
      }
   }

   getImageUrl() {
      let url = "";
      switch (this.targetEntity) {
         case TargetEntity.MusicFile:

            break;
         case TargetEntity.Track:
            switch (this.currentStyle) {
               case MusicStyles.Popular:
                  url = this.work.coverArtUrl;
                  break;
               case MusicStyles.WesternClassical:
                  url = this.performance.albumCoverArt;
                  break;
            }
            break;
         case TargetEntity.Work:
            url = this.work.coverArtUrl;
            break;
         case TargetEntity.Performance:
            url = this.performance.albumCoverArt;
            break;
      }
      return url;
   }

   private setDescription() {
      switch (this.targetEntity) {
         case TargetEntity.MusicFile:
            this.description = "some music file";
            break;
         case TargetEntity.Track:
            switch (this.currentStyle) {
               case MusicStyles.Popular:
                  this.description = `<div>${this.work.name}</div><div>${this.track.title}</div>`;// this.track.title;
                  break;
               case MusicStyles.WesternClassical:
                  this.description = `<div>${this.composition.name}</div><div>${this.track.title}</div>`;
                  break;
            }
            break;
         case TargetEntity.Work:
            this.description = this.work.name;
            break;
         case TargetEntity.Performance:
            this.description = this.getPerformanceSource(this.composition, this.performance);
            break;
      }
   }
   getPerformanceSource(c: Composition, p: Performance): string {
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
