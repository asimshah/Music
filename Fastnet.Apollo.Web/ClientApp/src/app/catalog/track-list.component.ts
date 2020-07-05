import { Component, OnInit, Input, ViewChild } from '@angular/core';
import { Track, MusicFile, Movement, isMusicFile, isTrack, isMovement } from '../shared/catalog.types';
import { ParameterService } from '../shared/parameter.service';
import { PlayerService } from '../shared/player.service';
import { PopupMessageComponent } from '../../fastnet/controls/popup-message.component';
import { DialogResult } from '../../fastnet/core/core.types';
//import { MusicStyles } from '../shared/common.enums';
import { CommandPanelComponent, CommandPanelResult, SelectedCommand } from './command-panel/command-panel.component';

@Component({
   selector: 'track-list',
   templateUrl: './track-list.component.html',
   styleUrls: ['./track-list.component.scss']
})
export class TrackListComponent implements OnInit {
   @ViewChild(CommandPanelComponent, { static: false }) commandPanel: CommandPanelComponent;
   @ViewChild(PopupMessageComponent, { static: false }) popupMessage: PopupMessageComponent;
   @Input() tracks: Track[] = [];
   @Input() showTrackNumber = true;
   @Input() highlightText = "";
   @Input() prefixMode: boolean = false;
   @Input() layoutAsSingles = false;
   constructor(private parameterService: ParameterService,
      private playerService: PlayerService) { }

   ngOnInit() {
   }
   singlesLayout() {
      return this.layoutAsSingles;
   }

   canShowMusicFile(mf: MusicFile) {
      return (mf.isGenerated === false || this.canShowGeneratedMusic());
   }
   canShowGeneratedMusic() {
      return this.parameterService.showGeneratedMusic;
   }
   onTrackMouse(t: Track, f: MusicFile, val: boolean) {
      t.isHighLit = val;
      if (f) {
         f.isHighLit = val;
      }
   }
   onClickTrack(e: Event, t: Track) { // e is a hammer event - where are the typescript types for this?
      e.stopPropagation();
      if (this.isTouchDevice()) {
         this.commandPanel.open2(t,
            async (r) => await this.executeCommand(r));
      }
   }
   //onTapTrack(e: any, t: Track) { // e is a hammer event - where are the typescript types for this?
   //   e.srcEvent.stopPropagation();
   //   if (this.isTouchDevice()) {
   //      this.commandPanel.open2(t,
   //         async (r) => await this.executeCommand(r));
   //   }
   //}
   async onPlayTrack(track: Track) {
      await this.playMusic(track);
   }
   async onQueueTrack(track: Track) {
      await this.queueMusic(track);
   }
   async onPlayMusicFile(musicFile: MusicFile) {
      await this.playMusic(musicFile);
   }
   async onQueueMusicFile(musicFile: MusicFile) {
      await this.queueMusic(musicFile);
   }
   async playMusic(entity: MusicFile | Track | Movement) {
      if (this.checkDeviceAvailable()) {
         if (isMusicFile(entity)) {
            await this.playerService.playFile(entity as MusicFile);
         } else if (isTrack(entity) || isMovement(entity)) { // will also match Movement
            await this.playerService.playTrack(entity as Track);
         }
      }
   }
   async queueMusic(entity: MusicFile | Track) {
      if (this.checkDeviceAvailable()) {
         if (isMusicFile(entity)) {
            await this.playerService.queueFile(entity as MusicFile);
         } else if (isTrack(entity) || isMovement(entity)) {
            await this.playerService.queueTrack(entity as Track);
         }
      }
   }
   private async executeCommand(cmd: CommandPanelResult) {
      switch (cmd.selectedCommand) {
         case SelectedCommand.Cancel:
            break;
         case SelectedCommand.Play:
            await this.playMusic(<MusicFile | Track>cmd.entity);
            break;
         case SelectedCommand.Queue:
            await this.queueMusic(<MusicFile | Track>cmd.entity);
            break;
         case SelectedCommand.TagEditor:
            ////if (cmd.targetEntity === TargetEntity.Performance) {
            ////   // await this.westernClassicalTagEditor.initialise(cmd.entity as Performance);
            ////   this.westernClassicalTagEditor.open(cmd.entity as Performance, async (changesMade) => {
            ////      if (changesMade) {
            ////         await this.onSearch();
            ////      }
            ////   });
            ////}
            //break;
            //case SelectedCommand.Reset:
            //   switch (cmd.targetEntity) {
            //      case TargetEntity.Work:
            //         await this.library.resetWork((cmd.entity as Work).id);
            //         break;
            //      case TargetEntity.Performance:
            //         await this.library.resetPerformance((cmd.entity as Performance).id);
            //         break;
            //   }
            //case SelectedCommand.Resample:
            //   switch (cmd.targetEntity) {
            //      case TargetEntity.Work:
            //         await this.library.resampleWork((cmd.entity as Work).id);
            //         break;
            //      case TargetEntity.Performance:
            //         await this.library.resamplePerformance((cmd.entity as Performance).id);
            //         break;
            //   }
            break;
      }
   }
   isTouchDevice() {
      return this.parameterService.isTouchDevice();
   }
   isMobileDevice() {
      return this.parameterService.isMobileDevice();
   }
   private checkDeviceAvailable() {
      let deviceAvailable = this.playerService.hasSelectedDevice();
      if (!deviceAvailable) {
         let messages: string[] = [];
         messages.push(`Select an audio device first`);
         this.popupMessage.open(messages, async (r) => {
            if (r === DialogResult.ok) {

            }
         });
      }
      return deviceAvailable;
   }
}
