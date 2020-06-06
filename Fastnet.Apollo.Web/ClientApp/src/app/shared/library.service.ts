
import { BaseService } from "./base-service.service";
import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Parameters, Style } from "./common.types";

import { Artist, Performance, Composition, Track, Movement, Work, TrackDetail, OpusDetails, PerformanceTEO, MusicFileTEO, PopularAlbumTEO, WesternClassicalAlbumTEO, TagValue, ArtistSet, Raga } from "../shared/catalog.types";
import { MusicStyles } from "./common.enums";
import { sortedInsert } from "../../fastnet/core/common.functions";

class BrowserInformation {
   userAgent: string;
}

@Injectable()
export class LibraryService extends BaseService {
   constructor(http: HttpClient) {
      super(http, "lib");
   }
   public async search<T>(style: Style, text: string): Promise<T> {
      return this.getAsync<T>(`search/${style.id}/${text}`);
   }
   public async getAllArtists(style: Style) {
      return this.getAsync<number[]>(`get/${style.id}/allartists`);
   }
   public async getArtist(style: Style, id: number): Promise<Artist> {
      let a = await this.getAsync<Artist>(`get/${style.id}/artist/${id}`);
      return new Promise<Artist>(resolve => {
         let artist = new Artist();
         artist.copyProperties(a); // so local properties and methods are present
         resolve(artist);
      });
   }
   public async getArtists(style: Style, ids: number[]): Promise<ArtistSet> {
      let args = ids.join("&id=");
      let a = await this.getAsync<ArtistSet>(`get/${style.id}/artistSet?id=${args}`);
      return new Promise<ArtistSet>(resolve => {
         let set = new ArtistSet();
         set.copyProperties(a); // so local properties and methods are present
         resolve(set);
      });
   }
   public async getAllRagas(set: ArtistSet): Promise<Raga[]> {
      //[HttpGet("get/artist/allragas")]
      let args = set.artistIds.join("&id=");
      let result = await this.getAsync<Raga[]>(`get/artist/allragas?id=${args}`);
      return new Promise<Raga[]>(resolve => {
         let list: Raga[] = [];
         for (let item of result) {
            let r = new Raga();
            r.copyProperties(item); // so local properties and methods are present
            list.push(r);
         }
         resolve(list);
      });
   }
   public async getRaga(ragaId: number): Promise<Raga> {
      let r = await this.getAsync<Raga>(`get/raga/${ragaId}`);
      return new Promise<Raga>((resolve) => {
         let raga = new Raga();
         raga.copyProperties(r); // so local properties and methods are present
         resolve(raga);
      });
   }
   public async getComposition(id: number): Promise<Composition> {
      let c = await this.getAsync<Composition>(`get/composition/${id}`);
      return new Promise<Composition>((resolve) => {
         let composition = new Composition();
         composition.copyProperties(c); // so local properties and methods are present
         resolve(composition);
      });
   }
   public async getWork(id: number): Promise<Work> {
      let w = await this.getAsync<Work>(`get/work/${id}`);
      return new Promise<Work>((resolve) => {
         let work = new Work();
         work.copyProperties(w); // so local properties and methods are present
         resolve(work);
      });
   }
   public async getTrack(id: number): Promise<Track> {
      let t = await this.getAsync<Track>(`get/track/${id}`);
      return new Promise<Track>((resolve) => {
         let track = new Track();
         track.copyProperties(t); // so local properties and methods are present
         resolve(track);
      });
   }
   public async getPerformance(id: number, full: boolean = false): Promise<Performance> {
      //let p = await this.getAsync<Performance>(`get/performance/${id}`);
      let query = full ? `get/performance/${id}/true` : `get/performance/${id}`;
      let p = await this.getAsync<Performance>(query);
      return new Promise<Performance>(resolve => {
         let performance = new Performance();
         performance.copyProperties(p);
         resolve(performance);
      });
   }
   public async getCompositionInfo(id: number): Promise<Composition> {
      return this.getAsync<Composition>(`get/composition/${id}`);
   }
   public async getPerformanceInfo(id: number): Promise<Performance> {
      return this.getAsync<Performance>(`get/performance/${id}`);
   }
   public async getAllCompositionPerformances(compositionId: number, full: boolean = false): Promise<Performance[]> {
      let query = full ? `get/composition/allperformances/${compositionId}/true` : `get/composition/allperformances/${compositionId}`;
      let list = await this.getAsync<Performance[]>(query);
      return new Promise<Performance[]>(resolve => {
         let performances: Performance[] = [];
         for (let item of list) {
            let performance = new Performance();
            performance.copyProperties(item);
            performances.push(performance);
         }
         resolve(performances);
      });
   }
   public async getAllRagaPerformances(style: Style, set: ArtistSet, raga: Raga): Promise<Performance[]> {
      let args = set.artistIds.join("&id=");
      let query = `get/${style.id}/${raga.id}/allperformances/artistSet?id=${args}`;
      let list = await this.getAsync<Performance[]>(query);
      return new Promise<Performance[]>(resolve => {
         let performances: Performance[] = [];
         for (let item of list) {
            let performance = new Performance();
            performance.copyProperties(item);
            performances.push(performance);
         }
         resolve(performances);
      });
   }
   public async getAllCompositions(artistId: number, full: boolean = false): Promise<Composition[]> {
      //let list = await this.getAsync<Composition[]>(`get/artist/allcompositions/${artistId}`);
      let query = full ? `get/artist/allcompositions/${artistId}/true` : `get/artist/allcompositions/${artistId}`;
      //return this.getAsync<Composition[]>(query);
      let list = await this.getAsync<Composition[]>(query);
      return new Promise<Composition[]>(resolve => {
         let compositions: Composition[] = [];
         for (let item of list) {
            let c = new Composition();
            c.copyProperties(item);
            compositions.push(c);
         }
         resolve(compositions);
      });

   }
   public async getAllWorks(style: Style, artist: Artist, full: boolean = false): Promise<Work[]> {
      //let list = await this.getAsync<Work[]>(`get/artist/allworks/${artist.id}`);
      let query = full ? `get/artist/${style.id}/allworks/${artist.id}/true` : `get/artist/allworks/${artist.id}`;
      let list = await this.getAsync<Work[]>(query);
      return new Promise<Work[]>(resolve => {
         let works: Work[] = [];
         for (let item of list) {
            let c = new Work();
            c.copyProperties(item);
            works.push(c);
         }
         resolve(works);
      });
      //return this.getAsync<Composition[]>(`get/artist/allcompositions/${artistId}`);
   }
   public async getAllMovements(performanceid: number): Promise<Movement[]> {
      let list = await this.getAsync<Track[]>(`get/performance/allmovements/${performanceid}`);
      return new Promise<Movement[]>(resolve => {
         let movements: Movement[] = [];
         for (let item of list) {
            let m = new Movement();
            m.copyProperties(item);
            movements.push(m);
         }
         resolve(movements);
      });
   }
   public async getAllTracks(work: Work): Promise<Track[]> {
      let list = await this.getAsync<Track[]>(`get/work/alltracks/${work.id}`);
      return new Promise<Track[]>(resolve => {
         let tracks: Track[] = [];
         for (let item of list) {
            let t = new Track();
            t.work = work;
            t.copyProperties(item);
            tracks.push(t);
         }
         resolve(tracks);
      });
   }
   public async getPerformanceDetails(performance: Performance) {
      return await this.getAsync<OpusDetails>(`get/performance/details/${performance.id}`);
   }
   public async getWorkDetails(work: Work) {
      return await this.getAsync<OpusDetails>(`get/work/details/${work.id}`);
   }
   public async editWork(id: number) {
      return this.getAsync<PopularAlbumTEO>(`edit/work/${id}`);
   }
   /**
    * When requesting edit for a performance of a particular id
    * always returns *all* PerformanceTEO for the disk folder that performance
    * This is so that music files can be moved around between these performances     
    */
   public async editPerformance(id: number) {
      return new Promise<WesternClassicalAlbumTEO>(async resolve => {
         let wcaTeo = await this.getAsync<WesternClassicalAlbumTEO>(`edit/performance/${id}`);
         for (let pteo of wcaTeo.performanceList) {
            pteo.movementList = [];
            //let i = 0;
            for (let fn of pteo.movementFilenames) {
               let result = wcaTeo.trackList.find((t) => {
                  if (t.file === fn) {
                     return t;
                  }
               })
               if (result) {
                  sortedInsert(pteo.movementList, result, (l, r) => {
                     let a = this.getSelectedNumber(l.movementNumberTag.values);
                     let b = this.getSelectedNumber(r.movementNumberTag.values);
                     return a > b ? 1 : b > a ? -1 : 0;
                     //return this.getSelectedNumber(l.movementNumberTag.values).localeCompare(this.getSelectedNumber(r.movementNumberTag.values));
                  });
                  //pteo.movementList.push(result);
                  //break;
               }
               //i++;
            }
         }
         resolve(wcaTeo);
      });
   }
   private getSelectedNumber(values: TagValue[]) {
      let candidates = values.filter((v) => v.selected);
      if (candidates.length === 1) {
         return parseInt(candidates[0].value);
      } else {
         return 0;//"";
      }
   }
   public async updatePopularAlbum(albumTeo: PopularAlbumTEO) {
      return this.postAsync(`update/work/${MusicStyles.Popular}`, albumTeo);
   }
   public async updateWesternClassicalAlbum(albumTeo: WesternClassicalAlbumTEO) {
      return this.postAsync(`update/work/${MusicStyles.WesternClassical}`, albumTeo);
   }
   //
   public async resampleWork(id: number) {
      return this.getAsync(`resample/work/${id}`);
   }
   public async resamplePerformance(id: number) {
      return this.getAsync(`resample/performance/${id}`);
   }
   public async startMusicScanner() {
      return this.getAsync("start/musicfilescanner");
   }
   public async startCatalogueValidator() {
      return this.getAsync("start/cataloguevalidator");
   }
   public async resetDatabase() {
      return this.getAsync("reset/database/true");
   }
   public async resetWork(id: number) {
      return this.getAsync(`reset/work/${id}`);
   }
   public async resetPerformance(id: number) {
      return this.getAsync(`reset/performance/${id}`);
   }
}
