import { Component, OnInit } from '@angular/core';
import { Work, PopularMusicFileTEO, PopularAlbumTEO } from '../../../shared/catalog.types';
import { LibraryService } from '../../../shared/library.service';
import { TagEditorComponent, teoModel, teValidationMessage } from '../../tag-editor.component';

class albumModel extends teoModel {
   public artist: string;
   public fileList: PopularMusicFileTEO[] = [];
}

@Component({
   selector: 'popular-tag-editor',
   templateUrl: './popular-tag-editor.component.html',
   styleUrls: ['./popular-tag-editor.component.scss']
})
export class PopularTagEditorComponent extends TagEditorComponent {
   public am: albumModel = null;
   private albumTeo: PopularAlbumTEO;
   private albumModelJson: string;
   constructor(library: LibraryService) {
      super(library);
   }
   public async initialise(work: Work) {
      console.log("PopularTagEditorComponent: initialise()");
      await this.loadWork(work);
   }
   public hasChanges() {
      let temp = JSON.stringify(this.am);
      return temp !== this.albumModelJson;
   }
   public isReady() {
      return this._isReady && this.am !== null;
   }
   onEditTracks() {
      this.musicfileEditor.open(this.am.fileList, (r) => {
      });
   }
   getValidationMessages(key: string) {
      let messages: teValidationMessage[] = [];
      switch (key) {
         case "album":
            if (this.albumTeo.albumTag.values.length > 1) {
                messages.push(new teValidationMessage(`multiple album names available`, true));
            }
            break;
         case "year":
            if (this.albumTeo.yearTag.values.length > 1) {
               messages.push(new teValidationMessage(`multiple years available`, true));
            }
            break;
      }
      return messages;
   }
   async onSaveChanges() {
      this.isBusy = true;
      await this.updateAlbum();
      this.isBusy = false;
      this.popup.close(true);
   }
   async onCancel() {
      this.am = JSON.parse(this.albumModelJson);
   }
   private async loadWork(work: Work) {
      this.am = null;
      this.albumTeo = await this.library.editWork(work.id);
      this.am = new albumModel();
      this.am.pathToMusicFiles = this.albumTeo.pathToMusicFiles;
      this.am.fileCount = this.albumTeo.trackList.length;
      this.am.artist = this.getSingleSelected(this.albumTeo.artistTag.values);
      this.am.year = this.getSingleSelected(this.albumTeo.yearTag.values);
      this.am.album = this.getSingleSelected(this.albumTeo.albumTag.values);
      this.am.fileList = this.albumTeo.trackList;
      this.albumModelJson = JSON.stringify(this.am);
   }
   private async updateAlbum() {
      this.setSingleValue(this.albumTeo.albumTag.values, this.am.album);
      this.setSingleValue(this.albumTeo.yearTag.values, this.am.year);
      await this.library.updatePopularAlbum(this.albumTeo);
      this.albumModelJson = JSON.stringify(this.am);
      console.log(`${JSON.stringify(this.am, null, 4)}`);
   }
}
