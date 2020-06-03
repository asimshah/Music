
import { Component, Input, ChangeDetectionStrategy, ViewChild, TemplateRef, ContentChildren, ElementRef, QueryList, AfterContentInit, AfterViewInit } from '@angular/core';
import { animate, state, style, transition, trigger, AnimationEvent } from '@angular/animations';
import { SlidingPanelComponent, SlidingPanelStates } from './sliding-panel.component';
//import { ILoggingService, Severity } from '../../fastnet/controls/controls.types';
import { Logger } from '../../fastnet/controls/controlbase.type';
import { Severity } from '../../fastnet/core/core.types';



@Component({
   selector: 'sliding-panels',
   templateUrl: './sliding-panels.component.html',
   styleUrls: ['./sliding-panels.component.scss']//,
})
export class SlidingPanelsComponent implements AfterContentInit {
   private logger: Logger;
   private previousPanelNumber: number = -1;
   @ViewChild('sliderparent', { static: false }) parent: ElementRef;
   @ContentChildren(SlidingPanelComponent) inputPanels: QueryList<SlidingPanelComponent>;
   public panels: SlidingPanelComponent[] = [];
   @Input() activePanelNumber: number = 0;
   constructor() {
      this.logger = new Logger();
   }
   public ngAfterContentInit(): void {
      //this.inputPanels.forEach(panel => {
      //   console.log(panel.template);
      //});
      let temp = this.inputPanels.toArray();
      setTimeout(() => {
         this.panels = temp;// this.inputPanels.toArray();
         this.panels.forEach((p, i) => {
            p.ident = `child ${i}`;
         });
         this.setChildStates();
      }, 200);

   }

   changePanel(backward = false): number {
      let step = backward ? -1 : 1;
      this.previousPanelNumber = this.activePanelNumber;
      this.activePanelNumber = this.getNextPanel(step);
      this.setChildStates(backward);
      this.log(Severity.information, `previous ${this.previousPanelNumber} active ${this.activePanelNumber}`);
      return this.activePanelNumber;
   }
   setChildStates(backward = false) {
      for (let p of this.panels) {
         let index = this.panels.findIndex(x => x === p);
         let bringIn = index === this.activePanelNumber;
         if (bringIn) {
            // bring in current one in
            switch (p.getState()) {
               case SlidingPanelStates.outtoleft:
               case SlidingPanelStates.outtoright:
                  p.setState(backward ? SlidingPanelStates.infromleft : SlidingPanelStates.infromright);
                  break;
            }
         } else {
            // take it out
            switch (p.getState()) {
               case SlidingPanelStates.infromright:
               case SlidingPanelStates.infromleft:
                  p.setState(backward ? SlidingPanelStates.outtoright : SlidingPanelStates.outtoleft);
                  break;
            }
         }
      }
   }
   getNextPanel(step: number = 1): number {
      let np = this.activePanelNumber + step;
      if (step === 1) {
         if (np < this.panels.length) {
            return np;
         }
         return 0;
      } else {
         // step == -1
         if (np >= 0) {
            return np;
         }
         return this.panels.length - 1;
      }
   }
   private log(s: Severity, t: string) {
      this.logger.logMessage(s, t);
   }
}
