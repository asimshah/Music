import { Component, OnInit, ViewChild, ElementRef, AfterContentInit, AfterViewInit } from '@angular/core';
import { PopupDialogComponent } from '../../fastnet/controls/popup-dialog.component';
import { DialogResult } from '../../fastnet/core/core.types';
import { Dictionary } from '../../fastnet/core/dictionary.types';
import { ValidationMethod } from '../../fastnet/controls/controlbase.type';
import { ValidationResult, ValidationContext } from '../../fastnet/controls/controls.types';

@Component({
   selector: 'dialog-test',
   templateUrl: './dialog-test.component.html',
   styleUrls: ['./dialog-test.component.scss']
})
export class DialogTestComponent implements OnInit, AfterViewInit {
   @ViewChild(PopupDialogComponent, { static: false }) popupDialog: PopupDialogComponent;
   firstName = "";
   lastName = "";
   public validators: Dictionary<ValidationMethod> = new Dictionary<ValidationMethod>();
   private logValidations = false;
   constructor() { }
   open() {
      this.popupDialog.open((dr: DialogResult) => {
         console.log(`${DialogResult[dr]}`);
      });
   }
   ngOnInit() {
      this.addValidators();
   }

   ngAfterViewInit() {
      //console.log(`${this.popupDialog.reference} control count is ${this.popupDialog.controls.length}`);
   }
   onCancel() {
      this.popupDialog.close(DialogResult.cancel);
   }
   onOK() {
      this.popupDialog.close(DialogResult.ok);
   }
   async getValidityState() {
      let r = await this.popupDialog.isValid();
      return r ? 'valid' : 'invalid';
   }
   private logValidationField(fieldName: string) {
      if (this.logValidations) {
         console.log(`validating field ${fieldName}`);
      }
   }
   private addValidators() {

      this.validators.add("firstName", (vc, v) => {
         this.logValidationField("firstName");
         return this.firstNameValidatorAsync(vc, v);
      });
      this.validators.add("lastName", (vc, v) => {
         this.logValidationField("lastName");
         return this.lastNameValidatorAsync(vc, v);
      });
   }
   private firstNameValidatorAsync(cs: ValidationContext, val: string): Promise<ValidationResult> {
      return new Promise<ValidationResult>((resolve) => {
         //let vr = cs.validationResult;
         //let text = cs.value || "";
         let vr = new ValidationResult();
         let text = <string>val || "";
         if (text.length === 0) {
            vr.valid = false;
            vr.message = `a First Name is required`;
         }
         //resolve(cs.validationResult);
         resolve(vr);
      });
   }
   //private lastNameValidatorAsync(cs: ControlState): Promise<ValidationResult> {
   private lastNameValidatorAsync(cs: ValidationContext, val: string): Promise<ValidationResult> {
      return new Promise<ValidationResult>((resolve) => {
         //let vr = cs.validationResult;
         //let text = cs.value || "";
         let vr = new ValidationResult();
         let text = val || "";
         if (text.length === 0) {
            vr.valid = false;
            vr.message = `a Last Name is required`;
         }
         //resolve(cs.validationResult);
         resolve(vr);
      });
   }
}
