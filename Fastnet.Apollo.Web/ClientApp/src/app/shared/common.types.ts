import { DeviceState, MusicStyles, PlaylistItemType, PlayerStates, AudioDeviceType, PlayerCommands } from "./common.enums";
import { EncodingType } from "./catalog.types";

export class Style {
   id: MusicStyles;
   enabled: boolean;
   displayName: string;
   totals: string[] = [];
}

export class Parameters {
   version: string;
   browserKey: string;
   appName: string;
   browser: string;
   isMobile: boolean = false;
   isIpad: boolean = false;
   compactLayoutWidth: number = 768;
   clientIPAddress: string;
   styles: Style[] = [];
}

export class SearchKey {
   key: number;
   name: string;
}
export class TrackKey extends SearchKey {
   number: number;
}
export class TrackResult {
   track: TrackKey;
}
export class PerformanceResult {
   performance: SearchKey;
   performanceIsMatched: boolean;
   movements: TrackResult[];
}

export class AudioCapability {
   constructor(public maxSampleRate: number) {

   }
}
export class AudioDevice {
   id: number;
   key: string;
   type: AudioDeviceType;
   enabled: boolean;
   displayName: string;
   name: string;

   capability: AudioCapability;
   hostMachine: string;
   canReposition: boolean;

   public copyProperties(d: AudioDevice) {
      this.id = d.id;
      this.key = d.key;
      this.displayName = d.displayName;
      this.hostMachine = d.hostMachine;
      this.enabled = d.enabled;
      this.type = d.type;
      this.canReposition = d.canReposition;

      this.capability = new AudioCapability(d.capability.maxSampleRate);
   }
}
export class PlaylistPosition {
   constructor(public major: number = 0, public minor: number = 0) {

   }
}
export class DeviceStatus {
   key: string;
   playlistPosition: PlaylistPosition;
   state: PlayerStates;
   volume: number;
   currentTime: number;
   totalTime: number;
   remainingTime: number;
   formattedCurrentTime: string;
   formattedTotalTime: string;
   formattedRemainingTime: string;
   commandSequence: number;
   public copyProperties(ds: DeviceStatus) {
      this.key = ds.key;
      this.playlistPosition = new PlaylistPosition(ds.playlistPosition.major, ds.playlistPosition.minor);
      this.state = ds.state;
      this.volume = ds.volume;
      this.currentTime = ds.currentTime;
      this.totalTime = ds.totalTime;
      this.remainingTime = ds.remainingTime;
      this.formattedCurrentTime = ds.formattedCurrentTime;
      this.formattedTotalTime = ds.formattedTotalTime;
      this.formattedRemainingTime = ds.formattedRemainingTime;
      this.commandSequence = ds.commandSequence;
   }
}
export class PlaylistItem {
   id: number;
   type: PlaylistItemType;
   notPlayableOnCurrentDevice: boolean;
   position: PlaylistPosition;
   sequence: number;
   //title: string;
   titles: string[];
   audioProperties: string;
   sampleRate: number;
   coverArtUrl: string;
   totalTime: number;
   formattedTotalTime: string;
   isOpen: boolean;
   isSubitem: boolean;
   subItems: PlaylistItem[];
   public copyProperties(pli: PlaylistItem) {
      this.id = pli.id;
      this.type = pli.type;
      this.notPlayableOnCurrentDevice = pli.notPlayableOnCurrentDevice;
      this.position = new PlaylistPosition(pli.position.major, pli.position.minor);// pli.position;
      //this.title = pli.title;
      this.titles = pli.titles;
      this.coverArtUrl = pli.coverArtUrl;
      this.audioProperties = pli.audioProperties;
      this.sampleRate = pli.sampleRate;
      this.sequence = pli.sequence;
      this.totalTime = pli.totalTime;
      this.formattedTotalTime = pli.formattedTotalTime;
      this.isOpen = false;
      this.isSubitem = pli.isSubitem;
      this.subItems = [];
      if (pli.subItems) {
         pli.subItems.forEach(x => {
            let item = new PlaylistItem();
            item.copyProperties(x);
            item.coverArtUrl = this.coverArtUrl;
            this.subItems.push(item);
         });
      }
   }
}
export class PlaylistUpdate {
   deviceKey: string;
   displayName: string;
   items: PlaylistItem[];
   public copyProperties(plu: PlaylistUpdate) {
      {
         this.deviceKey = plu.deviceKey;
         this.displayName = plu.displayName;
         this.items = [];
         plu.items.forEach(x => {
            let pli = new PlaylistItem();
            pli.copyProperties(x);
            this.items.push(pli);
         });
      }
   }
}
export class Highlighter {
   static highlightAttributes = "class='matched-text' style='text-decoration: underline'";
   static highlight(searchText: string, text: string, prefixMode: boolean): string {
      let pattern: string | null = null;
      if (prefixMode) {
         //pattern = "\\b" + this.searchText;
         return Highlighter.prefixMatchAnyWord(searchText, text);
      } else {
         let rt = Highlighter.removeDiacritics(text);
         let hasLengthChanged = rt.length !== text.length;
         pattern = Highlighter.createLocalSearchPattern(searchText);// searchHighlight.localSearchText;
         let re = new RegExp(pattern, "ig");
         let m = re.exec(rt);
         if (!hasLengthChanged && m != null) {
            let x = m[0];
            let index = m.index;
            let length = x.length;
            let firstPart = text.substr(0, index);
            let matchedPart = text.substr(index, length);
            let remainder = text.substr(index + length);
            return `${firstPart}<span ${this.highlightAttributes}>${matchedPart}</span>${remainder}`;
         }
         return text.replace(re, `<span ${this.highlightAttributes}>` + "$&" + "</span>");
      }
   }
   static prefixMatchAnyWord(searchText: string, text: string): string {
      let search = Highlighter.removeDiacritics(searchText);
      let words = Highlighter.splitToWords(text);
      let result: string | null = null;
      let index = 0;
      for (let w of words) {
         let wt = Highlighter.removeDiacritics(w);
         if (wt.toLowerCase().startsWith(search.toLowerCase())) {
            //console.log(`matched ${w} using ${wt}`);
            if (wt.length === w.length) {
               // remove diacritics did not alter the length of the string
               let highlitPart = w.substr(0, search.length);
               let remainder = w.substr(search.length);
               result = `<span ${this.highlightAttributes}>${highlitPart}</span>${remainder}`;
            } else {
               result = `<span ${this.highlightAttributes}>${w}</span>`;
            }
            break;
         }
         ++index;
      }
      if (result !== null) {
         words[index] = result;
      }
      let temp = words.join(" ");
      return temp.replace(new RegExp(" ,", "g"), ",");
      //return words.join(" ");
   }
   static diacritics = {
      '\u00C0': 'A',  // À => A
      '\u00C1': 'A',   // Á => A
      '\u00C2': 'A',   // Â => A
      '\u00C3': 'A',   // Ã => A
      '\u00C4': 'A',   // Ä => A
      '\u00C5': 'A',   // Å => A
      '\u00C6': 'AE', // Æ => AE
      '\u00C7': 'C',   // Ç => C
      '\u00C8': 'E',   // È => E
      '\u00C9': 'E',   // É => E
      '\u00CA': 'E',   // Ê => E
      '\u00CB': 'E',   // Ë => E
      '\u00CC': 'I',   // Ì => I
      '\u00CD': 'I',   // Í => I
      '\u00CE': 'I',   // Î => I
      '\u00CF': 'I',   // Ï => I
      '\u0132': 'IJ', // Ĳ => IJ
      '\u00D0': 'D',   // Ð => D
      '\u00D1': 'N',   // Ñ => N
      '\u00D2': 'O',   // Ò => O
      '\u00D3': 'O',   // Ó => O
      '\u00D4': 'O',   // Ô => O
      '\u00D5': 'O',   // Õ => O
      '\u00D6': 'O',   // Ö => O
      '\u00D8': 'O',   // Ø => O
      '\u0152': 'OE', // Œ => OE
      '\u00DE': 'TH', // Þ => TH
      '\u00D9': 'U',   // Ù => U
      '\u00DA': 'U',   // Ú => U
      '\u00DB': 'U',   // Û => U
      '\u00DC': 'U',   // Ü => U
      '\u00DD': 'Y',   // Ý => Y
      '\u0178': 'Y',   // Ÿ => Y
      '\u00E0': 'a',   // à => a
      '\u00E1': 'a',   // á => a
      '\u00E2': 'a',   // â => a
      '\u00E3': 'a',   // ã => a
      '\u00E4': 'a',   // ä => a
      '\u00E5': 'a',   // å => a
      '\u00E6': 'ae', // æ => ae
      '\u00E7': 'c',   // ç => c
      '\u00E8': 'e',   // è => e
      '\u00E9': 'e',   // é => e
      '\u00EA': 'e',   // ê => e
      '\u00EB': 'e',   // ë => e
      '\u00EC': 'i',   // ì => i
      '\u00ED': 'i',   // í => i
      '\u00EE': 'i',   // î => i
      '\u00EF': 'i',   // ï => i
      '\u0133': 'ij', // ĳ => ij
      '\u00F0': 'd',   // ð => d
      '\u00F1': 'n',   // ñ => n
      '\u00F2': 'o',   // ò => o
      '\u00F3': 'o',   // ó => o
      '\u00F4': 'o',   // ô => o
      '\u00F5': 'o',   // õ => o
      '\u00F6': 'o',   // ö => o
      '\u00F8': 'o',   // ø => o
      '\u0153': 'oe', // œ => oe
      '\u00DF': 'ss', // ß => ss
      '\u00FE': 'th', // þ => th
      '\u00F9': 'u',   // ù => u
      '\u00FA': 'u',   // ú => u
      '\u00FB': 'u',   // û => u
      '\u00FC': 'u',   // ü => u
      '\u00FD': 'y',   // ý => y
      '\u00FF': 'y',   // ÿ => y
      '\uFB00': 'ff', // ﬀ => ff
      '\uFB01': 'fi',   // ﬁ => fi
      '\uFB02': 'fl', // ﬂ => fl
      '\uFB03': 'ffi',  // ﬃ => ffi
      '\uFB04': 'ffl',  // ﬄ => ffl
      '\uFB05': 'ft', // ﬅ => ft
      '\uFB06': 'st'  // ﬆ => st
   };
   static removeDiacritics(text: string): string {
      let accentsMap = new Map<string, string>((<any>Object).entries(Highlighter.diacritics));

      return text.replace(/[^a-zA-Z0-9\s]+/g, (a) => {
         return accentsMap.get(a) || a;
      });
   }
   static splitToWords(text: string): string[] {
      let parts = text.split(/(\b[^\s]+\b)/);
      let words: string[] = [];
      for (let p of parts) {
         if (p.trim().length > 0) {
            words.push(p.trim())
         }
      }
      return words;
   }
   static createLocalSearchPattern(searchText: string): string {
      let r: string = "";
      let i = 0;
      let s = Highlighter.removeDiacritics(searchText);
      for (let c of s) {
         if (i > 0) {
            //r += '[^a-zA-Z0-9]?';
            r += '[^/w]?';
         }
         r += c;
         ++i;
      }
      return r;
   }
}
export class PlayerCommand {
   command: PlayerCommands;
   deviceKey: string;
   commandSequenceNumber: number;
   streamUrl: string;
   encodingType: EncodingType;
   position: number;
   volume: number;
}
//export class SomeDialogResult {
//    public cancelled: boolean;
//    public userData: any | null = null;
//}
