
import { Component, OnInit, ElementRef } from '@angular/core';
import { BaseCatalogComponent } from '../base-catalog.component';
import { LibraryService } from '../../shared/library.service';
import { PlayerService } from '../../shared/player.service';
import { LoggingService } from '../../shared/logging.service';
import { ParameterService } from '../../shared/parameter.service';
//import { DomSanitizer } from '@angular/platform-browser';
//import { MessageService } from '../../shared/message.service';

@Component({
    selector: 'app-default-catalog',
    templateUrl: './default-catalog.component.html',
    styleUrls: ['./default-catalog.component.scss']
})
export class DefaultCatalogComponent extends BaseCatalogComponent implements OnInit {

    constructor(elementRef: ElementRef, library: LibraryService,
        //messageService: MessageService,
        playerService: PlayerService, ps: ParameterService,
       /* sanitizer: DomSanitizer, */log: LoggingService) {
        super(elementRef, library,  ps, /*sanitizer,*/ playerService, log);
        //console.log(`constructor()`);
    }

    //async ngOnInit() {
    //}
    //protected addArtistToDefaultView() {

    //}
    protected onSearch() {

    }
}
