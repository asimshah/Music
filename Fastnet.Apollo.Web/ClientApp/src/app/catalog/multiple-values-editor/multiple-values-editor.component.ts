import { Component, OnInit, HostBinding, Input, Output, ViewChild } from '@angular/core';
import { Subscription, Subject } from 'rxjs';
import { PopupDialogComponent } from '../../../fastnet/controls/popup-dialog.component';
import { DialogResult } from '../../../fastnet/core/core.types';
import { TagValue } from "../../shared/catalog.types";

export class MultiplevalueEditorResult {
    dialogResult: DialogResult;
    choices: TagValue[] = [];
}


@Component({
    selector: 'multiple-values-editor',
    templateUrl: './multiple-values-editor.component.html',
    styleUrls: ['./multiple-values-editor-editor.component.scss']
})
export class MultipleValueEditorComponent implements OnInit {
    @ViewChild(PopupDialogComponent, { static: false }) popup: PopupDialogComponent;
    @Input() caption;
    isReady = false;
    choices: TagValue[] = [];
    singleSelectMode = false;
    private originalValuesJson: string;
    constructor() { }

    ngOnInit() {
    }
    async open(values: TagValue[], singleSelect: boolean, onClose: (r: MultiplevalueEditorResult) => void) {
        this.singleSelectMode = singleSelect;
        this.choices = values;
        this.originalValuesJson = JSON.stringify(this.choices);
        this.isReady = true;
        this.popup.open((r: MultiplevalueEditorResult) => {
            this.isReady = false;            
            onClose(r);
        });
    }
    hasChanged() {
        let temp = JSON.stringify(this.choices);
        return temp !== this.originalValuesJson;
    }
    ok() {
        let r = new MultiplevalueEditorResult();
        r.dialogResult = DialogResult.ok;
        r.choices = this.choices;
        //console.log(`${JSON.stringify(r.choices, null, 4)}`);
        this.popup.close(r);
    }
    cancel() {
        this.choices = JSON.parse(this.originalValuesJson);
        let r = new MultiplevalueEditorResult();
        r.dialogResult = DialogResult.cancel;
        this.popup.close(r);
    }
    onChange(v: TagValue) {
        if (this.singleSelectMode) {
            if (v.selected === true) {
                for (let c of this.choices) {
                    if (c !== v) {
                        c.selected = false;
                    }
                }
            }
        }
    }
}
