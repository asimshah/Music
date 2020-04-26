import { Component, OnInit, ElementRef } from '@angular/core';
import { BaseCatalogComponent } from '../base-catalog.component';
import { LibraryService } from '../../shared/library.service';
import { MessageService } from '../../shared/message.service';
import { ParameterService } from '../../shared/parameter.service';
import { DomSanitizer } from '@angular/platform-browser';
import { PlayerService } from '../../shared/player.service';
import { LoggingService } from '../../shared/logging.service';
import { Artist, Raga } from '../../shared/catalog.types';
import { sortedInsert } from '../../../fastnet/core/common.functions';
import { SearchKey, PerformanceResult } from '../../shared/common.types';

class RagaResult {
   raga: SearchKey;
   ragaIsMatched: boolean;
   performances: PerformanceResult[];
}
class IndianClassicalResult {
   artist: SearchKey;
   artistIsMatched: boolean;
   ragas: RagaResult[];
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
   async onTapRaga(r: Raga) {
      if (r.performances == null) {
         // not yet loaded
         await this.loadPerformances(r);
      }
      r.showPerformances = !r.showPerformances;
   }
   protected async onSearch() {
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
   }
   private async loadPerformances(r: Raga) {
      //c.performances = await this.library.getAllPerformances(c.id, true);
      //for (let p of c.performances) {
      //   p.highlightSearch(this.searchText, p.performers, false);
      //}
   }
}
