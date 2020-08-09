import { LibraryService } from "../shared/library.service";
import { Style } from "../shared/common.types";
import { ViewChild, ElementRef, HostBinding, OnInit, AfterViewInit, OnDestroy } from "@angular/core";
import { PopupMessageComponent } from "../../fastnet/controls/popup-message.component";
import { Artist, Work, MusicFile, Performance, isWork, isPerformance, Movement } from "../shared/catalog.types";
import { PlayerService } from "../shared/player.service";
import { LoggingService } from "../shared/logging.service";
import { ParameterService } from "../shared/parameter.service";
import { CommandPanelComponent, CommandPanelResult, SelectedCommand } from "./command-panel/command-panel.component";
import { DialogResult } from "../../fastnet/core/core.types";
import { Subscription } from "rxjs";

export enum CatalogView {
   DefaultView,
   SearchResults
}

export abstract class BaseCatalogComponent implements OnInit, OnDestroy, AfterViewInit {
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
      protected parameterService: ParameterService,
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
   async playMusic(entity: Work | Performance) {
      if (this.checkDeviceAvailable()) {
         if (isWork(entity)) {
            if (entity.tracks.length === 1) {
               await this.playerService.playTrack(entity.tracks[0]);
            } else {
               await this.playerService.playWork(entity);
            }
         } else if (isPerformance(entity)) {
            if (entity.movements.length === 1) {
               await this.playerService.playTrack(entity.movements[0]);
            } else {
               await this.playerService.playPerformance(entity);
            }
         }
      }
   }
   async queueMusic(entity: Work | Performance) {
      if (this.checkDeviceAvailable()) {
         if (isWork(entity)) {
            if (entity.tracks.length === 1) {
               await this.playerService.queueTrack(entity.tracks[0]);
            } else {
               await this.playerService.queueWork(entity);
            }
         } else if (isPerformance(entity)) {
            if (entity.movements.length === 1) {
               await this.playerService.queueTrack(entity.movements[0]);
            } else {
               await this.playerService.queuePerformance(entity);
            }
         }
      }
   }
   async onPlayPerformance(performance: Performance) {
      await this.playMusic(performance);
   }
   async onQueuePerformance(performance: Performance) {
      await this.queueMusic(performance);
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
            await this.playMusic(<Work | Performance>cmd.entity);
            break;
         case SelectedCommand.Queue:
            await this.queueMusic(<Work | Performance>cmd.entity);
            break;
         case SelectedCommand.Reset:
            break;
         case SelectedCommand.Resample:
            if (isWork(cmd.entity)) {
               await this.library.resampleWork((cmd.entity).id);
            } else if (isPerformance(cmd.entity)) {
               await this.library.resamplePerformance((cmd.entity as Performance).id);
            }
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
