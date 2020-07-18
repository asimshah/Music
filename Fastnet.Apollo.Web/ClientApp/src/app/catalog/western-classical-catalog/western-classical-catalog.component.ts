import { Component, ViewChild, ElementRef } from '@angular/core';
import { BaseCatalogComponent } from '../base-catalog.component';
import { SearchKey, PerformanceResult } from '../../shared/common.types';
import { LibraryService } from '../../shared/library.service';
import { Artist, Composition, Performance, Movement, MusicFile, Track, Work } from "../../shared/catalog.types";
import { PlayerService } from '../../shared/player.service';
import { LoggingService } from '../../shared/logging.service';
import { ParameterService } from '../../shared/parameter.service';
import { DomSanitizer } from '@angular/platform-browser';
//import { MusicStyles } from '../../shared/common.enums';
//import { WesternClassicalTagEditorComponent } from './western-classical-tag-editor/western-classical-tag-editor.component';
//import { CommandPanelResult, SelectedCommand, TargetEntity } from '../command-panel/command-panel.component';
import { sortedInsert } from '../../../fastnet/core/common.functions';
//import { MessageService } from '../../shared/message.service';



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
   //private guardShowMovementsClosure = false;
   constructor(elementRef: ElementRef, library: LibraryService,
      //messageService: MessageService,
      ps: ParameterService, sanitizer: DomSanitizer,
      playerService: PlayerService, log: LoggingService) {
      super(elementRef, library,  ps, sanitizer, playerService, log);
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
         //if (p.movements.length > 1) {
         //   this.commandPanel.open2(p,
         //      async (r) => await this.executeCommand(r));
         //} else {
         //   this.commandPanel.open2(p.movements[0],
         //      async (r) => await this.executeCommand(r));
         //}
      }
   }
   onTapMovement(c: Composition, p: Performance, t: Track) {
      this.commandPanel.open2(t, async (r) =>  await this.executeCommand(r));
   }

   async onRightClick(e: Event, c: Composition, p: Performance) {
      e.preventDefault();
      if (!this.isTouchDevice()) {
         this.commandPanel.open2(p,
            //c, p, null, null,
            async (r) => await this.executeCommand(r));
      }
      return false;
   }
   toggleShowMovements(e: Event, performance: Performance) {
      performance.showMovements = !performance.showMovements;
   }


   //getArtistStats(a: Artist) {
   //   let parts: string[] = [];
   //   if (a.compositionCount > 0) {
   //      if (a.compositionCount > 1) {
   //         parts.push(`${a.compositionCount} compositions`);
   //      } else {
   //         parts.push(`1 composition`);
   //      }
   //   }
   //   if (a.performanceCount > 0) {
   //      if (a.performanceCount > 1) {
   //         parts.push(`${a.performanceCount} performances`);
   //      } else {
   //         parts.push(`1 performance`);
   //      }
   //   }
   //   return parts.join(", ");
   //}
   getComposerDetails(a: Artist): string {
      let p_part = a.performanceCount === 1 ? `${a.performanceCount} performance` : `${a.performanceCount} performances`;
      let c_part = a.compositionCount === 1 ? `of ${a.compositionCount} composition` : `of ${a.compositionCount} compositions`;
      let t = `${p_part} ${c_part}`;
      return t;
   }

   //public async editorCallback(item: Performance) {
   //    this.log.information(`editorCallback()!!`);
   //    await this.westernClassicalTagEditor.open(item);

   //}
   //protected addArtistToDefaultView(a: Artist) {
   //   sortedInsert(this.allArtists, a, (l, r) => {
   //      return l.lastname.localeCompare(r.lastname);
   //   });
   //}
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
                  //let performances: Performance[] = [];
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
   //private async getPerformanceResults(list: PerformanceResult[]): Promise<Performance[]> {
   //   let performances: Performance[] = [];
   //   for (let pr of list) {
   //      let p = await this.library.getPerformance(pr.performance.key);
   //      performances.push(p);
   //   }
   //   return performances;
   //}
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
         //console.log(`${l.name} with ${r.name}`);
         return l.name.localeCompare(r.name);
      });
   }
}
