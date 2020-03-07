import { Component, OnInit, Input, AfterViewInit } from '@angular/core';
import { LoggingService } from '../shared/logging.service';

export class SpanFragment {
    text: string;
    highlight: boolean;
}

@Component({
    selector: 'highlighted-text',
    templateUrl: './highlighted-text.component.html',
    styleUrls: ['./highlighted-text.component.scss']
})
export class HighlightedTextComponent implements AfterViewInit {
    @Input() searchText: string = "";
    @Input() text: string = "";
    @Input() prefixMode: boolean = false;
    spans: SpanFragment[] = [];
    constructor(private log: LoggingService) { }
    ngAfterViewInit() {
        this.highlight(this.searchText, this.text, this.prefixMode);
    }

    private highlight(searchText: string, text: string, prefixMode: boolean) {
        let pattern: string | null = null;
        searchText = searchText.trim();
        //prefixMode = false;
        if (prefixMode) {
            this.prefixMatchAnyWord(searchText, text);
        } else {
            let rt = this.removeDiacritics(text);
            let hasLengthChanged = rt.length !== text.length;
            pattern = this.createLocalSearchPattern(searchText);// searchHighlight.localSearchText;
            let re = new RegExp(pattern, "ig");
            let m = re.exec(rt);
            //this.log.information(`highlight 0, m is ${JSON.stringify(m)}, pattern ${pattern}, ${rt}`);
            //setTimeout(() => { }, 1000);
            if (m != null) {
                if (!hasLengthChanged) {
                    //this.log.information(`highlight 1`);
                    let x = m[0];
                    let index = m.index;
                    let length = x.length;
                    let firstPart = text.substr(0, index);
                    let matchedPart = text.substr(index, length);
                    let remainder = text.substr(index + length);
                    if (firstPart.length > 0) {
                        this.spans.push({ text: firstPart, highlight: false });
                    }
                    this.spans.push({ text: matchedPart, highlight: true });
                    if (remainder.length > 0) {
                        this.spans.push({ text: remainder, highlight: false });
                    }
                } else {
                    //this.log.information(`highlight 2`);
                    this.spans.push({ text: text, highlight: true });
                }
            } else {
                this.spans.push({ text: text, highlight: false });
            }
        }
    }
    private prefixMatchAnyWord(searchText: string, text: string) {
        //console.log(`looking for ${searchText} in ${text}`);
        let search = this.removeDiacritics(searchText);
        let words = this.splitToWords(text);
        let result: string | null = null;
        let index = 0;
        let lastIndex = words.length - 1;
        for (let w of words) {
            if (index > 0) {
                this.spans.push({ text: " ", highlight: false });
            }
            let wt = this.removeDiacritics(w);
            if (wt.toLowerCase().startsWith(search.toLowerCase())) {
                if (wt.length === w.length) {
                    // remove diacritics did not alter the length of the string
                    let highlitPart = w.substr(0, search.length);
                    let remainder = w.substr(search.length);
                    this.spans.push({ text: highlitPart, highlight: true });
                    if (remainder.length > 0) {
                        this.spans.push({ text: remainder, highlight: false });
                    }
                } else {
                    this.spans.push({ text: w, highlight: true });
                }
                //break;
            } else {
                this.spans.push({ text: w, highlight: false });
                
            }
            ++index;
        }
    }
    private diacritics = {
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
    private removeDiacritics(text: string): string {
        let accentsMap = new Map<string, string>((<any>Object).entries(this.diacritics));

        return text.replace(/[^a-zA-Z0-9\s]+/g, (a) => {
            return accentsMap.get(a) || a;
        });
    }
    private splitToWords(text: string): string[] {
        //let parts = text.split(/([\b][^\s]+[\b])/);
        let parts = text.match(/\S+/g);
        let words: string[] = [];
        if (parts) {
            for (let p of parts) {
                if (p.trim().length > 0) {
                    words.push(p.trim())
                }
            }
        }
        //console.log(`split into ${words.join('|')}`);
        return words;
    }
    private createLocalSearchPattern(searchText: string): string {
        let r: string = "";
        let i = 0;
        let s = this.removeDiacritics(searchText);
        for (let c of s) {
            if (i > 0) {
                //r += '[^a-zA-Z0-9]?';
                r += '[^\\w]?';
            }
            r += c;
            ++i;
        }
        return r;
    }
}
