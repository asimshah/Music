import { Component, OnInit, Input, ElementRef, ChangeDetectorRef, AfterViewInit } from '@angular/core';

@Component({
   selector: 'tooltip-content',
   templateUrl: './tooltip-content.component.html',
   styleUrls: ['./tooltip-content.component.scss']
})
export class TooltipContentComponent implements AfterViewInit {
   @Input() hostElement: HTMLElement;
   @Input() content: string;
   @Input() placement: "top" | "bottom" | "left" | "right" | "top-left" | "bottom-left" | "left-top" | "left-bottom"  = "bottom";
   //"top" | "bottom" | "left" | "right" | "top-left" | "bottom-left" | "left-top" | "left-bottom" 
   //@Input() animation: boolean = false;
   private animation: boolean = false;
   @Input() positionOffset = 20;
   @Input() delay: number = 0;
   // -------------------------------------------------------------------------
   // Properties
   // -------------------------------------------------------------------------

   top: number = -100000;
   left: number = -100000;
   isIn: boolean = false;
   isFade: boolean = false;
   constructor(private element: ElementRef,
      private cdr: ChangeDetectorRef) {
   }
   ngAfterViewInit(): void {
      this.show();
      this.cdr.detectChanges();
   }
   show(): void {
      if (!this.hostElement)
         return;
      //this.showDimensions(this.hostElement);
      if (this.delay > 0) {
         let targetElement: HTMLElement = this.element.nativeElement.children[0];
         targetElement.style.transitionDelay = `${this.delay}ms`;// "1500ms";
      }
      const p = this.positionElements(this.hostElement, this.element.nativeElement.children[0], this.placement);
      //console.log(`placement ${this.placement}, ${JSON.stringify(p)}`);
      this.top = p.top;// - 20;
      this.left = p.left;
      this.isIn = true;
      if (this.animation)
         this.isFade = true;
   }

   hide(): void {
      if (this.delay > 0) {
         let targetElement: HTMLElement = this.element.nativeElement.children[0];
         targetElement.style.transitionDelay = "unset";
      }
      this.top = -100000;
      this.left = -100000;
      this.isIn = true;
      if (this.animation)
         this.isFade = false;
   }
   private positionElements(hostEl: HTMLElement, targetEl: HTMLElement, positionStr: string, appendToBody: boolean = false): { top: number, left: number } {
      let positionStrParts = positionStr.split("-");
      let pos0 = positionStrParts[0];
      let pos1 = positionStrParts[1] || "center";
      let hostElPos = appendToBody ? this.offset(hostEl) : this.position(hostEl);
      let targetElWidth = targetEl.offsetWidth;
      let targetElHeight = targetEl.offsetHeight;
      let shiftWidth: any = {
         center: function (): number {
            return hostElPos.left + hostElPos.width / 2 - targetElWidth / 2;
         },
         left: function (): number {
            return hostElPos.left;
         },
         right: function (): number {
            return hostElPos.left + hostElPos.width;
         }
      };

      let shiftHeight: any = {
         center: function (): number {
            return hostElPos.top + hostElPos.height / 2 - targetElHeight / 2;
         },
         top: function (): number {
            return hostElPos.top;
         },
         bottom: function (): number {
            return hostElPos.top + hostElPos.height;
         }
      };

      let targetElPos: { top: number, left: number };
      //console.log(`pos0 = ${pos0}, pos1 = ${pos1}`);
      switch (pos0) {
         case "right":
            targetElPos = {
               top: shiftHeight[pos1](),
               left: shiftWidth[pos0]() + this.positionOffset
            };
            break;

         case "left":
            targetElPos = {
               top: shiftHeight[pos1](),
               left: hostElPos.left - targetElWidth - this.positionOffset
            };
            break;

         case "bottom":
            targetElPos = {
               top: shiftHeight[pos0]() + this.positionOffset ,
               left: shiftWidth[pos1]()
            };
            break;

         default:
            targetElPos = {
               top: hostElPos.top - targetElHeight - this.positionOffset,
               left: shiftWidth[pos1]()
            };
            break;
      }

      return targetElPos;
   }

   private position(nativeEl: HTMLElement): { width: number, height: number, top: number, left: number } {
      let offsetParentBCR = { top: 0, left: 0 };
      const elBCR = this.offset(nativeEl);
      const offsetParentEl = this.parentOffsetEl(nativeEl);
      if (offsetParentEl !== window.document) {
         offsetParentBCR = this.offset(offsetParentEl);
         offsetParentBCR.top += offsetParentEl.clientTop - offsetParentEl.scrollTop;
         offsetParentBCR.left += offsetParentEl.clientLeft - offsetParentEl.scrollLeft;
      }

      const boundingClientRect = nativeEl.getBoundingClientRect();
      return {
         width: boundingClientRect.width || nativeEl.offsetWidth,
         height: boundingClientRect.height || nativeEl.offsetHeight,
         top: elBCR.top - offsetParentBCR.top,
         left: elBCR.left - offsetParentBCR.left
      };
   }

   private offset(nativeEl: any): { width: number, height: number, top: number, left: number } {
      const boundingClientRect = nativeEl.getBoundingClientRect();
      return {
         width: boundingClientRect.width || nativeEl.offsetWidth,
         height: boundingClientRect.height || nativeEl.offsetHeight,
         top: boundingClientRect.top + (window.pageYOffset || window.document.documentElement.scrollTop),
         left: boundingClientRect.left + (window.pageXOffset || window.document.documentElement.scrollLeft)
      };
   }

   private getStyle(nativeEl: HTMLElement, cssProp: string): string {
      if ((nativeEl as any).currentStyle) // IE
         return (nativeEl as any).currentStyle[cssProp];

      if (window.getComputedStyle)
         return (window.getComputedStyle(nativeEl) as any)[cssProp];

      // finally try and get inline style
      return (nativeEl.style as any)[cssProp];
   }

   private isStaticPositioned(nativeEl: HTMLElement): boolean {
      return (this.getStyle(nativeEl, "position") || "static") === "static";
   }

   private parentOffsetEl(nativeEl: HTMLElement): any {
      let offsetParent: any = nativeEl.offsetParent || window.document;
      while (offsetParent && offsetParent !== window.document && this.isStaticPositioned(offsetParent)) {
         offsetParent = offsetParent.offsetParent;
      }
      return offsetParent || window.document;
   }
   private showDimensions(element: HTMLElement) {
      let offsetRect = this.offset(element);
      let positionRect = this.position(element);
      console.log(`offsetRect: ${JSON.stringify(offsetRect)}`);
      console.log(`position: ${JSON.stringify(positionRect)}`);
   }
}