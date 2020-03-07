import { Component, OnInit, ViewChild } from '@angular/core';
import { PopupPanelComponent } from '../../fastnet/controls/popup-panel.component';
import { Style, Parameters } from '../shared/common.types';
import { ParameterService } from '../shared/parameter.service';
import { SettingsComponent } from '../settings/settings.component';

@Component({
  selector: 'menu-panel',
  templateUrl: './menu-panel.component.html',
  styleUrls: ['./menu-panel.component.scss']
})
export class MenuPanelComponent implements OnInit {
    //UIMode = UIMode;
    @ViewChild(PopupPanelComponent, { static: false }) menu: PopupPanelComponent;
    @ViewChild(SettingsComponent, { static: false }) settingsDialog: SettingsComponent;
    parameters: Parameters = new Parameters();
    private _currentStyle: Style;
    get currentStyle(): Style {
        return this._currentStyle;
    }
    set currentStyle(val: Style) {
        this._currentStyle = val;
        this.ps.setCurrentStyle(this.currentStyle.id);
    }
    constructor(private ps: ParameterService) {
        this.parameters = this.ps.getParameters();
        this.currentStyle = this.ps.getCurrentStyle();
    }

    ngOnInit() {
    }
    clickMenuBars(e) {
        this.menu.open(e);
    }
    getEnabledStyles(): Style[] {
        return this.parameters.styles.filter((s) => { return s.enabled; });
    }
    setCurrentStyle(style: Style) {
        this.currentStyle = style;
    }
    isMobileDevice() {
        return this.ps.isMobileDevice();
    }
    onSettingsClick() {
        this.settingsDialog.open(() => { });
    }
    //getMode() {
    //    //console.log(`mode is ${UIMode[this.mode]}`);
    //    return this.ps.getMode();
    //}
    //setMode(m: UIMode) {
    //    this.ps.setMode(m);
    //}
}
