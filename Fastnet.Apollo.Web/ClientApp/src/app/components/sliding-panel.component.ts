import { Component, Input, ViewChild, TemplateRef } from "@angular/core";
//import { SlidingPanelStates } from "./sliding-panels.component";

export enum SlidingPanelStates {
    infromleft,
    infromright,
    outtoleft,
    outtoright
}

@Component({
    selector: 'sliding-panel',
    templateUrl: './sliding-panel.component.html',
    styleUrls: ['./sliding-panel.component.scss']
    //template: `<ng-template>
    //                <div [@slide]="getStateName()" >
    //                    <ng-content></ng-content>
    //                </div>
    //           </ng-template>`,
    ////template: `<ng-template>
    ////                <div  >
    ////                    <ng-content></ng-content>
    ////                </div>
    ////           </ng-template>`,
    //,
    //animations: [
    //    trigger('slide', [
    //        state(slidingPanelStates[slidingPanelStates.infromleft], style({ height: '100%', left: 0 })),
    //        state(slidingPanelStates[slidingPanelStates.infromright], style({ height: '100%', left: 0 })),
    //        state(slidingPanelStates[slidingPanelStates.outtoleft], style({ height: '100%', left: '-100%' })),
    //        state(slidingPanelStates[slidingPanelStates.outtoright], style({ height: '100%', left: '100%' })),
    //        transition('outtoleft => infromleft, outtoright => infromleft', [style({ height: '100%', left: '-100%' }), animate(500)]),
    //        transition('outtoleft => infromright, outtoright => infromright', [style({ height: '100%', left: '100%' }), animate(500)]),
    //        transition('infromleft => outtoleft, infromright => outtoleft', [style({ height: '100%', left: '0' }), animate(500)]),
    //        transition('infromleft => outtoright, infromright => outtoright', [style({ height: '100%', left: '0' }), animate(500)])//,
    //    ])
    //]
})
export class SlidingPanelComponent {
    private state: SlidingPanelStates = SlidingPanelStates.outtoleft;
    private previousState: SlidingPanelStates = SlidingPanelStates.outtoleft;
    ident: string = "";
    private classes: string[] = [SlidingPanelStates[this.state]];
    @Input() public label: string;
    @ViewChild(TemplateRef, { static: false }) template: TemplateRef<any>;
    constructor() {

    }
    getState() {
        return this.state;// this.state;
    }
    getStateName() {
        return SlidingPanelStates[this.state];
    }
    setState(state: SlidingPanelStates) {
        this.previousState = this.state;
        this.state = state;
        this.setClasses();
    }
    getClasses(): string[] {
        return this.classes;
    }    
    //onTransitionEnd(e: Event) {
    //    console.log(`${this.ident}: onTransitionEnd()`)
    //    switch (this.state) {
    //        case SlidingPanelStates.infromleft:
    //        case SlidingPanelStates.infromright:
    //            break;
    //        case SlidingPanelStates.outtoleft:
    //        case SlidingPanelStates.outtoright:
    //            this.parkPanel();
    //            break;
    //    }
    //}
    private setClasses() {
        if (this.previousState !== this.state) {
            this.classes = [SlidingPanelStates[this.state]];
        }
    }
    //private parkPanel() {
    //    this.classes = ['parked'];
    //}
}
