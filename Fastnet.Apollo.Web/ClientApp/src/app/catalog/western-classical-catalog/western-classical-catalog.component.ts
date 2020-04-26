import { Component, ViewChild, ElementRef } from '@angular/core';
import { BaseCatalogComponent } from '../base-catalog.component';
import { SearchKey, PerformanceResult } from '../../shared/common.types';
import { LibraryService } from '../../shared/library.service';
import { Artist, Composition, Performance, Movement, MusicFile, Track, Work } from "../../shared/catalog.types";
import { PlayerService } from '../../shared/player.service';
import { LoggingService } from '../../shared/logging.service';
import { ParameterService } from '../../shared/parameter.service';
import { DomSanitizer } from '@angular/platform-browser';
import { MusicStyles } from '../../shared/common.enums';
import { WesternClassicalTagEditorComponent } from './western-classical-tag-editor/western-classical-tag-editor.component';
import { CommandPanelResult, SelectedCommand, TargetEntity } from '../command-panel/command-panel.component';
import { sortedInsert } from '../../../fastnet/core/common.functions';
import { MessageService } from '../../shared/message.service';



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
   @ViewChild(WesternClassicalTagEditorComponent, { static: false }) westernClassicalTagEditor: WesternClassicalTagEditorComponent;

   constructor(elementRef: ElementRef, library: LibraryService,
      messageService: MessageService,
      ps: ParameterService, sanitizer: DomSanitizer,
      playerService: PlayerService, log: LoggingService) {
      super(elementRef, library, messageService, ps, sanitizer, playerService, log);
   }
   async onTapComposition(c: Composition) {
      if (c.performances == null) {
         // not yet loaded
         await this.loadPerformances(c);
      }
      c.showPerformances = !c.showPerformances;
   }
   onTapPerformance(c: Composition, p: Performance) {
      if (this.isTouchDevice() && p.movements.length > 1) {
         this.commandPanel.open(MusicStyles.WesternClassical, c, p, null, async (r) => await this.executeCommand(r));
      }
   }
   onTapMovement(c: Composition, p: Performance, t: Track) {
      this.log.information("onTapTrack()");
      this.commandPanel.open(MusicStyles.WesternClassical, c, p, t, async (r) => await this.executeCommand(r));
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
   async onRightClick(e: Event, c: Composition, p: Performance) {
      e.preventDefault();
      if (!this.isTouchDevice()) {
         this.commandPanel.open(MusicStyles.WesternClassical, c, p, null, async (r) => await this.executeCommand(r));
      }
      return false;
   }
   toggleShowMovements(performance: Performance) {
      performance.showMovements = !performance.showMovements;
   }
   onPerformanceMouse(p: Performance, val: boolean) {
      p.isHighLit = val;
   }
   onMovementMouse(m: Movement, f: MusicFile, val: boolean) {
      m.isHighLit = val;
      f.isHighLit = val;
   }
   getArtistStats(a: Artist) {
      let parts: string[] = [];
      if (a.compositionCount > 0) {
         if (a.compositionCount > 1) {
            parts.push(`${a.compositionCount} compositions`);
         } else {
            parts.push(`1 composition`);
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
   getComposerDetails(a: Artist): string {
      let p_part = a.performanceCount === 1 ? `${a.performanceCount} performance` : `${a.performanceCount} performances`;
      let c_part = a.compositionCount === 1 ? `of ${a.compositionCount} composition` : `of ${a.compositionCount} compositions`;
      let t = `${p_part} ${c_part}`;
      return t;
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
   //public async editorCallback(item: Performance) {
   //    this.log.information(`editorCallback()!!`);
   //    await this.westernClassicalTagEditor.open(item);

   //}
   protected addArtistToDefaultView(a: Artist) {
      sortedInsert(this.allArtists, a, (l, r) => {
         return l.lastname.localeCompare(r.lastname);
      });
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
         let artist = await this.library.getArtist(wcr.composer.key);
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
                  composition.performances = await this.library.getAllPerformances(composition.id, true);
                  for (let p of composition.performances) {
                     p.highlightSearch(this.searchText, p.performers, prefixMode);
                  }
               } else {
                  composition.performances = [];//await this.getPerformanceResults(cr.performances);
                  let performances: Performance[] = [];
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
      c.performances = await this.library.getAllPerformances(c.id, true);
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
   private async getPerformanceResults(list: PerformanceResult[]): Promise<Performance[]> {
      let performances: Performance[] = [];
      for (let pr of list) {
         let p = await this.library.getPerformance(pr.performance.key);
         performances.push(p);
      }
      return performances;
   }
   private async getCompositionResults(list: CompositionResult[]): Promise<Composition[]> {
      let compositions: Composition[] = [];
      for (let cr of list) {
         let c = await this.library.getComposition(cr.composition.key);
         compositions.push(c);
      }
      return compositions;
   }
   private async executeCommand(cmd: CommandPanelResult) {
      switch (cmd.selectedCommand) {
         case SelectedCommand.Cancel:
            break;
         case SelectedCommand.Play:
            await this.playMusic(cmd.entity);
            break;
         case SelectedCommand.Queue:
            await this.queueMusic(cmd.entity);
            break;
         case SelectedCommand.TagEditor:
            if (cmd.targetEntity === TargetEntity.Performance) {
               // await this.westernClassicalTagEditor.initialise(cmd.entity as Performance);
               this.westernClassicalTagEditor.open(cmd.entity as Performance, async (changesMade) => {
                  if (changesMade) {
                     await this.onSearch();
                  }
               });
               //await this.westernClassicalTagEditor.open(cmd.entity as Performance, async (changesMade: boolean) => {
               //    if (changesMade) {
               //        await this.onSearch();
               //    }
               //});
            }
            break;
         case SelectedCommand.Reset:
            switch (cmd.targetEntity) {
               case TargetEntity.Work:
                  await this.library.resetWork((cmd.entity as Work).id);
                  break;
               case TargetEntity.Performance:
                  await this.library.resetPerformance((cmd.entity as Performance).id);
                  break;
            }
            break;
      }
   }

   private addComposer(composer: Artist) {
      sortedInsert(this.composers, composer, (l, r) => {
         //console.log(`${l.name} with ${r.name}`);
         return l.name.localeCompare(r.name);
      });
   }
}
