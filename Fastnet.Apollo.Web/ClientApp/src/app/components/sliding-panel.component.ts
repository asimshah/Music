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
    private setClasses() {
        if (this.previousState !== this.state) {
            this.classes = [SlidingPanelStates[this.state]];
        }
    }
}
