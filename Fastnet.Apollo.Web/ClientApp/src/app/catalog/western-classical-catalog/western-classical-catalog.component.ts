import { Component, ElementRef } from '@angular/core';
import { BaseCatalogComponent } from '../base-catalog.component';
import { SearchKey, PerformanceResult } from '../../shared/common.types';
import { LibraryService } from '../../shared/library.service';
import { Artist, Composition, Performance, Track } from "../../shared/catalog.types";
import { PlayerService } from '../../shared/player.service';
import { LoggingService } from '../../shared/logging.service';
import { ParameterService } from '../../shared/parameter.service';
import { sortedInsert } from '../../../fastnet/core/common.functions';

class CompositionResult {
   composition: SearchKey;
   compositionIsMatched: boolean;
   performances: PerformanceResult[];
}
class WesternClassicalResult {
   composer: SearchKey;
   composerIsMatched: boolean;
   compositions: CompositionResult[];
}
class WesternClassicalResults {
   prefixMode: boolean;
   results: WesternClassicalResult[];
}

@Component({
   selector: 'app-western-classical-catalog',
   templateUrl: './western-classical-catalog.component.html',
   styleUrls: [
      './western-classical-catalog.component.scss'
   ]
})
export class WesternClassicalCatalogComponent extends BaseCatalogComponent {
   composers: Artist[] = [];
   searchFoundNothing = false;
   constructor(elementRef: ElementRef, library: LibraryService,
      ps: ParameterService, playerService: PlayerService, log: LoggingService) {
      super(elementRef, library,  ps, playerService, log);
   }
   async onTapComposition(c: Composition) {
      // also called by mouse click
      if (c.performances == null) {
         // not yet loaded
         await this.loadPerformances(c);
      }
      c.showPerformances = !c.showPerformances;
   }
   onTapPerformance(p: Performance) {
      if (this.isTouchDevice()) {
         this.commandPanel.open2(p,
            async (r) => await this.executeCommand(r));
      }
   }
   onTapMovement(c: Composition, p: Performance, t: Track) {
      this.commandPanel.open2(t, async (r) =>  await this.executeCommand(r));
   }

   async onRightClick(e: Event, c: Composition, p: Performance) {
      e.preventDefault();
      if (!this.isTouchDevice()) {
         this.commandPanel.open2(p,
            async (r) => await this.executeCommand(r));
      }
      return false;
   }
   toggleShowMovements(e: Event, performance: Performance) {
      performance.showMovements = !performance.showMovements;
   }

   getComposerDetails(a: Artist): string {
      let p_part = a.performanceCount === 1 ? `${a.performanceCount} performance` : `${a.performanceCount} performances`;
      let c_part = a.compositionCount === 1 ? `of ${a.compositionCount} composition` : `of ${a.compositionCount} compositions`;
      let t = `${p_part} ${c_part}`;
      return t;
   }

   protected async onSearch() {
      this.searchFoundNothing = false;
      let r = await this.library.search<WesternClassicalResults>(this.parameterService.getCurrentStyle(), this.searchText);
      this.prefixMode = r.prefixMode;
      this.composers = [];
      if (r.results.length > 0) {
         this.processSearchResults(r, this.prefixMode);
      } else {
         console.log('nothing found');
         this.searchFoundNothing = true;
      }
   }
   private async processSearchResults(r: WesternClassicalResults, prefixMode: boolean) {
      r.results.forEach(async (wcr) => { //NB; foreach does not wait for all async stuff to finish
         let artist = await this.library.getArtist(this.currentStyle, wcr.composer.key);
         artist.highlightSearch(this.searchText, artist.name, prefixMode);
         this.addComposer(artist);
         if (wcr.composerIsMatched) {
            await this.processMatchedComposer(artist, prefixMode);
         } else {
            artist.compositions = await this.getCompositionResults(wcr.compositions);
            for (let cr of wcr.compositions) {
               // if composition were matched we need all the performances
               // else we need the performance results
               let composition = artist.compositions.find(x => x.id === cr.composition.key)!;
               composition.highlightSearch(this.searchText, composition.name, prefixMode);
               if (cr.compositionIsMatched) {
                  composition.performances = await this.library.getAllCompositionPerformances(composition.id, true);
                  for (let p of composition.performances) {
                     p.highlightSearch(this.searchText, p.performers, prefixMode);
                  }
               } else {
                  composition.performances = [];
                  for (let pr of cr.performances) {
                     let p = await this.library.getPerformance(pr.performance.key, true);
                     p.highlightSearch(this.searchText, p.performers, prefixMode);
                     composition.performances.push(p);
                  }
               }
            }
         }
      });
   }
   private async loadPerformances(c: Composition) {
      c.performances = await this.library.getAllCompositionPerformances(c.id, true);
      for (let p of c.performances) {
         p.highlightSearch(this.searchText, p.performers, false);
      }
   }
   private async processMatchedComposer(composer: Artist, prefixMode: boolean) {
      // when the composer is matched, we get all compositions and all the related content - performances, movements, music files.
      composer.compositions = await this.library.getAllCompositions(composer.id, false);
      for (let c of composer.compositions) {
         c.highlightSearch(this.searchText, c.name, prefixMode);
      }
   }

   private async getCompositionResults(list: CompositionResult[]): Promise<Composition[]> {
      let compositions: Composition[] = [];
      for (let cr of list) {
         let c = await this.library.getComposition(cr.composition.key);
         compositions.push(c);
      }
      return compositions;
   }

   private addComposer(composer: Artist) {
      sortedInsert(this.composers, composer, (l, r) => {
         return l.name.localeCompare(r.name);
      });
   }
}
