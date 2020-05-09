import { Component, OnInit, ElementRef } from '@angular/core';
import { BaseCatalogComponent } from '../base-catalog.component';
import { LibraryService } from '../../shared/library.service';
import { MessageService } from '../../shared/message.service';
import { ParameterService } from '../../shared/parameter.service';
import { DomSanitizer } from '@angular/platform-browser';
import { PlayerService } from '../../shared/player.service';
import { LoggingService } from '../../shared/logging.service';
import { Artist, Raga, ArtistSet, Performance, Track } from '../../shared/catalog.types';
import { sortedInsert } from '../../../fastnet/core/common.functions';
import { SearchKey, PerformanceResult } from '../../shared/common.types';
import { MusicStyles } from '../../shared/common.enums';

class IndianClassicalRagaResult {
   raga: SearchKey;
   ragaIsMatched: boolean;
   performances: PerformanceResult[];
}
class IndianClassicalResult {
   artists: SearchKey[];
   artistIsMatched: boolean;
   ragas: IndianClassicalRagaResult[];
}
class IndianClassicalResults {
   prefixMode: boolean;
   results: IndianClassicalResult[];
}

@Component({
   selector: 'app-indian-classical-catalog',
   templateUrl: './indian-classical-catalog.component.html',
   styleUrls: ['./indian-classical-catalog.component.scss']
})
export class IndianClassicalCatalogComponent extends BaseCatalogComponent {
   searchFoundNothing = false;
   artistSets: ArtistSet[] = [];
   constructor(elementRef: ElementRef, library: LibraryService,
      messageService: MessageService,
      ps: ParameterService, sanitizer: DomSanitizer,
      playerService: PlayerService, log: LoggingService) {
      super(elementRef, library, messageService, ps, sanitizer, playerService, log);
   }
   getArtistStats(a: Artist) {
      let parts: string[] = [];
      if (a.ragaCount > 0) {
         if (a.ragaCount > 1) {
            parts.push(`${a.ragaCount} ragas`);
         } else {
            parts.push(`1 raga`);
         }
      }
      if (a.performanceCount > 0) {
         if (a.performanceCount > 1) {
            parts.push(`${a.performanceCount} performances`);
         } else {
            parts.push(`1 performance`);
         }
      }
      return parts.join(", ");
   }
   async onTapRaga(set: ArtistSet, r: Raga) {
      if (r.performances == null) {
         // not yet loaded
         await this.loadPerformances(set, r);
      }
      r.showPerformances = !r.showPerformances;
   }
   getMusicSummary(set: ArtistSet): string {
      let r_part = set.ragaCount == 1 ? `${set.ragaCount} raga` : `${set.ragaCount} ragas`;
      let p_part = set.performanceCount === 1 ? `${set.performanceCount} performance` : `${set.performanceCount} performances`;
      return `${r_part} in ${p_part}`;
   }
   toggleShowMusic(set: ArtistSet) {
      set.showMusic = !set.showMusic;
   }
   toggleShowMovements(performance: Performance) {
      performance.showMovements = !performance.showMovements;
   }
   onTapMovement(r: Raga, p: Performance, t: Track) {
      //this.log.information("onTapTrack()");
      this.commandPanel.open(MusicStyles.IndianClassical, r, p, t, async (r) => await this.executeCommand(r));
   }
   onTapPerformance(r: Raga, p: Performance) {
      //if (this.isTouchDevice() && p.movements.length > 1) {
      //   this.commandPanel.open(MusicStyles.IndianClassical, r, p, null, async (r) => await this.executeCommand(r));
      //}
      if (this.isTouchDevice()) {
         if (p.movements.length > 1) {
            this.commandPanel.open(MusicStyles.IndianClassical, r, p, null, async (r) => await this.executeCommand(r));
         } else {
            this.commandPanel.open(MusicStyles.IndianClassical, r, p, p.movements[0], async (r) => await this.executeCommand(r));
         }
      }
   }
   async onRightClick(e: Event, r:Raga, p: Performance) {
      e.preventDefault();
      if (!this.isTouchDevice()) {
         this.commandPanel.open(MusicStyles.IndianClassical, r, p, null, async (r) => await this.executeCommand(r));
      }
      return false;
   }
   protected async onSearch() {
      this.artistSets = [];
      this.searchFoundNothing = false;
      let r = await this.library.search<IndianClassicalResults>(this.parameterService.getCurrentStyle(), this.searchText);
      this.prefixMode = r.prefixMode;
      if (r.results.length > 0) {
         this.processSearchResults(r, this.prefixMode);
      } else {
         console.log('nothing found');
         this.searchFoundNothing = true;
      }
   }
   protected addArtistToDefaultView(a: Artist) {
      sortedInsert(this.allArtists, a, (l, r) => {
         return l.lastname.localeCompare(r.lastname);
      });
   }

   private async processSearchResults(r: IndianClassicalResults, prefixMode: boolean) {
      //let text = JSON.stringify(r, null, 2);
      //console.log(text);
      r.results.forEach(async (icr) => {
         let set = await this.getArtists(icr.artists);
         //console.log(JSON.stringify(set, null, 2));
         //artist.highlightSearch(this.searchText, artist.name, prefixMode);
         this.addArtists(set);
         if (icr.artistIsMatched) {
            // get all the ragas for this artist set
            set.ragas = await this.library.getAllRagas(set);
         } else {
            for (let r of icr.ragas) {
               set.ragas = [];
               let raga = await this.library.getRaga(r.raga.key);
               set.ragas.push(raga);
               if (r.ragaIsMatched) {

               } else {
                  for (let p of r.performances) {
                     if (p.performanceIsMatched) {

                     } else {
                        // here if one or more movements are matched
                        // show all or only matched?
                     }
                  }
               }
            }
         }
      });
   }

   private async loadPerformances(set: ArtistSet, r: Raga) {
      r.performances = await this.library.getAllRagaPerformances(this.parameterService.getCurrentStyle(), set, r)
      for (let p of r.performances) {
         p.highlightSearch(this.searchText, p.performers, false);
      }
   }
   private async getArtists(artistKeys: SearchKey[]) {
      let set = await this.library.getArtists(this.currentStyle, artistKeys.map(k => k.key));
      for (let artistId of set.artistIds) {
         let artist = await this.library.getArtist(this.currentStyle, artistId);
         set.artists.push(artist);
      }
      return set;
   }
   private addArtists(artistSet: ArtistSet) {
      sortedInsert(this.artistSets, artistSet, (l, r) => {
         return l.artistIds.sort().toString().localeCompare(r.artistIds.sort().toString())
      });
   }
}
