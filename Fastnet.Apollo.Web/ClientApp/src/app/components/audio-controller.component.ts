import { Component, OnInit, Input, ViewChild, ElementRef, OnDestroy } from '@angular/core';
import { PlaylistItem, DeviceStatus, AudioDevice, Parameters } from '../shared/common.types';
import { PlayerStates } from '../shared/common.enums';
import { PlayerService } from '../shared/player.service';
import { Subscription } from 'rxjs';
import { LoggingService } from '../shared/logging.service';
import { ParameterService } from '../shared/parameter.service';

@Component({
    selector: 'audio-controller',
    templateUrl: './audio-controller.component.html',
    styleUrls: ['./audio-controller.component.scss']
})
export class AudioControllerComponent implements OnInit, OnDestroy {
    PlayerStates = PlayerStates;
    @ViewChild('playSlider', { static: false}) playSliderRef: ElementRef;
    @ViewChild('volumeSlider', { static: false }) volumeSliderRef: ElementRef;
    @ViewChild('playBead', { static: false }) playBeadRef: ElementRef;
    @ViewChild('volumeBead', { static: false }) volumeBeadRef: ElementRef;
    playlistItem: PlaylistItem | null = null;

    deviceStatus: DeviceStatus | null = null;
    volumeBeadPosition: string = "0px";
    playBeadPosition: string = "0px";
    private playlist: PlaylistItem[] = [];
    private device: AudioDevice;// | null = null;
    private subscriptions: Subscription[] = [];
    constructor(private playerService: PlayerService,
        private ps: ParameterService,
        private log: LoggingService) { }

    async ngOnInit() {
        this.subscriptions.push(this.playerService.deviceStatusUpdate.subscribe((ds) => {
            this.onDeviceStatusUpdate(ds);
        }));
        this.subscriptions.push(this.playerService.currentDeviceChanged.subscribe(async (d) => {
            this.device = d;
            //this.log.information(`AudioControllerComponent: received device change to ${this.device.displayName} [${this.device.key}]`);
            this.updateUI();
        }));
        this.subscriptions.push(this.playerService.currentPlaylistChanged.subscribe((pl) => {
            this.playlist = pl;
            //this.log.information(`AudioControllerComponent: received play list of ${this.playlist.length} items for [${this.device.key}]`);
            this.updateUI();
        }));
        await this.playerService.triggerUpdates();
    }
    ngOnDestroy() {
        //this.stopUpdate();
        for (let sub of this.subscriptions) {
            sub.unsubscribe();
        }
        this.subscriptions = [];
    }
    isMobileDevice() {
        return this.ps.isMobileDevice();
    }
    isPlaying() {
        return this.deviceStatus && this.deviceStatus.state === PlayerStates.Playing;
    }
    canReposition() {
        return this.device && this.device.canReposition;
    }
    async skipBackward() {
        await this.playerService.skipBack();
    }
    async skipForward() {
        console.log(`skipForward()`);
        await this.playerService.skipForward();
    }
    async togglePlayPause() {
        console.log(`togglePlayPause()`);
        await this.playerService.togglePlayPause();
    }
    async volumeUp() {
        let level = this.getVolumeLevel();
        level = level > 0.9 ? 1.0 : Math.min(level + 0.1, 1.0);
        await this.setVolume(level);

    }
    async volumeDown() {
        let level = this.getVolumeLevel();
        level = level < 0.1 ? 0.0 : Math.max(level - 0.1, 0.0);
        await this.setVolume(level);
    }
    async onVolumeSliderMouseUp(ev: MouseEvent) {
        let volumeTrack: HTMLElement = this.volumeSliderRef.nativeElement;
        let trackLength = volumeTrack.offsetWidth;
        let offset = ev.clientX - volumeTrack.offsetLeft;
        console.log(`click at ${offset} =  ${(offset / trackLength) * 100.0}`);
        await this.setVolume((offset / trackLength));
    }
    async onPlayingSliderMouseUp(ev: MouseEvent) {
        let playTrack: HTMLElement = this.playSliderRef.nativeElement;
        let trackLength = playTrack.offsetWidth;
        let offset = (ev.clientX - playTrack.offsetLeft) / trackLength;
        let isForward = (offset * 100.0) > this.getCurrentTime();
        console.log(`click at ${offset}, forward = ${isForward}`);
        await this.playerService.setPosition(offset);
    }
    private async setVolume(level: number) {
        await this.playerService.setVolume(level);
    }
    private getCurrentTime() {
        return this.deviceStatus ? (this.deviceStatus.currentTime / this.deviceStatus.totalTime) * 100.0 : 0.0;
    }
    private getVolumeLevel() {
        return this.deviceStatus ? this.deviceStatus.volume * 100.0 : 0.0;
    }
    private onDeviceStatusUpdate(ds: DeviceStatus): any {
        this.deviceStatus = ds;
        this.updateUI();

    }
    private updateUI() {
        if (this.deviceStatus) {
            this.positionPlaybead();
            this.positionVolumebead();
            if (this.playlist.length > 0) {
                let major = this.deviceStatus.playlistPosition.major;
                let minor = this.deviceStatus.playlistPosition.minor;
                if (major !== 0) {
                    this.playlistItem = this.findCurrentPlaylistItem(major, minor);
                }
            }
        }
    }
    findCurrentPlaylistItem(major: number, minor: number): PlaylistItem | null {
        let playlistItem = this.playlist.find(x => x.position.major === major)!;
        if (!playlistItem) {
            console.error(`major = ${major}, playlistItem not found, playlist length is ${this.playlist.length}, state = ${PlayerStates[this.deviceStatus!.state]}`);
        }
        else if (minor > 0) {
            playlistItem = playlistItem.subItems.find(x => x.position.minor === minor)!;
            if (!playlistItem) {
                console.error(`major = ${major}, playlistItem not found, playlist length is ${this.playlist.length}, state = ${PlayerStates[this.deviceStatus!.state]}`);
            }
        }
        return playlistItem;
    }
    private positionPlaybead() {
        if (this.playSliderRef) {
            let playTrack: HTMLElement = this.playSliderRef.nativeElement;
            let trackLength = playTrack.offsetWidth;
            let beadLeft = (trackLength * this.getCurrentTime()) / 100.0;
            let bead: HTMLElement = this.playBeadRef.nativeElement;
            this.playBeadPosition = `${beadLeft - (bead.offsetWidth / 2)}px`;
        }
    }
    private positionVolumebead() {
        if (this.volumeSliderRef) {
            let volumeTrack: HTMLElement = this.volumeSliderRef.nativeElement;
            let trackLength = volumeTrack.offsetWidth;
            let beadLeft = (trackLength * this.getVolumeLevel()) / 100.0;
            let bead: HTMLElement = this.volumeBeadRef.nativeElement;
            this.volumeBeadPosition = `${beadLeft - (bead.offsetWidth / 2)}px`;
        }
    }

}
