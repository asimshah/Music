import { Component, ViewChild, ElementRef } from '@angular/core';
import { BaseCatalogComponent } from '../base-catalog.component';
import { LibraryService } from '../../shared/library.service';
import { Artist, Work, Track, MusicFile, OpusType } from "../../shared/catalog.types";
import { SearchKey } from '../../shared/common.types';
import { PlayerService } from '../../shared/player.service';
import { LoggingService } from '../../shared/logging.service';
import { ParameterService } from '../../shared/parameter.service';
import { DomSanitizer } from '@angular/platform-browser';
import { MusicStyles } from '../../shared/common.enums';
import { CommandPanelResult, SelectedCommand, TargetEntity } from '../command-panel/command-panel.component';
import { MessageService } from '../../shared/message.service';
import { sortedInsert } from '../../../fastnet/core/common.functions';
import { PopularTagEditorComponent } from './popular-tag-editor/popular-tag-editor.component';

class TrackKey extends SearchKey {
   number: number | null;
}
class TrackResult {
   track: TrackKey;
}
class WorkResult {
   work: SearchKey;
   workIsMatched: boolean;
   tracks: TrackResult[];
}
class PopularResult {
   artist: SearchKey;
   artistIsMatched: boolean;
   works: WorkResult[];
}
class PopularResults {
   prefixMode: boolean;
   results: PopularResult[];
}

enum CatalogMode {
   Search
}

@Component({
   selector: 'app-popular-catalog',
   templateUrl: './popular-catalog.component.html',
   styleUrls: [
      './popular-catalog.component.scss'
   ]
})
export class PopularCatalogComponent extends BaseCatalogComponent {
   OpusType = OpusType;
   @ViewChild(PopularTagEditorComponent, { static: false }) popularTagEditor: PopularTagEditorComponent;
   //@ViewChild(PopularArtistEditorComponent) artistEditor: PopularArtistEditorComponent;
   private artistsInCurrentSearch: Artist[] = [];
   albumEditDisabled = true;
   matchedWorks: Work[] = []; // i.e. where work name was matched by search (but not the artist name)
   matchedTracks: Track[] = []; // i.e. where track name was matched but not work or artist
   searchFoundNothing = false;
   allowEditArtists = false;
   popularSettingsSubscription;
   constructor(elementRef: ElementRef, libraryService: LibraryService,
      messageService: MessageService,
      ps: ParameterService,
      sanitizer: DomSanitizer, playerService: PlayerService, log: LoggingService) {
      super(elementRef, libraryService, messageService, ps, sanitizer, playerService, log);
      this.popularSettingsSubscription = this.parameterService.popularSettings.subscribe((ps) => {
         this.allowEditArtists = ps.showArtists;
      });
      console.log('PopularCatalogComponent ctor()');
   }

