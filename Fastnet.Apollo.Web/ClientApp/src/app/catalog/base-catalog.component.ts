import { LibraryService } from "../shared/library.service";
import { Highlighter } from "../shared/common.types";
import { ViewChild, ElementRef, HostBinding, OnInit, AfterViewInit, OnDestroy } from "@angular/core";
import { PopupMessageComponent } from "../../fastnet/controls/popup-message.component";
import { Artist, Work, MusicFile, Track, Performance, Composition, isMusicFile, isTrack, isWork, isPerformance, Movement, ArtistType } from "../shared/catalog.types";
import { PlayerService } from "../shared/player.service";
import { LoggingService } from "../shared/logging.service";
import { ParameterService } from "../shared/parameter.service";
import { CommandPanelComponent } from "./command-panel/command-panel.component";
import { DomSanitizer } from "@angular/platform-browser";
import { DialogResult } from "../../fastnet/core/core.types";
import { sortedInsert } from "../../fastnet/core/common.functions";
import { MessageService } from "../shared/message.service";
import { Subscription } from "rxjs";

export enum CatalogView {
   DefaultView,
   SearchResults
}

export abstract class BaseCatalogComponent implements OnInit, OnDestroy, AfterViewInit/*, ITagEditorCallback*/ {
   
   CatalogView = CatalogView;
   prefixMode: boolean = false;
   searchText: string;
   protected editMode: boolean = false;
   public catalogView: CatalogView = CatalogView.DefaultView;
   @HostBinding("class.is-edge") isEdge = false;
   @ViewChild(CommandPanelComponent, { static: false }) commandPanel: CommandPanelComponent;
   @ViewChild(PopupMessageComponent, { static: false }) popupMessage: PopupMessageComponent;

   protected allArtists: Artist[] = [];
   private subscriptions: Subscription[] = [];
   constructor(private elementRef: ElementRef, protected readonly library: LibraryService,
      private messageService: MessageService,
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
      this.subscriptions.push(this.messageService.newOrModifiedArtist.subscribe(async (id) => await this.onNewOrModifiedArtist(id)));
      this.subscriptions.push(this.messageService.deletedArtist.subscribe((id) => this.onDeletedArtist(id)));
   }
   async ngAfterViewInit() {
      let artistIdList = await this.library.getArtists(this.parameterService.getCurrentStyle());
      for (let id of artistIdList) {
         let a = await this.library.getArtist(id);
         this.addArtistToDefaultView(a);
      }
      console.log(`${artistIdList.length} artist ids loaded`);
   }

   protected async abstract onSearch();
   protected abstract addArtistToDefaultView(a: Artist);
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
   //isMobile() {
   //   return this.parameterService.isMobileDevice();
   //}
   //isIpad() {
   //   return this.parameterService.isIpad();
   //}
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
   protected getBrowser() {
      return this.parameterService.getBrowser();
   }
   private async onNewOrModifiedArtist(id: number) {
      let a = await this.library.getArtist(id);
      if (a.artistType !== ArtistType.Various && a.styles.findIndex((x) => this.parameterService.getCurrentStyle().id === x) > -1) {
         let index = this.allArtists.findIndex((v, i, obj) => {
            return v.id === id;
         });
         if (index === -1) {
            this.addArtistToDefaultView(a);
         } else {
            this.allArtists[index] = a;
         }
      }
   }
   private onDeletedArtist(id: number) {
      let index = this.allArtists.findIndex((v, i) => {
         return v.id === id;// ? i : -1;
      });
      if (index != -1) {
         this.allArtists.splice(index, 1);
      }
   }
}
