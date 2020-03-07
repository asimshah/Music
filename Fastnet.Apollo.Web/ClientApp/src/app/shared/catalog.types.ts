import { Highlighter } from "./common.types";
import { MusicStyles } from "./common.enums";


export enum EditorResult {
   saveChanges,
   cancel
}

export enum ArtistType {
   Artist,
   Composer,
   Performer,
   Various
}
export enum OpusType {
   Normal,
   Singles,
   Collection
}
export enum EncodingType {
   unknown,
   mp3,
   flac,
   alac,
   m4a,
   wma,
   //ape,
   //wv,
   mixed // applies only to collections, e.g. albums, where individual tracks are encoded differently
}
export enum LoadState {
   NotLoaded,
   PartiallyLoaded,
   FullyLoaded
}
export enum MetadataQuality {
   High,
   Medium,
   Low
}
export class BaseEntity {
   public id: number;
   public isHighLit = false;
   public highlightedName: string;
   public isMatchedBySearch: boolean;
   highlightSearch(searchText: string, text: string, prefixMode: boolean) {
      this.highlightedName = Highlighter.highlight(searchText, text, prefixMode);
   }
}

export class Artist extends BaseEntity {
   public name: string;
   public lastname: string;
   public artistType: ArtistType;
   public styles: MusicStyles[];
   public workCount: number;
   public singlesCount: number;
   public compositionCount: number;
   public performanceCount: number;
   public quality: MetadataQuality;
   public imageUrl: string;
   public works: Work[] = [];
   public compositions: Composition[];
   public copyProperties(a: Artist) {
      this.id = a.id;
      this.name = a.name;
      this.lastname = a.lastname;
      this.highlightedName = a.highlightedName;
      this.artistType = a.artistType;
      this.styles = a.styles;
      this.workCount = a.workCount;
      this.singlesCount = a.singlesCount;
      this.compositionCount = a.compositionCount;
      this.performanceCount = a.performanceCount;
      this.quality = a.quality;
      this.imageUrl = a.imageUrl;
      this.compositions = a.compositions ? a.compositions : [];
      //this.works = a.works ? a.works : [];
      this.works = [];
      if (a.works) {
         for (let w1 of a.works) {
            let w2 = new Work();
            w2.copyProperties(w1);
            this.works.push(w2);
         }
      }
   }
   public showWorks: boolean = true;
   public editMode: boolean = false;

}
export class Work extends BaseEntity {
   public type = 'work';
   public artistId: number;
   public name: string;
   public opusType: OpusType;
   public partNumber: number | null;
   public partName: string;
   public year: number;
   public trackCount: number;
   public coverArtUrl: string;
   public quality: MetadataQuality;
   public tracks: Track[];
   public formattedDuration: string;
   public showTracks: boolean = false;
   public copyProperties(w: Work) {
      this.id = w.id;
      this.artistId = w.artistId;
      this.name = w.name;
      this.highlightedName = w.name;
      this.partNumber = w.partNumber;
      this.partName = w.partName;
      this.trackCount = w.trackCount;
      this.coverArtUrl = w.coverArtUrl;
      this.quality = w.quality;
      this.opusType = w.opusType;
      this.formattedDuration = w.formattedDuration;
      if (w.tracks) {
         this.tracks = [];
         for (let t1 of w.tracks) {
            let t2 = new Track();
            t2.copyProperties(t1);
            this.tracks.push(t2);
         }
      }
   }
   public editMode: boolean = false;
}
export class Track extends BaseEntity {
   public type = 'track';
   public workId: number;
   public artistId: number;
   public number: number | null;
   public title: string;
   public musicFileCount: number;
   public numberQuality: MetadataQuality;
   public titleQuality: MetadataQuality;
   public musicFiles: MusicFile[];
   public work: Work;
   public copyProperties(t: Track) {
      this.workId = t.workId;
      this.artistId = t.artistId;
      this.id = t.id;
      this.number = t.number;
      this.title = t.title;
      this.highlightedName = t.title;
      this.musicFileCount = t.musicFileCount;
      this.numberQuality = t.numberQuality;
      this.titleQuality = t.titleQuality;
      //this.musicFiles = t.musicFiles;
      this.musicFiles = [];
      for (let mf1 of t.musicFiles) {
         let mf2 = new MusicFile();
         mf2.copyProperties(mf1);
         this.musicFiles.push(mf2);
      }
   }
   public showVersions: boolean;

}
export class MusicFile {
   public type = 'musicfile';
   public id: number;
   public encoding: EncodingType;
   public bitRate: number | null;
   public sampleRate: number | null;
   public duration: number;
   public formattedDuration: string;
   public audioProperties: string;
   public isHighLit = false;
   public copyProperties(mf: MusicFile) {
      this.id = mf.id;
      this.encoding = mf.encoding;
      this.bitRate = mf.bitRate;
      this.sampleRate = mf.sampleRate;
      this.duration = mf.duration;
      this.formattedDuration = mf.formattedDuration;
      this.audioProperties = mf.audioProperties;
      this.isHighLit = mf.isHighLit;
   }
}
export class Composition extends BaseEntity {
   public type = 'composition';
   public name: string;
   public performances: Performance[] | null = null;
   public showPerformances: boolean;
   public copyProperties(c: Composition) {
      this.id = c.id;
      this.name = c.name;
      this.highlightedName = c.name;
      //this.performances = c.performances ? c.performances : null;
      if (c.performances) {
         this.performances = [];
         for (let p1 of c.performances) {
            let p2 = new Performance();
            p2.copyProperties(p1);
            this.performances.push(p2);
         }
      }
      this.showPerformances = false;
   }
}
export class Performance extends BaseEntity {
   public type = 'performance';
   public performers: string;
   public year: number;
   public albumName: string;
   public albumCoverArt: string;
   public movementCount: string;
   public formattedDuration: string;
   public movements: Track[];
   public showMovements: boolean = false;
   public copyProperties(p: Performance) {
      this.id = p.id;
      this.performers = p.performers;
      this.highlightedName = p.highlightedName;
      this.year = p.year;
      this.albumName = p.albumName;
      this.albumCoverArt = p.albumCoverArt;
      this.movementCount = p.movementCount;
      this.formattedDuration = p.formattedDuration;
      //this.movements = p.movements ? p.movements : [];
      if (p.movements) {
         this.movements = [];
         for (let m1 of p.movements) {
            let m2 = new Movement();
            m2.copyProperties(m1);
            this.movements.push(m2);
         }
      }
   }
}

