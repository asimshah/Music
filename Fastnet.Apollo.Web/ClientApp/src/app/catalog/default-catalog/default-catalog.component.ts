import { Component, OnInit, ElementRef } from '@angular/core';
import { BaseCatalogComponent } from '../base-catalog.component';
import { LibraryService } from '../../shared/library.service';
import { PlayerService } from '../../shared/player.service';
import { LoggingService } from '../../shared/logging.service';
import { ParameterService } from '../../shared/parameter.service';

@Component({
   selector: 'app-default-catalog',
   templateUrl: './default-catalog.component.html',
   styleUrls: ['./default-catalog.component.scss']
})
export class DefaultCatalogComponent extends BaseCatalogComponent implements OnInit {

   constructor(elementRef: ElementRef, library: LibraryService,
      playerService: PlayerService, ps: ParameterService,
      log: LoggingService) {
      super(elementRef, library, ps, playerService, log);
   }

   protected onSearch() {

   }
}
