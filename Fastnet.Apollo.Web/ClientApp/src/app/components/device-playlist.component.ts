import { Component, OnInit, OnDestroy } from '@angular/core';
import { PlaylistItem, DeviceStatus, AudioDevice, PlaylistUpdate } from '../shared/common.types';
import { Subscription } from 'rxjs';
import { PlayerService } from '../shared/player.service';
import { PlaylistItemType } from '../shared/common.enums';
import { LoggingService } from '../shared/logging.service';
import { ParameterService } from '../shared/parameter.service';

@Component({
    selector: 'device-playlist',
    templateUrl: './device-playlist.component.html',
    styleUrls: ['./device-playlist.component.scss']
})
export class DevicePlaylistComponent implements OnInit, OnDestroy {
    PlaylistItemType = PlaylistItemType;
    playlist: PlaylistItem[] = [];// | null = null;

    private deviceStatus: DeviceStatus;// | null = null;
    private device: AudioDevice;// | null = null;
    private subscriptions: Subscription[] = [];
    constructor(private playerService: PlayerService,
        private ps: ParameterService,
        private log: LoggingService) {
        //this.parameters = this.ps.getParameters();
        this.log.isMobileDevice = this.isMobileDevice();
    }
    async ngOnInit() {
        this.subscriptions.push(this.playerService.deviceStatusUpdate.subscribe(async (ds) => {
            await this.onDeviceStatusUpdate(ds);
        }));
        this.subscriptions.push(this.playerService.currentDeviceChanged.subscribe(async (d) => {
            this.device = d;
            //this.log.information(`DevicePlaylistComponent: received device change to ${this.device.displayName} [${this.device.key}]`);
            //this.playlist = await this.playerService.getCurrentPlaylist();
            //this.deviceStatus = await this.playerService.getCurrentDeviceStatus();
        }));
        this.subscriptions.push(this.playerService.currentPlaylistChanged.subscribe((pl) => {
            this.playlist = pl;
            //this.log.information(`DevicePlaylistComponent: received play list of ${this.playlist.length} items for [${this.device.key}]`);
        }));
        await this.playerService.triggerUpdates();
        //this.device = this.playerService.getCurrentDevice();
        //this.playlist = this.playerService.getCurrentPlaylist();
        ////this.deviceStatus = await this.playerService.getCurrentDeviceStatus();
        //console.log(`current device is ${this.device === null ? "null" : this.device.displayName}`);
    }
    ngOnDestroy() {
        for (let sub of this.subscriptions) {
            sub.unsubscribe();
        }
        this.subscriptions = [];
    }

    async playItem(item: PlaylistItem) {

        await this.playerService.playItem(item.position);
    }
    async toggleMultipleItems(item: PlaylistItem) {
        item.isOpen = !item.isOpen;
    }
    isMobileDevice() {
        return this.ps.isMobileDevice();
    }
    isPlayable(item: PlaylistItem) {
        return !item.notPlayableOnCurrentDevice;
        //return this.device && (this.device.capability.maxSampleRate === 0 || item.sampleRate <= this.device.capability.maxSampleRate);
    }
    getTitle(item: PlaylistItem) {
        return item.titles[item.titles.length - 1];
    }
    getFullTitle(item: PlaylistItem) {
        let text = `<div>${item.titles[0]}</div><div>${item.titles[1]}</div><div>${item.titles[2]}</div>`;
    }
    isPlayingIconVisible(item: PlaylistItem) {
        let r = false;
        if (this.deviceStatus) {
            if (item.isSubitem) {
                if (item.position.major === this.deviceStatus.playlistPosition.major
                    && item.position.minor === this.deviceStatus.playlistPosition.minor) {
                    r = true;
                }
            }
            else if (item.position.major === this.deviceStatus.playlistPosition.major) {
                r = true;
            }
        }
        return r;
    }
    private async onDeviceStatusUpdate(ds: DeviceStatus) {
        this.deviceStatus = ds;
        if (this.device && this.device.key !== this.deviceStatus.key) {
            console.warn(`received status update for ${this.deviceStatus.key} - not the current device!`);
        }
    }

}
