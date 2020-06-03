import { Injectable } from "@angular/core";

import { Parameters, Style } from "./common.types";
import { BaseService } from "./base-service.service";
import { HttpClient } from "@angular/common/http";
import { BehaviorSubject } from "rxjs";
import { setLocalStorageValue, getLocalStorageValue } from "./common.functions";
import { MusicStyles, LocalStorageKeys } from "./common.enums";
import { LoggingService } from "./logging.service";
//import { MessageService } from "./message.service";
//import { PlayerService } from "./player.service";

export class PopularSettings {
   showArtists: boolean = false;
}

export function ParameterServiceFactory(ps: ParameterService) {
   return () => ps.init();
}

@Injectable()
export class ParameterService extends BaseService {
   private emulateTouchDevice = false;
   //private currentDeviceStorageKey: string;
   private savedStyleStorageKey: string;
   private savedBrowserStorageKey: string;
   private popularSettingsStorageKey: string;
   private parameters: Parameters;
   public ready$ = new BehaviorSubject<boolean>(false);
   public currentStyleChanged$ = new BehaviorSubject<boolean>(false);
   public popularSettings: BehaviorSubject<PopularSettings>;
   public currentStyle: BehaviorSubject<Style>;
   public currentTotals: string[] = [];
   constructor(http: HttpClient,
      private log: LoggingService) {
      super(http, "lib");
      //this.currentDeviceStorageKey = `music:${LocalStorageKeys[LocalStorageKeys.currentDevice]}`;
      this.savedStyleStorageKey = `music:${LocalStorageKeys[LocalStorageKeys.currentStyle]}`;// this.getStorageKey(LocalStorageKeys.currentStyle);// LocalStorageKeys[LocalStorageKeys.currentStyle];
      this.savedBrowserStorageKey = `music:${LocalStorageKeys[LocalStorageKeys.browserKey]}`; //this.getStorageKey(LocalStorageKeys.browserKey)
      this.popularSettingsStorageKey = `music:${LocalStorageKeys[LocalStorageKeys.popularSettings]}`;//this.getStorageKey(LocalStorageKeys.popularSettings);// LocalStorageKeys[LocalStorageKeys.popularSettings];
   }
   //getStorageKey(key: LocalStorageKeys) {
   //   return `music:${LocalStorageKeys[key]}`;
   //}
   async init() {
      console.log("ParameterService: init()");
      let currentKey = this.getStoredBrowserKey();
      this.parameters = await this.getAsync<Parameters>(`parameters/get/${currentKey}`);
      if (currentKey !== this.parameters.browserKey) {
         setLocalStorageValue(this.savedBrowserStorageKey, this.parameters.browserKey);
         console.log(`browser key changed from ${currentKey} to ${this.parameters.browserKey}`);
      }
      //console.log(`Parameters: ${JSON.stringify(this.parameters)}`);
      this.log.information(`[ParameterService] client started on ip address ${this.parameters.clientIPAddress}`);
      let firstStyle = this.getFirstEnabledStyle();
      let savedMusicStyle = parseInt(getLocalStorageValue(this.savedStyleStorageKey, firstStyle.id.toString())) as MusicStyles;
      let cs = this.getStyle(savedMusicStyle);
      if (!cs || cs.enabled === false) {
         cs = firstStyle;
      };
      this.currentStyle = new BehaviorSubject<Style>(cs);
      setLocalStorageValue(this.savedStyleStorageKey, cs.id.toString());
      //await this.updateStyleTotals(cs.id);      
      this.popularSettings = new BehaviorSubject<PopularSettings>(this.getPopularSettings());
      this.log.information(`[ParameterService] ready, browser (and local audio) key is ${this.getStoredBrowserKey()}`);
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
      //await this.updateStyleTotals(musicStyle);
      //this.currentTotals = await this.getAsync<string[]>(`information/${musicStyle}`);
      this.currentStyle.next(cs);
   }
   setPopularSettings(val: PopularSettings) {
      setLocalStorageValue(this.popularSettingsStorageKey, JSON.stringify(val));
      this.popularSettings.next(val);
   }
   getPopularSettings(): PopularSettings {
      let settings: PopularSettings = JSON.parse(getLocalStorageValue(this.popularSettingsStorageKey, JSON.stringify(new PopularSettings())));
      return settings;
   }
   //private async updateStyleTotals(musicStyle: MusicStyles) {
   //   this.currentTotals = this.parameters.styles[musicStyle - 1].totals;
   //   //this.currentTotals = await this.getAsync<string[]>(`information/${musicStyle}`);
   //   this.currentStyleChanged$.next(true);
   //}
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
   private getStoredBrowserKey() {
      return getLocalStorageValue(this.savedBrowserStorageKey, "");
   }
}
