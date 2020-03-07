import { Component, OnInit, ViewChild } from '@angular/core';
import { PopupDialogComponent, PopupCloseHandler } from '../../fastnet/controls/popup-dialog.component';
import { EditorResult } from '../shared/catalog.types';
import { LibraryService } from '../shared/library.service';
import { PlayerService } from '../shared/player.service';
import { AudioDevice } from '../shared/common.types';
import { PopularSettings, ParameterService } from '../shared/parameter.service';

enum SettingsSelector {
    Popular,
    WesternClassical,
    CatalogMethods,
    AudioDevices,
    Colours
}

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss']
})
export class SettingsComponent implements OnInit {
    SettingsSelector = SettingsSelector;
    @ViewChild('settingsdialog', { static: false }) popup: PopupDialogComponent;

    currentSettings: SettingsSelector = SettingsSelector.Popular;
    popularSettings: PopularSettings;

    devices: AudioDevice[] = [];
    private closeHandler: (r: EditorResult) => void;
    constructor(private parameterService: ParameterService, private ls: LibraryService, private playerService: PlayerService) {
        this.popularSettings = this.parameterService.getPopularSettings();
    }

    ngOnInit() {

    }
    open(onClose: PopupCloseHandler) {
        this.currentSettings = SettingsSelector.Popular;
        this.closeHandler = onClose;
        this.popup.open((r: EditorResult) => { this.popupClosed(r) });
    }
    popupClosed(r: EditorResult): void {
        this.closeHandler(r);
    }
    onClose() {
        this.popup.close(EditorResult.cancel);
    }
    async onResetDatabase() {
        await this.ls.resetDatabase();
    }
    async onStartMusicScanner() {
        await this.ls.startMusicScanner();
    }
    async onStartCatalogueValidator() {
        await this.ls.startCatalogueValidator();
    }
    async changeSettings(value: SettingsSelector) {
        this.currentSettings = value;
        switch (this.currentSettings) {
            case SettingsSelector.AudioDevices:
                await this.loadDevices();
                break;
        }
    }
    async onDeviceUpdate(d: AudioDevice) {
        await this.playerService.updateDevice(d);
    }
    togglePopularShowArtists() {
        this.popularSettings.showArtists = !this.popularSettings.showArtists;
        this.parameterService.setPopularSettings(this.popularSettings);
    }
    private async loadDevices() {
        this.devices = await this.playerService.getDevices(true);
    }
}
