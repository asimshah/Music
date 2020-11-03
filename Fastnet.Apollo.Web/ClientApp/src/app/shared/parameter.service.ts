import { Injectable } from "@angular/core";

import { Parameters, Style } from "./common.types";
import { BaseService } from "./base-service.service";
import { HttpClient } from "@angular/common/http";
import { BehaviorSubject } from "rxjs";
import { setLocalStorageValue, getLocalStorageValue } from "./common.functions";
import { MusicStyles, LocalStorageKeys } from "./common.enums";
import { LoggingService } from "./logging.service";

export function ParameterServiceFactory(ps: ParameterService) {
   return () => ps.init();
}

@Injectable()
export class ParameterService extends BaseService {
   private emulateTouchDevice = false;
   private savedStyleStorageKey: string;
   private parameters: Parameters;
   public ready$ = new BehaviorSubject<boolean>(false);
   public currentStyleChanged$ = new BehaviorSubject<boolean>(false);
   public currentStyle: BehaviorSubject<Style>;
   public currentTotals: string[] = [];
   public showGeneratedMusic = false;
   constructor(http: HttpClient,
      private log: LoggingService) {
      super(http, "lib");
      this.savedStyleStorageKey = `music:${LocalStorageKeys[LocalStorageKeys.currentStyle]}`;
      //this.popularSettingsStorageKey = `music:${LocalStorageKeys[LocalStorageKeys.popularSettings]}`;
   }
   async init() {
      //console.log("ParameterService: init()");
      this.parameters = await this.getAsync<Parameters>(`parameters/get`);
      let firstStyle = this.getFirstEnabledStyle();
      let savedMusicStyle = parseInt(getLocalStorageValue(this.savedStyleStorageKey, firstStyle.id.toString())) as MusicStyles;
      let cs = this.getStyle(savedMusicStyle);
      if (!cs || cs.enabled === false) {
         cs = firstStyle;
      };
      this.currentStyle = new BehaviorSubject<Style>(cs);
      setLocalStorageValue(this.savedStyleStorageKey, cs.id.toString());
      this.ready$.next(true);
   }
   getParameters() {
      return this.parameters;
   }
   getBrowserKey() {
      return this.parameters.browserKey;
   }
   getBrowser() {
      return this.parameters.browser.toLowerCase();
   }

   isTouchDevice() {
      return this.emulateTouchDevice || this.isMobileDevice() || this.isIpad();
   }
   isIpad() {
      return this.parameters.isIpad;
   }

   isMobileDevice() {
      let mq = matchMedia("(hover: none) and (pointer: coarse)");
      return this.parameters.isMobile || mq.matches;
   }
   getCurrentStyle(): Style {
      return this.currentStyle.getValue();
   }
   async setCurrentStyle(musicStyle: MusicStyles) {
      let cs = this.getStyle(musicStyle);
      setLocalStorageValue(this.savedStyleStorageKey, cs.id.toString());
      this.currentStyle.next(cs);
   }
   private getStyle(s: MusicStyles): Style {
      let r = this.parameters.styles.find(x => x.id === s);
      if (r) {
         return r;
      } else {
         throw `Music style ${s.toString()} not found`;
      }
   }
   private getFirstEnabledStyle(): Style {
      let enabledStyles = this.parameters.styles.filter(x => x.enabled);
      if (enabledStyles.length > 0) {
         return enabledStyles[0];
      } else {
         alert("No enabled styles found");
      }
      throw `No enabled styles found`;
   }
}
