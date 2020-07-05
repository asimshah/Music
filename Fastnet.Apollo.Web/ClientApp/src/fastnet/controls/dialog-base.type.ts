import { Input, ContentChildren, QueryList } from "@angular/core";
import { ControlBase } from "./controlbase.type";
import { ValidationResult, ValidationContext } from "./controls.types";

export class DialogValidationResult {
   control: ControlBase;
   validationResult: ValidationResult;
}
export class DialogBase {
   @Input() columns: number = 1;
   @ContentChildren(ControlBase) controls: QueryList<ControlBase>;
   public isMobileDevice() {
      if (ControlBase.deviceSensitivity) {
         return matchMedia("only screen and (max-width: 760px)").matches;
      }
      return false;
   }

   /** @description validates all controls in this dialog by calling Validate() on each in turn
    * and returns false if any one or more is invalid
    */
   async isValid() {
      let results = await this.validate();
      let somethingIsInvalid = results.some(x => x.validationResult.valid === false);
      return !somethingIsInvalid;
   }
   /** @description validates all controls in this dialog by calling Validate() on each in turn
    * and returns false if there is any control that is invalid. Also sets focus to that control
    */
   async validateAll() {
      let results = await this.validate();
      let r = results.length == 0; // true if there are no errors
      if (!r) {
         let c = results[0].control;
         c.focus();
      }
      return r;
   }
   /** @description validates all controls in this dialog by calling Validate() on each in turn
    * and returns an array of DialogValidationResult for each control that is invalid
    *
    */
   validate() {
      return new Promise<DialogValidationResult[]>(async resolve => {
         let results: DialogValidationResult[] = [];
         for (let control of this.controls.toArray()) {
            let vr = await control.validate(ValidationContext.DialogValidation);
            if (!vr.valid) {
               let dvr = new DialogValidationResult();
               dvr.control = control;
               dvr.validationResult = vr;
               results.push(dvr);
            }
         }
         resolve(results);
      });
   }
   reset() {
      for (let control of this.controls.toArray()) {
         control.reset();
      }
   }
}
