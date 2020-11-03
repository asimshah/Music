import { Component, OnInit, ElementRef } from '@angular/core';
import { BaseCatalogComponent } from '../base-catalog.component';
import { LoggingService } from '../../shared/logging.service';
import { PlayerService } from '../../shared/player.service';
import { ParameterService } from '../../shared/parameter.service';
import { LibraryService } from '../../shared/library.service';

@Component({
   selector: 'app-hindi-films-catalog',
   templateUrl: './hindi-films-catalog.component.html',
   styleUrls: ['./hindi-films-catalog.component.scss']
})
export class HindiFilmsCatalogComponent extends BaseCatalogComponent implements OnInit {
      constructor(elementRef: ElementRef, library: LibraryService,
         ps: ParameterService, playerService: PlayerService, log: LoggingService) {
         super(elementRef, library, ps, playerService, log);
      }

   ngOnInit() {
   }
   protected onSearch() {
      throw new Error("Method not implemented.");
   }

}
