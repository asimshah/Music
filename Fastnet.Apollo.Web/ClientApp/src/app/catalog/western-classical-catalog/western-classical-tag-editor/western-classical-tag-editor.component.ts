import { Component } from '@angular/core';
import { Performance,  WesternClassicalMusicFileTEO, TagValue, WesternClassicalAlbumTEO } from "../../../shared/catalog.types";
import { LibraryService } from '../../../shared/library.service';
import { LoggingService } from '../../../shared/logging.service';

import { DialogResult } from '../../../../fastnet/core/core.types';
import { TagEditorComponent, teValidationMessage } from '../../tag-editor.component';


export class performanceModel {
   public id: number;
   public composer: string;
   public composition: string;
   public orchestras: TagValue[] = [];
   public conductors: TagValue[] = [];
   public performers: TagValue[] = [];
   public movementList: WesternClassicalMusicFileTEO[] = [];
}
@Component({
   selector: 'wc-tag-editor',
   templateUrl: './western-classical-tag-editor.component.html',
   styleUrls: ['./western-classical-tag-editor.component.scss']
})
export class WesternClassicalTagEditorComponent extends TagEditorComponent {
   currentModel: number = 0;
   public teo: WesternClassicalAlbumTEO;
   public pmList: performanceModel[] = [];
   private performanceModelJson: string;
   constructor(library: LibraryService, private log: LoggingService) {
      super(library);
   }
   public isReady() {
      return this._isReady && this.pmList.length > 0;
   }
   public async initialise(performance: Performance) {
      console.log("WesternClassicalTagEditorComponent: initialise()");
      await this.loadPerformances(performance);
      let i = this.pmList.findIndex(x => x.id === performance.id);
      this.currentModel = i;//0;
   }
   onCancel() {
      this.pmList = JSON.parse(this.performanceModelJson);
   }
   async onSaveChanges() {
      this.isBusy = true;
      await this.updatePerformance();
      this.isBusy = false;
      this.popup.close(true);
   }
   getPathToMusicFiles() {
      return this.teo.pathToMusicFiles;
   }
   getAlbumName() {
      return this.getSingleSelected(this.teo.albumTag.values);
   }
   getPerformanceCount() {
      return this.pmList.length;
   }
   getPerformanceIdentifier() {
      return `performance ${this.currentModel + 1} of ${this.getPerformanceCount()}`;
   }
   getFileCount() {
      let text = `${this.teo.trackList.length} files`;
      if (this.pmList.length > 1) {
         text = `${text} in ${this.pmList.length} performances`;
      }
      return text;
   }
   onPrevious() {
      if (this.currentModel > 0) {
         this.currentModel--;
      }
   }
   onNext() {
      if (this.currentModel < this.getPerformanceCount() - 1) {
         this.currentModel++;
      }
   }
   hasChanges() {
      let temp = JSON.stringify(this.pmList);
      return temp !== this.performanceModelJson;
   }

   getValidationMessages(key: string) {
      let performanceTEO = this.teo.performanceList[this.currentModel];
      let messages: teValidationMessage[] = [];
      switch (key) {
         case "conductor":
            if (performanceTEO.conductorTag.values.length > 1) {
               messages.push(new teValidationMessage(`multiple conductor names available`, true));
            }
            break;
         case "composer":
            if (performanceTEO.composerTag.values.length > 1) {
               messages.push(new teValidationMessage(`multiple composer names available`, true));
            }
            break;
         case "composition":
            if (performanceTEO.compositionTag.values.length > 1) {
               messages.push(new teValidationMessage(`multiple composition names available`, true));
            }
            break;
         //case "album":
         //   //if (performanceTEO.albumTag.values.length > 1) {
         //   //    messages.push(new teValidationMessage(`multiple album names available`, true));
         //   //}
         //   break;
         case "orchestra":
            if (performanceTEO.orchestraTag.values.length > 1) {
               messages.push(new teValidationMessage(`multiple orchestra names available`, true));
            }
            break;
         //case "year":
         //   if (performanceTEO.yearTag.values.length > 1) {
         //      messages.push(new teValidationMessage(`multiple years available`, true));
         //   }
         //   break;
         default:
            break;
      }
      return messages;
   }
   editPerformers() {
      let pm = this.getCurrentPerformance();
      this.multipleValuesEditor.caption = "Select Performers";
      this.multipleValuesEditor.open(pm.performers, false, (r) => {
         this.log.information(`returned from performers editor`);
         if (r.dialogResult === DialogResult.ok) {
            //console.log(`has changes = ${this.hasChanges()}`);
         }
      });

   }
   onEditMovements() {
      let pm = this.getCurrentPerformance();
      this.musicfileEditor.open(pm.movementList, (r) => {
         console.log(`${JSON.stringify(pm.movementList, null, 3)}`);
      });
   }
   private getCurrentPerformance(): performanceModel {
      return this.pmList[this.currentModel];
   }
   private async loadPerformances(performance: Performance) {
      this.teo = await this.library.editPerformance(performance.id);
      this.pmList.length = 0;
      for (let pteo of this.teo.performanceList) {
         let pm = new performanceModel();
         pm.id = pteo.performanceId;
         pm.composer = this.getSingleSelected(pteo.composerTag.values);
         pm.composition = this.getSingleSelected(pteo.compositionTag.values);
         pm.orchestras = pteo.orchestraTag.values;
         pm.conductors = pteo.conductorTag.values;
         pm.performers = pteo.performerTag.values;
         pm.movementList = pteo.movementList;
         this.pmList.push(pm);
      }
      this.performanceModelJson = JSON.stringify(this.pmList);
   }
   getMultipleValuesCSV(values: TagValue[]) {
      return this.getCSV(values.filter((f) => f.selected).map((x) => x.value));
   }
   private async updatePerformance() {
      for (let i = 0; i < this.teo.performanceList.length; ++i) {
         let pteo = this.teo.performanceList[i];
         let pm = this.pmList[i];
         this.setSingleValue(pteo.composerTag.values, pm.composer);
         this.setSingleValue(pteo.compositionTag.values, pm.composition);
         this.setMultipleValues(pteo.orchestraTag.values, pm.orchestras.filter(x => x.selected).map(m => m.value));
         this.setMultipleValues(pteo.conductorTag.values, pm.conductors.filter(x => x.selected).map(m => m.value));
         this.setMultipleValues(pteo.performerTag.values, pm.performers.filter(x => x.selected).map(m => m.value));
         // remaining properties (performers and filelist) are byref and so are already up to date ...
      }
      await this.library.updateWesternClassicalAlbum(this.teo);
      this.performanceModelJson = JSON.stringify(this.pmList);
   }
   //private capitalise(text: string) {
   //   return text.charAt(0).toUpperCase() + text.slice(1).toLowerCase();
   //}
   private getCSV(values: string[]) {
      let r = values.join(', ');
      return r;
   }
}
