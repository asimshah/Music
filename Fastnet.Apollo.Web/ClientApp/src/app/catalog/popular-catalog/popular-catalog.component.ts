import { Component,  ElementRef } from '@angular/core';
import { BaseCatalogComponent } from '../base-catalog.component';
import { LibraryService } from '../../shared/library.service';
import { Artist, Work, Track, MusicFile, OpusType } from "../../shared/catalog.types";
import { SearchKey } from '../../shared/common.types';
import { PlayerService } from '../../shared/player.service';
import { LoggingService } from '../../shared/logging.service';
import { ParameterService } from '../../shared/parameter.service';
import { DomSanitizer } from '@angular/platform-browser';
//import { MusicStyles } from '../../shared/common.enums';
//import { CommandPanelResult, SelectedCommand, TargetEntity } from '../command-panel/command-panel.component';
//import { MessageService } from '../../shared/message.service';
import { sortedInsert } from '../../../fastnet/core/common.functions';
//import { PopularTagEditorComponent } from './popular-tag-editor/popular-tag-editor.component';

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

//enum CatalogMode {
//   Search
//}

@Component({
   selector: 'app-popular-catalog',
   templateUrl: './popular-catalog.component.html',
   styleUrls: [
      './popular-catalog.component.scss'
   ]
})
export class PopularCatalogComponent extends BaseCatalogComponent {
   OpusType = OpusType;
   //@ViewChild(PopularTagEditorComponent, { static: false }) popularTagEditor: PopularTagEditorComponent;
   private artistsInCurrentSearch: Artist[] = [];
   matchedWorks: Work[] = []; // i.e. where work name was matched by search (but not the artist name)
   matchedTracks: Track[] = []; // i.e. where track name was matched but not work or artist
   searchFoundNothing = false;
   constructor(elementRef: ElementRef, libraryService: LibraryService,
      //messageService: MessageService,
      ps: ParameterService,
      sanitizer: DomSanitizer, playerService: PlayerService, log: LoggingService) {
      super(elementRef, libraryService, ps, sanitizer, playerService, log);
      //console.log('PopularCatalogComponent ctor()');
   }

   async onPlayAlbum(album: Work) {
      await this.playMusic(album);
   }
   async onQueueAlbum(album: Work) {
      await this.queueMusic(album);
   }

   onAlbumMouse(w: Work, val: boolean) {
      w.isHighLit = val;
   }

   onTapAlbum(w: Work) {
      this.commandPanel.open2(w,
         //w.coverArtUrl, w.artistName, w.name,
         async (r) => await this.executeCommand(r));

   }
   async onRightClick(e: Event, w: Work) {
      e.preventDefault();
      if (!this.isTouchDevice()) {
         this.commandPanel.open2(w,
            //w.coverArtUrl, w.artistName, w.name,
            async (r) => await this.executeCommand(r));
      }
      return false;
   }
   //getArtistStats(a: Artist) {
   //   let parts: string[] = [];
   //   if (a.workCount > 0) {
   //      if (a.workCount > 1) {
   //         parts.push(`${a.workCount} albums`);
   //      } else {
   //         parts.push(`1 album`);
   //      }
   //   }
   //   if (a.singlesCount > 0) {
   //      if (a.singlesCount > 1) {
   //         parts.push(`${a.singlesCount} singles`);
   //      } else {
   //         parts.push(`1 single`);
   //      }
   //   }
   //   return parts.join(", ");
   //}

   //getStackAngle(index: number, total: number) {
   //   let i = total - index - 1;
   //   i = i > 23 ? 23 : i;
   //   return `rotate-${i}`;
   //}
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

   protected async onSearch() {
      this.searchFoundNothing = false;
      let r = await this.library.search<PopularResults>(this.parameterService.getCurrentStyle(), this.searchText);
      let prefixMode = r.prefixMode;
      this.artistsInCurrentSearch = [];
      this.matchedWorks = [];
      this.matchedTracks = [];
      if (r.results.length > 0) {
         this.processSearchResults2(r, prefixMode);
      } else {
         console.log('nothing found');
         this.searchFoundNothing = true;
      }
   }

   private processSearchResults2(r: PopularResults, prefixMode: boolean) {
      r.results.forEach(async (pr) => { //NB; foreach does not wait for all async stuff to finish
         let artist = await this.library.getArtist(this.currentStyle, pr.artist.key);
         //artist.highlightSearch(this.searchText, artist.name, prefixMode);
         this.addArtist(artist);
         if (pr.artistIsMatched) {
            artist.isMatchedBySearch = true;
            artist.works = await this.library.getAllWorks(this.currentStyle, artist, true);
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
            //w.highlightSearch(this.searchText, w.name, prefixMode);
         } else {
            for (let tr of wr.tracks) {
               let t = await this.library.getTrack(tr.track.key);
               //t.highlightSearch(this.searchText, t.title, prefixMode);
               t.work = w;
               this.addTrack(t);
            }
         }
         works.push(w);
      }
      return works;
   }

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

}
