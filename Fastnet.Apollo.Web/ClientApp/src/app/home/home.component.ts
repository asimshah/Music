import { Component, OnInit, ViewChild, ViewContainerRef, ComponentFactoryResolver, Type, ComponentRef, OnDestroy, AfterViewInit } from '@angular/core';
import { MusicStyles } from '../shared/common.enums';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { log } from '../shared/common.functions';
import { LibraryService } from '../shared/library.service';
import { DefaultCatalogComponent } from '../catalog/default-catalog/default-catalog.component';
import { BaseCatalogComponent } from '../catalog/base-catalog.component';
import { Style } from '../shared/common.types';
import { PopularCatalogComponent } from '../catalog/popular-catalog/popular-catalog.component';
import { WesternClassicalCatalogComponent } from '../catalog/western-classical-catalog/western-classical-catalog.component';
import { ParameterService } from '../shared/parameter.service';
import { IndianClassicalCatalogComponent } from '../catalog/indian-classical-catalog/indian-classical-catalog.component';
import { HindiFilmsCatalogComponent } from '../catalog/hindi-films-catalog/hindi-films-catalog.component';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.scss']
})
export class HomeComponent implements AfterViewInit, OnDestroy {
    currentStyle: Style;
    searchText: string;
    w: Window;
    @ViewChild('catalogComponent', {static: false, read: ViewContainerRef }) catalogContainer: ViewContainerRef;
    private currentCatalog: BaseCatalogComponent;
    private catalogComponentRef: ComponentRef<any> | null = null;
    private currentStyleSubscription: Subscription;
    constructor(private route: ActivatedRoute, private parameterService: ParameterService,
        private componentResolver: ComponentFactoryResolver) {
    }
    ngAfterViewInit() {
        this.currentStyleSubscription = this.parameterService.currentStyle.subscribe((d) => {
            this.currentStyle = d;
            this.resolveCatalog();
        });
    }

    ngOnDestroy() {
        if (this.currentStyleSubscription) {
            this.currentStyleSubscription.unsubscribe();
        }
        if (this.catalogComponentRef !== null) {
            this.catalogComponentRef.destroy();
        }
    }
    //isTouchDevice() {
    //    return this.isMobile() || this.isIpad();
    //}
    isMobile() {
        return this.parameterService.isMobileDevice();
    }
    isIpad() {
        return this.parameterService.isIpad();
    }
    async onSearchClick() {
        //console.log(`search text is ${this.searchText}`);
        if (this.currentCatalog && this.searchText && this.searchText.trim().length > 0) {
            await this.currentCatalog.setSearch(this.searchText);
        }
    }
    async onClearClick() {
        if (this.currentCatalog) {
            await this.currentCatalog.clearSearch();
        }
    }
    private resolveCatalog() {
        setTimeout(() => {
            switch (this.currentStyle.id) {
                case MusicStyles.Popular:
                    this.loadCatalog(PopularCatalogComponent);
                    break;
                case MusicStyles.WesternClassical:
                    this.loadCatalog(WesternClassicalCatalogComponent);
                  break;
               case MusicStyles.IndianClassical:
                  this.loadCatalog(IndianClassicalCatalogComponent);
                  break;
               case MusicStyles.HindiFilms:
                  this.loadCatalog(HindiFilmsCatalogComponent);
                  break;
                default:
                    this.loadCatalog(DefaultCatalogComponent);
                    break;
            }
        }, 0);
    }
    private loadCatalog<T extends BaseCatalogComponent>(catalog: Type<T>) {
        this.catalogContainer.clear();
        let cf = this.componentResolver.resolveComponentFactory<T>(catalog);
        this.catalogComponentRef = this.catalogContainer.createComponent(cf);
        this.currentCatalog = this.catalogComponentRef.instance;
    }
}
