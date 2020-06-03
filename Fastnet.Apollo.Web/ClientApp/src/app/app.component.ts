import { Component, HostListener, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { LibraryService } from './shared/library.service';
import { Parameters, Style } from './shared/common.types';
import { Router } from '@angular/router';

import { LoggingService } from './shared/logging.service';
import { ParameterService } from './shared/parameter.service';
import { ControlBase, Logger } from '../fastnet/controls/controlbase.type';
import { SlidingPanelsComponent } from './components/sliding-panels.component';

declare var require: any;
export const appInfo = {
    version: require('../../package.json').version
}


@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss']
})
export class AppComponent {
    @ViewChild(SlidingPanelsComponent, { static: false }) slidingPanels: SlidingPanelsComponent;
    parameters: Parameters = new Parameters();
    screenHeight: number;
    screenWidth: number;

    private showPrimary: boolean = true;
    private currentPanelNumber = 0;

    constructor(private ls: LibraryService,
        private ps: ParameterService,
        private router: Router, private log: LoggingService) {
        onerror = function appErrorHandler(errmsg, url, lineNumber) {
            alert(`Error: ${errmsg}`);
            return false;
        }
        Logger.loggingService = log;
        this.screenHeight = innerHeight;
        this.screenWidth = innerWidth;



       ControlBase.deviceSensitivity = true;
       this.ps.ready$.subscribe(async (v) => {
          if (v === true) {
             this.parameters = this.ps.getParameters();
             this.log.isMobileDevice = this.isMobileDevice();
             this.log.information(`[AppComponent] apollo version ${appInfo.version.toString()} : client ${this.parameters.clientIPAddress} started (window inner dimensions: ${innerWidth}w x ${innerHeight}h), isMobile = ${this.isMobileDevice()}, isIpad = ${this.isIpad()}`);
          }
       });

    }


    canShowMenuBars(): boolean {

        return true;//r;
    }
    isPrimary() {
        return this.showPrimary;
    }
    getActivePanelNumber() {
        //console.log(`returning cuurent panel number ${this.currentPanelNumber}`);
        return this.currentPanelNumber;
    }
    onSwipe(e: AnimationEvent) {
        //this.log.information(`swipe ${JSON.stringify(e)}`);
    }
    onSwipeLeft(e: AnimationEvent) {
        //this.log.information(`swipe left ${JSON.stringify(e)}`);
        this.currentPanelNumber = this.slidingPanels.changePanel();
    }
    onSwipeRight(e: AnimationEvent) {
        //this.log.information(`swipe right ${JSON.stringify(e)}`);
        this.currentPanelNumber = this.slidingPanels.changePanel(true);
    }
    toggleSlidingPanels() {
        this.showPrimary = !this.showPrimary;
        this.currentPanelNumber = this.slidingPanels.changePanel();
    }

    @HostListener('window:resize', ['$event'])
    onResize(event?) {

        this.screenHeight = innerHeight;
        this.screenWidth = innerWidth;
    }

    isIpad() {
        return this.ps.isIpad();
    }
    isMobileDevice() {
        return this.ps.isMobileDevice();
    }

}
