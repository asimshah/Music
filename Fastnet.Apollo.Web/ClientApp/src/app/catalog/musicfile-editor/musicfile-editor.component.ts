import { Component, OnInit, ViewChild } from '@angular/core';
import { PopupDialogComponent } from '../../../fastnet/controls/popup-dialog.component';
import { MusicFileTEO } from "../../shared/catalog.types";
import { DialogResult } from '../../../fastnet/core/core.types';

export class musicFileModel {
    public trackNumber: number;
    public title: string;
    public file: string;
}

@Component({
    selector: 'musicfile-editor',
    templateUrl: './musicfile-editor.component.html',
    styleUrls: ['./musicfile-editor.component.scss']
})
export class MusicfileEditorComponent implements OnInit {
    @ViewChild(PopupDialogComponent, { static: false }) popup: PopupDialogComponent;
    isReady = false;
    stringToStrip: string = "";
    musicFiles: MusicFileTEO[] = [];
    files: musicFileModel[] = [];
    constructor() { }

    ngOnInit() {
    }
    open(files: MusicFileTEO[], onClose: (r: DialogResult) => void) {
        this.musicFiles = files;
        this.loadMusicFiles();
        this.isReady = true;
        this.popup.open((r: DialogResult) => {
            this.isReady = false;
            onClose(r);
        });
    }
    onOK() {
        this.updateMusicFiles();
        this.popup.close(DialogResult.ok);
    }
    onClose() {
        this.popup.close(DialogResult.cancel);
    }
    onRenumberTracks() {
        let n = 0;
        for (let f of this.files) {
            f.trackNumber = ++n;
            //f.trackNumberTag.singleValue = `${++n}`;
            //f.trackNumberTag.valid = true;
        }
    }
    onLeadingStrip() {
        console.log(`strip leading ${this.stringToStrip}`);
        let rtex = new RegExp(`^${this.stringToStrip}`, "i");
        for (let f of this.files) {
            f.title = f.title.replace(rtex, "").trim();
        }
    }
    private loadMusicFiles() {
        this.files = [];
        for (let f of this.musicFiles) {
            let nf = new musicFileModel();
            nf.file = f.file;
            nf.title = f.titleTag.values[0].value;
            nf.trackNumber = parseInt(f.trackNumberTag.values[0].value);
            this.files.push(nf);
        }
    }
    private updateMusicFiles() {
        let i = 0;
        for (let f of this.files) {
            this.musicFiles[i].titleTag.values[0].value = `${f.title}`;
            this.musicFiles[i].trackNumberTag.values[0].value = `${f.trackNumber}`;
            ++i;
        }
    }
}