export class Movement extends Track {
   isHighLit: boolean = false;
   public copyProperties(m: Movement) {
      super.copyProperties(m);
      this.isHighLit = m.isHighLit;
   }

}
export class OpusDetails {
   public id: number;
   public artistName: string;
   public opusName: string;
   public compressedArtistName: string;
   public compressedOpusName: string;
   public compressedPerformanceName: string;
   public trackDetails: TrackDetail[];
}
export class MusicFileDetail {
   public id: number;
   public isFaulty: boolean;
   public file: string;
   public fileLength: number;
   public fileLastWriteTimeString: string;
}
export class TrackDetail {
   public id: number;
   public number: number;
   public title: string;
   public musicFileCount: number;
   public musicFiles: MusicFileDetail[];
}
export class TagValue {
   public value: string;
   public selected: boolean;
}
export class TagValueStatus {
   public name: string; // name of the tag
   public values: TagValue[] = [];

}
export class MusicFileTEO {
   public file: string;
   public musicFileId: number;
   public trackId: number;
   public trackNumberTag: TagValueStatus;
   public titleTag: TagValueStatus;
   public albumTag: TagValueStatus;
   public yearTag: TagValueStatus;
}
export class PopularMusicFileTEO extends MusicFileTEO {

}
export class WesternClassicalMusicFileTEO extends MusicFileTEO {
   public composerTag: TagValueStatus;
   public compositionTag: TagValueStatus;
   public performerTag: TagValueStatus;
   public orchestraTag: TagValueStatus;
   public conductorTag: TagValueStatus;
   public movementNumberTag: TagValueStatus;
}
export abstract class TEOBase<T> {
   public id: number;
   public pathToMusicFiles: string;
   public artistTag: TagValueStatus;
   public albumTag: TagValueStatus;
   public yearTag: TagValueStatus;
   public trackList: T[];
   public trackNumbersValid: boolean;
}
export class PopularAlbumTEO extends TEOBase<PopularMusicFileTEO> {

}
export class WesternClassicalAlbumTEO extends TEOBase<WesternClassicalMusicFileTEO> {
   public performanceList: PerformanceTEO[];
}
export class PerformanceTEO {//extends TEOBase<WesternClassicalMusicFileTEO> {
   public performanceId: number;
   public composerTag: TagValueStatus;
   public compositionTag: TagValueStatus;
   public performerTag: TagValueStatus;
   public orchestraTag: TagValueStatus;
   public conductorTag: TagValueStatus;
   public movementList: WesternClassicalMusicFileTEO[];
   public movementFilenames: string[];
}
export function isWork(item: MusicFile | Work | Track | Composition | Performance): item is Work {
   return (item as Work).type === 'work';
}
export function isTrack(item: MusicFile | Work | Track | Composition | Performance): item is Track {
   return (item as Track).type === 'track';
}
export function isComposition(item: MusicFile | Work | Track | Composition | Performance): item is Composition {
   return (item as Composition).type === 'composition';
}
export function isPerformance(item: MusicFile | Work | Track | Composition | Performance): item is Performance {
   return (item as Performance).type === 'performance';
}
export function isMusicFile(item: MusicFile | Work | Track | Composition | Performance): item is MusicFile {
   return (item as MusicFile).type === 'musicfile';
}
