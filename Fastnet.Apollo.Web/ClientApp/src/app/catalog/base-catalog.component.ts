import { LibraryService } from "../shared/library.service";
import { Style } from "../shared/common.types";
import { ViewChild, ElementRef, HostBinding, OnInit, AfterViewInit, OnDestroy } from "@angular/core";
import { PopupMessageComponent } from "../../fastnet/controls/popup-message.component";
import { Artist, Work, MusicFile, Track, Performance, Composition, isMusicFile, isTrack, isWork, isPerformance, Movement } from "../shared/catalog.types";
import { PlayerService } from "../shared/player.service";
import { LoggingService } from "../shared/logging.service";
import { ParameterService } from "../shared/parameter.service";
import { CommandPanelComponent, CommandPanelResult, SelectedCommand } from "./command-panel/command-panel.component";
import { DomSanitizer } from "@angular/platform-browser";
import { DialogResult } from "../../fastnet/core/core.types";
//import { sortedInsert } from "../../fastnet/core/common.functions";
//import { MessageService } from "../shared/message.service";
import { Subscription } from "rxjs";

export enum CatalogView {
   DefaultView,
   SearchResults
}

export abstract class BaseCatalogComponent implements OnInit, OnDestroy, AfterViewInit/*, ITagEditorCallback*/ {
   private guard = false;
   CatalogView = CatalogView;
   prefixMode: boolean = false;
   searchText: string;
   protected editMode: boolean = false;
   public catalogView: CatalogView = CatalogView.DefaultView;
   @HostBinding("class.is-edge") isEdge = false;
   @ViewChild(CommandPanelComponent, { static: false }) commandPanel: CommandPanelComponent;
   @ViewChild(PopupMessageComponent, { static: false }) popupMessage: PopupMessageComponent;
   protected currentStyle: Style;
   private subscriptions: Subscription[] = [];
   constructor(private elementRef: ElementRef, protected readonly library: LibraryService,
      //private messageService: MessageService,
      protected parameterService: ParameterService, private sanitizer: DomSanitizer,
      private readonly playerService: PlayerService, protected log: LoggingService) {
   }
   ngOnDestroy() {
      for (let sub of this.subscriptions) {
         sub.unsubscribe();
      }
      this.subscriptions = [];
   }
   ngOnInit() {
      if (this.getBrowser() === "edge") {
         this.isEdge = true;
      }
   }
   async ngAfterViewInit() {
      if (this.guard === false) {
         this.guard = true;
         this.currentStyle = this.parameterService.getCurrentStyle();
      }
   }
   protected async abstract onSearch();
   public async setSearch(text: string) {
      this.searchText = text;
      this.catalogView = CatalogView.SearchResults;
      await this.onSearch();
   }
   public async clearSearch() {
      //console.log(`$clear search`);
      this.catalogView = CatalogView.DefaultView;
   }
   // i need these handlers because I need to set swipe events on the
   // catalog - these then bubble up to where they are handled...
   onSwipeLeft() {
      //console.log("onSwipeLeft()");
   }
   onSwipeRight() {
      //console.log("onSwipeRight()");
   }
   async onArtistClick(artist: Artist) {
      console.log(`${artist.name} clicked`);
      this.setSearch(artist.name);
   }
   isTouchDevice() {
      return this.parameterService.isTouchDevice();
   }
   toggleArtistEditMode(c: Artist) {
      c.editMode = !c.editMode;
   }
   toggleWorkEditMode(w: Work) {
      w.editMode = !w.editMode;
   }
   toggleShowWorks(c: Artist) {
      c.showWorks = !c.showWorks;
   }
   onMovementMouse(m: Movement, f: MusicFile, val: boolean) {
      m.isHighLit = val;
      f.isHighLit = val;
   }
   getPerformanceSource(p: Performance): string {
      let text: string[] = [];
      text.push(`"${p.albumName}"`);
      if (p.performers.trim().length > 0) {
         text.push(p.performers);
      }
      if (p.year > 1900) {
         text.push(p.year.toString());
      }
      return text.join(", ");
   }
   async playMusic(entity: MusicFile | Track | Movement | Work | Performance) {
      if (this.checkDeviceAvailable()) {
         if (isMusicFile(entity)) {
            await this.playerService.playFile(entity as MusicFile);
         } else if (isTrack(entity)) { // will also match Movement
            await this.playerService.playTrack(entity as Track);
         } else if (isWork(entity)) {
            await this.playerService.playWork(entity as Work);
         } else if (isPerformance(entity)) {
            await this.playerService.playPerformance(entity as Performance);
         }
      }
   }
   async queueMusic(entity: MusicFile | Track | Work | Performance) {
      if (this.checkDeviceAvailable()) {
         if (isMusicFile(entity)) {
            await this.playerService.queueFile(entity as MusicFile);
         } else if (isTrack(entity)) {
            await this.playerService.queueTrack(entity as Track);
         } else if (isWork(entity)) {
            await this.playerService.queueWork(entity as Work);
         } else if (isPerformance(entity)) {
            await this.playerService.queuePerformance(entity as Performance);
         }
      }
   }
   async onPlayMusicFile(musicFile: MusicFile) {
      await this.playMusic(musicFile);
   }
   async onQueueMusicFile(musicFile: MusicFile) {
      await this.queueMusic(musicFile);
   }
   async onPlayPerformance(performance: Performance) {
      await this.playMusic(performance);
   }
   async onQueuePerformance(performance: Performance) {
      await this.queueMusic(performance);
   }
   async onPlayMovement(movement: Movement) {
      await this.playMusic(movement);
   }
   async onQueueMovement(movement: Movement) {
      await this.queueMusic(movement);
   }
   onPerformanceMouse(p: Performance, val: boolean) {
      p.isHighLit = val;
    }
   canShowMusicFile(mf: MusicFile) {
      return (mf.isGenerated === false || this.canShowGeneratedMusic());
   }
   canShowGeneratedMusic() {
      return this.parameterService.showGeneratedMusic;
   }
   protected getBrowser() {
      return this.parameterService.getBrowser();
   }
   protected async executeCommand(cmd: CommandPanelResult) {
      switch (cmd.selectedCommand) {
         case SelectedCommand.Cancel:
            break;
         case SelectedCommand.Play:
            await this.playMusic(cmd.entity);
            break;
         case SelectedCommand.Queue:
            await this.queueMusic(cmd.entity);
            break;
         //case SelectedCommand.TagEditor:
         //   //if (cmd.targetEntity === TargetEntity.Performance) {
         //   //   // await this.westernClassicalTagEditor.initialise(cmd.entity as Performance);
         //   //   this.westernClassicalTagEditor.open(cmd.entity as Performance, async (changesMade) => {
         //   //      if (changesMade) {
         //   //         await this.onSearch();
         //   //      }
         //   //   });
         //   //}
         //   break;
         case SelectedCommand.Reset:
            //switch (cmd.targetEntity) {
            //   case TargetEntity.Work:
            //      await this.library.resetWork((cmd.entity as Work).id);
            //      break;
            //   case TargetEntity.Performance:
            //      await this.library.resetPerformance((cmd.entity as Performance).id);
            //      break;
            //}
         case SelectedCommand.Resample:
            if (isWork(cmd.entity)) {
               await this.library.resampleWork((cmd.entity).id);
            } else if (isPerformance(cmd.entity)) {
               await this.library.resamplePerformance((cmd.entity as Performance).id);
            }
            //switch (cmd.targetEntity) {
            //   case TargetEntity.Work:
            //      await this.library.resampleWork((cmd.entity as Work).id);
            //      break;
            //   case TargetEntity.Performance:
            //      await this.library.resamplePerformance((cmd.entity as Performance).id);
            //      break;
            //}
            break;
      }
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
