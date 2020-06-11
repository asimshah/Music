
import { Component, AfterViewInit, ViewChild, ElementRef, Renderer2, Input, AfterViewChecked, HostListener, OnDestroy, forwardRef, OnChanges, SimpleChanges, EventEmitter, Output, ViewChildren, QueryList } from '@angular/core';
import { InputControlBase, ControlBase } from './controlbase.type';

import { NG_VALUE_ACCESSOR } from '@angular/forms';
import { ListItem } from '../core/core.types';
import { TextInputControl } from './text-input.component';

@Component({
   selector: 'combo-box',
   templateUrl: './combo-box.component.html',
   styleUrls: ['./combo-box.component.scss'],
   providers: [
      {
         provide: NG_VALUE_ACCESSOR,
         useExisting: forwardRef(() => ComboBoxComponent),
         multi: true
      },
      {
         provide: ControlBase, useExisting: forwardRef(() => ComboBoxComponent)
      }
   ]
})
export class ComboBoxComponent extends InputControlBase implements AfterViewInit, AfterViewChecked, OnDestroy {
   private static allComboBoxes: ComboBoxComponent[] = [];
   showDropdown: boolean = false;
   @Input() maxRows: number = 5;
   @Input() items: ListItem<any>[] | string[];
   @Input() compact: boolean = false;
   @Input() aligncentre: boolean = false;
   @Input() dropdownonly: boolean = false;
   @Output() selectionchanged = new EventEmitter<ListItem<any> | string>();
   filteredItems: ListItem<any>[] | string[];
   @ViewChild('textBox', { static: false }) textBox: ElementRef;
   @ViewChild('droppanel', { static: false }) dropPanel: ElementRef;
   @ViewChild(TextInputControl, { static: false }) textInput: TextInputControl;
   inputElementText: string = '';
   public itemType: 'string' | 'listitem' = 'listitem';
   //private padding: number = 3.5;
   //private rowHeight = 20;
   private dropPanelWidth: number;
   private dropPanelHeight: number;
   private inputElementTextValid: boolean = false;
   private userTyping = false;

   constructor(private renderer: Renderer2) {
      super();
      this.setReference("combo-box");
      ComboBoxComponent.allComboBoxes.push(this);
      //console.log(`isMobileDevice = ${this.isMobileDevice()}`);
      //console.log(`constructor: ComboBoxComponent.allComboBoxes length now ${ComboBoxComponent.allComboBoxes.length}`);
   }
   writeValue(obj: any): void {
      super.writeValue(obj);
      if (obj) {
         this.inputElementText = this.itemType === 'string' ? obj : obj.name;
      }
   }
   ngOnDestroy() {
      let index = ComboBoxComponent.allComboBoxes.findIndex(x => x === this);
      if (index >= 0) {
         ComboBoxComponent.allComboBoxes.splice(index, 1);
      }
      //console.log(`ngOnDestroy(): ComboBoxComponent.allComboBoxes length now ${ComboBoxComponent.allComboBoxes.length}`);
   }
   @HostListener('window:resize', ['$event.target'])
   onResize() {
      //console.log(`onResize()`);
      this.computeDropPanelSize();
      if (this.showDropdown === true) {
         // it is about to open
         this.setDropPanelSize();
      }
   }
   ngAfterViewChecked() {
      //console.log(`ngAfterViewChecked()`);
      if (this.showDropdown === true) {
         // it is about to open
         this.setDropPanelSize();
      }

   }