   //onEditArtist(artist: Artist) {
   //    this.artistEditor.open(artist, () => { });
   //}
   async onPlayAlbum(artist: Artist, album: Work) {
      await this.playMusic(album);
      //if (this.checkDeviceAvailable()) {
      //    await this.playerService.playWork(album);
      //}
   }
   async onQueueAlbum(artist: Artist, album: Work) {
      await this.queueMusic(album);
      //if (this.checkDeviceAvailable()) {
      //    await this.playerService.queueWork(album);
      //}
   }
   async onPlayTrack(track: Track) {
      await this.playMusic(track);
   }
   async onQueueTrack(track: Track) {
      await this.queueMusic(track);
   }
   onAlbumMouse(w: Work, val: boolean) {
      w.isHighLit = val;
   }
   onTrackMouse(t: Track, f: MusicFile, val: boolean) {
      t.isHighLit = val;
      if (f) {
         f.isHighLit = val;
      }
   }
   onTapTrack(w: Work, t: Track) {
      this.log.information("onTapTrack()");
      //this.commandPanel.setTrack(t);
      //this.commandPanel.open(MusicStyles.Popular);
      this.commandPanel.open(MusicStyles.Popular, w, t, null, async (r) => await this.executeCommand(r));
   }
   onTapAlbum(w: Work) {
      //this.commandPanel.setWork(w);
      //this.commandPanel.open(MusicStyles.Popular);
      this.commandPanel.open(MusicStyles.Popular, w, null, null, async (r) => await this.executeCommand(r));
   }
   async onRightClick(e: Event, w: Work) {
      e.preventDefault();
      if (!this.isTouchDevice()) {
         //this.commandPanel.setWork(w);
         //this.commandPanel.open(MusicStyles.Popular);
         this.commandPanel.open(MusicStyles.Popular, w, null, null, async (r) => await this.executeCommand(r));
      }
      return false;
   }
   getArtistStats(a: Artist) {
      let parts: string[] = [];
      if (a.workCount > 0) {
         if (a.workCount > 1) {
            parts.push(`${a.workCount} albums`);
         } else {
            parts.push(`1 album`);
         }
      }
      if (a.singlesCount > 0) {
         if (a.singlesCount > 1) {
            parts.push(`${a.singlesCount} singles`);
         } else {
            parts.push(`1 single`);
         }
      }
      return parts.join(", ");
   }
   getArtist(w: Work): Artist {
      let artist = this.artistsInCurrentSearch.find(x => x.id === w.artistId)!;
      return artist;
   }
   getStackAngle(index: number, total: number) {
      let i = total - index - 1;
      i = i > 23 ? 23 : i;
      return `rotate-${i}`;
   }
   async toggleAlbumTracks(album: Work) {
      album.showTracks = !album.showTracks;
      if (album.showTracks === true && (!album.tracks || album.tracks.length === 0)) {
         album.tracks = await this.library.getAllTracks(album);
      }
   }
   getMatchedArtists(): Artist[] {

      let r = this.artistsInCurrentSearch.filter(x => x.isMatchedBySearch);
      //console.log(`getMatchedArtists: count is ${r.length}`);
      return r;
   }
   protected addArtistToDefaultView(a: Artist) {
      sortedInsert(this.allArtists, a, (l, r) => {
         return l.name.localeCompare(r.name);
      });
   }
   protected async onSearch() {
      this.searchFoundNothing = false;
      let r = await this.library.search<PopularResults>(this.parameterService.getCurrentStyle(), this.searchText);
      let prefixMode = r.prefixMode;
      this.artistsInCurrentSearch = [];
      //this.matchedArtists = [];
      this.matchedWorks = [];
      this.matchedTracks = [];
      if (r.results.length > 0) {
         this.processSearchResults2(r, prefixMode);
      } else {
         console.log('nothing found');
         this.searchFoundNothing = true;
      }
   }
   private processSearchResults1(r: PopularResults, prefixMode: boolean) {
      r.results.forEach(async (pr) => { //NB; foreach does not wait for all async stuff to finish
         let artist = await this.library.getArtist(this.currentStyle, pr.artist.key);
         artist.isMatchedBySearch = pr.artistIsMatched;
         artist.highlightSearch(this.searchText, artist.name, prefixMode);
         this.addArtist(artist);
         if (pr.artistIsMatched) {
            artist.works = await this.library.getAllWorks(artist);
            for (let w of artist.works) {
               this.addWork(w);
            }
         } else {
            // artist was not matched so it must have been one or more works and/or
            // one or more tracks
            artist.works = await this.getWorkResults(pr.works, prefixMode);
         }
      })
   }
   private processSearchResults2(r: PopularResults, prefixMode: boolean) {
      r.results.forEach(async (pr) => { //NB; foreach does not wait for all async stuff to finish
         let artist = await this.library.getArtist(this.currentStyle, pr.artist.key);
         artist.highlightSearch(this.searchText, artist.name, prefixMode);
         this.addArtist(artist);
         if (pr.artistIsMatched) {
            artist.isMatchedBySearch = true;
            artist.works = await this.library.getAllWorks(artist, true);
            for (let w of artist.works) {
               this.addWork(w);
            }
         } else {
            // artist was not matched so it must have been one or more works and/or
            // one or more tracks
            artist.isMatchedBySearch = false;
            artist.works = await this.getWorkResults(pr.works, prefixMode);
         }
      })
   }
   private async getWorkResults(list: WorkResult[], prefixMode: boolean): Promise<Work[]> {
      let works: Work[] = [];
      for (let wr of list) {
         let w = await this.library.getWork(wr.work.key);
         if (wr.workIsMatched) {
            this.addWork(w);
            w.highlightSearch(this.searchText, w.name, prefixMode);
         } else {
            for (let tr of wr.tracks) {
               let t = await this.library.getTrack(tr.track.key);
               t.highlightSearch(this.searchText, t.title, prefixMode);
               t.work = w;
               this.addTrack(t);
            }
         }
         works.push(w);
      }
      return works;
   }
   //private sortedInsert<T>(array: T[], item: T, compare: (l: T, r: T) => number) {
   //    let inserted = false;
   //    for (let i = 0, len = array.length; i < len; ++i) {
   //        if (compare(item, array[i]) < 0) {
   //            array.splice(i, 0, item);
   //            inserted = true;
   //            break;
   //        }
   //    }
   //    if (!inserted) {
   //        array.push(item);
   //        inserted = true;
   //    } 
   //    //console.log(`sortedInsert: ${JSON.stringify(item)}`);
   //}
   private addArtist(artist: Artist) {
      sortedInsert(this.artistsInCurrentSearch, artist, (l, r) => {
         //console.log(`${l.name} with ${r.name}`);
         return l.name.localeCompare(r.name);
      });
   }
   private addWork(work: Work) {
      sortedInsert(this.matchedWorks, work, (l, r) => {
         return l.name.localeCompare(r.name);
      });
   }
   private addTrack(track: Track) {
      sortedInsert(this.matchedTracks, track, (l, r) => {
         return l.title.localeCompare(r.title);
      });
   }
   //private async executeCommand(cmd: CommandPanelResult) {
   //   switch (cmd.selectedCommand) {
   //      case SelectedCommand.Cancel:
   //         break;
   //      case SelectedCommand.Play:
   //         await this.playMusic(cmd.entity);
   //         break;
   //      case SelectedCommand.Queue:
   //         await this.queueMusic(cmd.entity);
   //         break;
   //      //case SelectedCommand.Details:
   //      //    break;
   //      case SelectedCommand.TagEditor:
   //         if (cmd.targetEntity === TargetEntity.Work) {
   //            //this.popularTagEditor.initialise(cmd.entity as Work);
   //            this.popularTagEditor.open(cmd.entity as Work, (changesMade) => {
   //            });
   //         }
   //         break;
   //      case SelectedCommand.Reset:
   //         switch (cmd.targetEntity) {
   //            case TargetEntity.Work:
   //               await this.library.resetWork((cmd.entity as Work).id);
   //               break;
   //         }
   //         break;
   //   }
   //}
}
