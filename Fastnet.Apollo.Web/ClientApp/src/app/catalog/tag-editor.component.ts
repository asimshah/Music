import { PopupDialogComponent } from "../../fastnet/controls/popup-dialog.component";
import { MultipleValueEditorComponent } from "./multiple-values-editor/multiple-values-editor.component";
import { MusicfileEditorComponent } from "./musicfile-editor/musicfile-editor.component";
import { ViewChild } from "@angular/core";
import { LibraryService } from "../shared/library.service";
import { TagValue, Work, BaseEntity } from "../shared/catalog.types";

export class teValidationMessage {
   constructor(public text: string, public isError: boolean = false) {

   }
}
export class teoModel {
   public pathToMusicFiles: string;
   public fileCount = 0;
   public album: string;
   public year: string;
}
export abstract class TagEditorComponent {
   public isBusy = false;
   protected _isReady = false;
   @ViewChild(PopupDialogComponent, { static: false }) popup: PopupDialogComponent;
   @ViewChild(MultipleValueEditorComponent, { static: false }) multipleValuesEditor: MultipleValueEditorComponent;
   @ViewChild(MusicfileEditorComponent, { static: false }) musicfileEditor: MusicfileEditorComponent;
   public abstract hasChanges(): boolean;
   public abstract initialise(entity: BaseEntity): Promise<void>;
   public abstract isReady(): boolean;
   constructor(protected library: LibraryService) {

   }
   public async open(entity: BaseEntity, onClose: (hasChanges: boolean) => void) {
      this._isReady = false;
      await this.initialise(entity);
      this._isReady = true;
      this.popup.open((hasChanges: boolean) => {
         if (onClose) {
            console.log(`closed with changes = ${hasChanges}`);
            onClose(hasChanges);
         }
      })
   }
   public onClose() {
      this.popup.close(this.hasChanges());
   }   
   protected getSingleSelected(values: TagValue[]) {
      let candidates = values.filter((v) => v.selected);
      if (candidates.length === 1) {
         return candidates[0].value;
      } else {
         return "";
      }
   }
   protected setSingleValue(values: TagValue[], newValue: string) {
      values.length = 0;
      if (newValue && newValue !== null && newValue !== "") {
         let nv = new TagValue();
         nv.selected = true;
         nv.value = newValue;
         values.push(nv);
      }
   }
   protected setMultipleValues(values: TagValue[], newValues: string[]) {
      for (let v of values) {
         v.selected = false;
      }
      for (let s of newValues) {
         let item = values.find(x => x.value.toLowerCase() === s.toLowerCase());
         if (item) {
            item.selected = true;
         }
      }
   }
}
