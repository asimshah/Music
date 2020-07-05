import { Component, OnInit, TemplateRef, ContentChild, Input, ViewEncapsulation, EmbeddedViewRef, AfterViewInit, AfterContentChecked, AfterViewChecked, AfterContentInit, ViewChild, ViewContainerRef, ElementRef, Output, EventEmitter, Renderer2 } from '@angular/core';
import { ControlBase } from './controlbase.type';

/**
 * display an array of items in a user designed manner from which a single item can be selected
 * the header for the list is shown using <ng-template #headerTemplate>
 * each item is shown using <ng-template #itemTemplate let-item> where let-item allows access to an individual item (from [items])
 * Bother the header template and the item template are set as "display: grid" with "grid-template-columns" set to "1fr"
 * [maxRows] (optional) limits the vertical height to show maxRows rows (with a scroll basr if required)
 * [items] set this to an array of objects
 * gridTemplateColumns css string such as "1fr 100px 100px", default is "1fr" (use only nFr and fixed width columns to ensure alignment of header and items parts)
 * (selectedItemChanged) raised whenever the selected changes - $event contains the selected item (also avaliable as the selectedItem property)
 * #itemTemplate variables are let-item, let-xxx="index", let-xxx="even", let-xxx="odd", let-xxx="first" and let-xxx="last"
 * */
@Component({
   selector: 'list-view',
   templateUrl: './list-view.component.html',
   styleUrls: ['./list-view.component.scss']
})
export class ListViewComponent extends ControlBase implements AfterViewChecked {
   @ContentChild('headerTemplate', { static: false, read: TemplateRef }) headerTemplate: TemplateRef<any>; // used by [ngTemplateOutlet]
   @ContentChild('itemTemplate', { static: false, read: TemplateRef }) itemTemplate: TemplateRef<any>; // used by [ngTemplateOutlet]
   @ViewChild('bodycontainer', { static: false }) bodyContainer: ElementRef;
   @ViewChild('headercontainer', { static: false }) headerContainer: ElementRef;
   @Input() items: Array<any> = [];
   @Input() gridTemplateColumns: string = '1fr';
   @Input() maxRows;
   @Output() selectedItemChanged = new EventEmitter<any>();
   selectedItem: any;

   constructor(private renderer: Renderer2) { super(); }

   getGridTemplateColumns() {
      return this.gridTemplateColumns;
   }
   onListItemClick(item: any) {
      this.selectedItem = item;
      this.selectedItemChanged.next(item);
   }

   ngAfterViewChecked() {
      if (this.maxRows) {
         if (this.maxRows < this.items.length) {
            if (!this.isMobileDevice()) {
               this.renderer.setStyle(this.headerContainer.nativeElement, "padding-right", `20px`);
            }
            let itemDiv: HTMLElement = this.bodyContainer.nativeElement.querySelector('.list-view-item');
            var itemHeight = itemDiv.clientHeight;
            let panelHeight = this.maxRows * itemHeight;
            if (this.bodyContainer.nativeElement.clientHeight !== panelHeight) {
               this.renderer.setStyle(this.bodyContainer.nativeElement, "height", `${panelHeight}px`);
            }
         } else {
            this.renderer.removeStyle(this.bodyContainer.nativeElement, "height");
            this.renderer.removeStyle(this.headerContainer.nativeElement, "padding-right");
         }
      }
   }
}
