import { NG_VALUE_ACCESSOR } from "@angular/forms";
import { forwardRef, Component, Input, Output, EventEmitter } from "@angular/core";
import { ControlBase, InputControlBase } from "./controlbase.type";

@Component({
   selector: 'text-input',
   templateUrl: './text-input.component.html',
   styleUrls: ['./text-input.component.scss'],
   providers: [
      {
         provide: NG_VALUE_ACCESSOR,
         useExisting: forwardRef(() => TextInputControl),
         multi: true
      },
      {
         provide: ControlBase, useExisting: forwardRef(() => TextInputControl)
      }
   ]
})
export class TextInputControl extends InputControlBase {
   @Output() change = new EventEmitter();
   @Input() readonly: boolean = false;
   @Input() align: 'left' | 'centre' | 'right' = 'left';
   @Input() border: boolean = true;
   //private _alignright: boolean;
   //private _alignrightSet: boolean = false;
   //private _noborder: boolean;
   //private noborderSet: boolean = false;

   //public get noborder() {
   //   return this._noborder;
   //}
   //@Input() public set noborder(val: boolean) {
   //   this.noborderSet = true;
   //   this._noborder = val;
   //}
   //public get alignright() {
   //   return this._alignright;
   //}
   //@Input() public set alignright(val: boolean) {
   //   this._alignrightSet = true;
   //   this._alignright = val;
   //}
   constructor() {
      super();
      this.setReference("text");
      //console.log(`${this.getReference()} is TextInputControl`);
   }
   onChange() {
      super.onInput();
      this.change.emit();
   }
   getStyle() {
      let style = {};
      if (this.align === 'left') {
         style['text-align'] = 'left';
      } else if (this.align === 'centre') {
         style['text-align'] = 'center';
      } else {
         style['text-align'] = 'right';
      }
      //console.log(`getStyle(): _alignright = ${this._alignright}, _alignrightSet = ${this._alignrightSet}`);
      //if (this._alignrightSet === true) {
      //   style['text-align'] = 'right';
      //}
      if (this.border === false) {
         style['border'] = 'none';
      }
      if (this.isMobileDevice) {
         style['font-size'] = 'larger';
      }
      return style;
   }
   //shouldBeReadOnly() {
   //   if (this.readonly) {
   //      return true;
   //   }
   //   return false;
   //}
}

