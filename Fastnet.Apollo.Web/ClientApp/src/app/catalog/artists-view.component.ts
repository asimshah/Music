import { Component, OnInit, OnDestroy, EventEmitter, Output } from '@angular/core';
import { Artist, ArtistType } from '../shared/catalog.types';
import { LibraryService } from '../shared/library.service';
import { ParameterService } from '../shared/parameter.service';
import { Subscription } from 'rxjs';
import { sortedInsert } from '../../fastnet/core/common.functions';
import { Style } from '../shared/common.types';
import { MusicStyles } from '../shared/common.enums';
import { MessageService } from '../shared/message.service';

@Component({
   selector: 'artists-view',
   templateUrl: './artists-view.component.html',
   styleUrls: ['./artists-view.component.scss']
})
export class ArtistsViewComponent implements OnInit, OnDestroy {
   public allArtists: Artist[] = [];
   private currentStyle: Style;
   @Output() artistSelected = new EventEmitter<Artist>();
   private subscriptions: Subscription[] = [];
   constructor(private parameterService: ParameterService,
      private messageService: MessageService,
      private library: LibraryService) { }

   ngOnInit() {
      let s = this.parameterService.ready$.subscribe(async () => { await this.init();});
      this.subscriptions.push(s);
      this.subscriptions.push(this.messageService.newOrModifiedArtist.subscribe(async (id) => await this.onNewOrModifiedArtist(id)));
      this.subscriptions.push(this.messageService.deletedArtist.subscribe((id) => this.onDeletedArtist(id)));
   }
   ngOnDestroy() {
      for (let s of this.subscriptions) {
         s.unsubscribe();
      }
      this.subscriptions = [];
   }
   onArtistClick(artist: Artist) {
      this.artistSelected.next(artist);
   }
   getArtistStats(artist: Artist) {
      switch (this.currentStyle.id) {
         case MusicStyles.Popular:
            return this.getPopularArtistStats(artist);
         case MusicStyles.WesternClassical:
            return this.getWesternClassicalArtistStats(artist);
         case MusicStyles.IndianClassical:
            return this.getIndianClassicalArtistStats(artist);
      }
   }
   private getPopularArtistStats(a: Artist) {
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
   private getWesternClassicalArtistStats(a: Artist) {
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
   getIndianClassicalArtistStats(a: Artist) {
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
   private async init() {
      this.currentStyle = this.parameterService.getCurrentStyle();
      this.allArtists = await this.library.getAllArtistsFull(this.currentStyle);
   }
   private onDeletedArtist(id: number) {
      let index = this.allArtists.findIndex((v, i) => {
         return v.id === id;// ? i : -1;
      });
      if (index != -1) {
         this.allArtists.splice(index, 1);
      }
   }
   private async onNewOrModifiedArtist(id: number) {
      let a = await this.library.getArtist(this.currentStyle, id);
      if (a.artistType !== ArtistType.Various && a.styles.findIndex((x) => this.currentStyle.id === x) > -1) {
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
   private addArtistToDefaultView(a: Artist) {
      sortedInsert(this.allArtists, a, (l, r) => {
         return l.name.localeCompare(r.name);
      });
   }
}