   ngAfterViewInit() {
      //console.log(`ngAfterViewInit()`);
      //if (this.aligncentre) {
      //   this.textInput.alignright = true;
      //}
      this.setItemsType();
      this.matchItems();
      this.computeDropPanelSize();
      super.ngAfterViewInit();
   }
   //isDropdownOnly() {
   //   if (this.dropdownonly !== undefined && this.dropdownonly.length >= 0) {
   //      return true;
   //   }
   //   return true;
   //}
   onItemClick(e: Event, item: ListItem<any> | string) {
      //console.log(`onItemClick: ${JSON.stringify(item, null, 2)}`);
      this.selectItem(item);
   }
   onLocalInput(text: string) {
      //console.log(`onInput(): ${text}, inputElementText = ${this.inputElementText}`);
      super.onInput();
      this.userTyping = true;
      this.matchItems(text);

      if (this.showDropdown) {
         this.computeDropPanelSize();
         this.setDropPanelSize();
      } else {
         this.openDropdown(false);
      }
      this.inputElementTextValid = false;
      if (this.filteredItems.length === 1) {
         this.selectItem(this.filteredItems[0]);
         //if (this.itemType === 'string') {
         //   this.selectStringItem(<string>(this.filteredItems[0]));
         //} else {
         //   this.selectItem(<ListItem<any>>(this.filteredItems[0]));
         //}
         this.inputElementTextValid = true;
      }

   }
   onDownIconClick() {
      //console.log(`onDownIconClick()`);
      //this.showDropdown = !this.showDropdown;
      //if (this.showDropdown) {
      //    this.closeOthers();
      //}
      if (this.showDropdown) {
         this.closeDropDown();
      } else {
         this.openDropdown();

      }

   }
   isOpen(): boolean {
      return this.showDropdown === true;
   }
   stopEvent(e: Event) {
      e.stopPropagation();
      e.preventDefault();
   }
   @HostListener('document:click')
   public externalEvent() {
      //console.log(`externalEvent`);
      this.closeDropDown();
   }
   private openDropdown(initialState = true) {
      this.closeOthers();
      this.showDropdown = true;
      setTimeout(() => {
         if (initialState === true) {
            this.matchItems();
         }
         this.computeDropPanelSize();
         this.setDropPanelSize();
         let currentElement = this.findCurrentDropItem();
         if (currentElement !== null) {
            currentElement.scrollIntoView();
         }
      }, 0);
   }
   private closeDropDown() {
      if (this.userTyping === true) {
         this.selectItem(this.value);
      }
      setTimeout(() => {
         this.showDropdown = false;
      }, 0);

   }
   private closeOthers() {
      for (let control of ComboBoxComponent.allComboBoxes) {
         if (control !== this) {
            if (control.showDropdown === true) {
               control.closeDropDown();
            }
         }
      }
   }
   private selectItem(item: ListItem<any> | string) {
      this.inputElementText = '';
      setTimeout(() => {
         if (this.itemType === 'string') {
            this.inputElementText = <string>item;
         } else {
            this.inputElementText = (<ListItem<any>>item).name;
         }
         this.writeValue(item);
         this.matchItems();
         this.userTyping = false;
         //console.log(`selectItem: ${JSON.stringify(item)}`);
         this.selectionchanged.emit(item);
         this.closeDropDown();
      }, 0);
   }
   private matchItems(filter: string = '') {
      if (this.itemType === 'string') {
         let items = <string[]>this.items;
         this.filteredItems = items.filter(x => x.toLowerCase().startsWith(filter.toLowerCase()));
      } else {
         let items = <ListItem<any>[]>this.items;
         this.filteredItems = items.filter(x => x.name.toLowerCase().startsWith(filter.toLowerCase()));
      }
   }
   private computeDropPanelSize() {
      this.dropPanelWidth = this.textBox.nativeElement.clientWidth;
      let di = document.querySelector('.drop-item');
      let rows = Math.min(this.maxRows, this.filteredItems.length);
      if (di) {
         this.dropPanelHeight = (rows * di.clientHeight);// + (this.padding * 2);
      }
   }
   private setDropPanelSize() {
      this.renderer.setStyle(this.dropPanel.nativeElement, 'width', `${this.dropPanelWidth}px`);
      this.renderer.setStyle(this.dropPanel.nativeElement, 'height', `${this.dropPanelHeight}px`);
      //console.log(`panel size reset`);
   }
   private findCurrentDropItem(): HTMLElement | null {
      if (this.value) {
         let currentText = this.value.name;
         let items = this.dropPanel.nativeElement.querySelectorAll('.drop-item');
         let r = null;
         for (let item of items) {
            let text = item.innerHTML;
            if (currentText === text) {
               r = item;
               break;
            }
         }
         return r!;
      }
      return null;
   }
   private setItemsType() {
      if (!this.items || this.items.length == 0) {
         console.error("combo-box - no items defined");
      }
      let first = this.items[0];
      if (typeof first === "string") {
         this.itemType = 'string';
      }
   }
}
