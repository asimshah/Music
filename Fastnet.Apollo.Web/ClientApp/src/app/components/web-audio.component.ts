import { Component, ViewChild, ElementRef, AfterViewInit, OnDestroy, Inject } from '@angular/core';
import { LoggingService } from '../shared/logging.service';
import { PlayerService } from '../shared/player.service';
import { AudioDevice, PlayerCommand, DeviceStatus } from '../shared/common.types';
import { PlayerCommands, PlayerStates } from '../shared/common.enums';
import { ParameterService } from '../shared/parameter.service';
import { MessageService } from '../shared/message.service';
import { Subscription } from 'rxjs';
import { PopupMessageComponent } from '../../fastnet/controls/popup-message.component';
import { DOCUMENT } from '@angular/common';

enum PlayerEvents {
    Play,
    TogglePlayPause,
    PlayCompleted,
    ListFinished,
    WaitTimeout,
    PlayerStarted,
    FaultOccurred,
    Reposition,
    SetVolume,
    Reset//,
    //AudioGesture
}

const waitForEventInterval = 3000 * 3;
const deviceStatusUpdateInterval = 3000;

type FSMAction = (state: PlayerStates, ev: PlayerEvents, ...args: any[]) => Promise<PlayerStates> | PlayerStates;
class FSM {
    private statesCount = 7; // the number PlayerStates
    private eventsCount = 10; // the number PlayerEvents
    public currentState: PlayerStates = PlayerStates.Initial;
    private actions: FSMAction[][];
    constructor(public name: string, private log: LoggingService) {
        console.log(`${Object.keys(PlayerStates).length / 2} states by ${Object.keys(PlayerEvents).length / 2} events`);
        this.actions = [...Array(this.statesCount)].fill(0).map(_ => Array(this.eventsCount).fill((s, e, args) => this.defaultAction(s, e, args)));
    }
    public getState() {
        return this.currentState;
    }
    public getAction(state: PlayerStates, event: PlayerEvents) {
        let action = this.actions[state][event];
        return action;
    }
    public defaultAction(state: PlayerStates, ev: PlayerEvents, ...args: any[]): Promise<PlayerStates> {
        this.log.information(`${this.name}: default action called with state ${PlayerStates[state]}, event ${PlayerEvents[ev]}`);
        return new Promise<PlayerStates>(resolve => {
            resolve(state);
        });
    }
    public addAction(state: PlayerStates, ev: PlayerEvents, action: FSMAction) {
        //if (this.actions[state][ev] !== this.defaultAction) {
        //    //this.log.warning(`${this.name}: State ${PlayerStates[state]}, Event ${PlayerEvents[ev]}, existing action being overwritten!!`);
        //}
        this.actions[state][ev] = action;
    }
    public addActions(states: PlayerStates[], ev: PlayerEvents, action: FSMAction) {
        for (let state of states) {
            this.addAction(state, ev, action);
        }
    }
    public async act(ev: PlayerEvents, ...args: any[]) {
        let method = this.getAction(this.currentState, ev);
        let action = async () => {
            return await method(this.currentState, ev, args);
        };
        let nextState = await action();
        this.log.debug(`state ${PlayerStates[this.currentState]}, event ${PlayerEvents[ev]}, --> state ${PlayerStates[nextState]}`);
        this.currentState = nextState;
    }
}

@Component({
    selector: 'web-audio',
    templateUrl: './web-audio.component.html',
    styleUrls: ['./web-audio.component.scss']
})
export class WebAudioComponent implements AfterViewInit, OnDestroy {
    @ViewChild(PopupMessageComponent, { static: false }) popupMessage: PopupMessageComponent;
    audioStreamUrl: string;
    //safeUrl: SafeUrl;
    @ViewChild('audio', { static: false }) audioElement: ElementRef;
    audio: HTMLAudioElement;
    enableAudioButtonVisible = false;
    pendingStreamurl: string;
    pendingVolume: number
    private waitForEventTimer: any;
    private deviceStatusUpdateInterval: any;
    private stateMachine: FSM;
    private device: AudioDevice;
    private messageSubscriptions: Subscription[] = [];
    constructor(private ps: PlayerService, private parameterService: ParameterService,
        private messageService: MessageService,
        private log: LoggingService,
        @Inject(DOCUMENT) private document: Document) {
        this.messageSubscriptions.push(this.messageService.playerCommand.subscribe((pc) => {
            this.onPlayerCommand(pc);
        }));
        this.messageSubscriptions.push(this.messageService.connectedState.subscribe(async (tf) => {
            if (tf === true) {
                await this.enableWebAudio();
                this.onEvent(PlayerEvents.PlayerStarted);
                log.debug("web audio: re-enabled");
            } else {
                log.warning("web audio: signalr connection lost");
            }
        }));
        //this.testWebAudio();
        //this.unlockAudioContext();
    }
    async unlockAudioContextAsync() {
        var AudioContext = (<any>window).AudioContext || (<any>window).webkitAudioContext;
        var audioCtx: any = new AudioContext();
        return new Promise<string>((resolve) => {
            if (audioCtx.state !== 'suspended') {
                //this.log.information(`audio context state is ${audioCtx.state}`);
                resolve(audioCtx.state);
            } else {
                const b = document.body;
                const events = ['touchstart', 'touchend', 'mousedown', 'keydown'];
                //this.log.information(`audio context starting unlock`);
                events.forEach(e => b.addEventListener(e, () => {
                    unlockF();
                }, false));
                var unlockF = () => {
                    audioCtx.resume().then(() => {
                        cleanF();
                    });
                };
                var cleanF = () => {
                    events.forEach(e => b.removeEventListener(e, unlockF));
                    //this.log.information(`audio context unlock clean up`);
                    resolve(audioCtx.state);
                };
            }

        });
    }
    unlockAudioContext() {
        var AudioContext = (<any>window).AudioContext || (<any>window).webkitAudioContext;
        var audioCtx: any = new AudioContext();
        if (audioCtx.state !== 'suspended') {
            this.log.information(`audio context state is ${audioCtx.state}`);
            return ;
        }
        //alert(`state is ${audioCtx.state}`);
        const b = document.body;
        const events = ['touchstart', 'touchend', 'mousedown', 'keydown'];
        //events.forEach(e => b.addEventListener(e, unlock, false));
        //function unlock() {
        //    audioCtx.resume().then(() => {
        //        clean();
                
        //    });
        //}
        //function clean() {
        //    events.forEach(e => b.removeEventListener(e, unlock));
            
        //}
        this.log.information(`audio context starting unlock`);
        events.forEach(e => b.addEventListener(e, () => {
            unlockF();
        }, false));
        var unlockF = () => {
            audioCtx.resume().then(() => {
                cleanF();
            });
        };
        var cleanF = () => {
            events.forEach(e => b.removeEventListener(e, unlockF));
            this.log.information(`audio context unlock clean up`);
        };
    }
    testWebAudio() {
        var AudioContext = (<any>window).AudioContext || (<any>window).webkitAudioContext;
        var audioCtx: any = new AudioContext();
        alert(`state is ${audioCtx.state}`);
    }
    ngOnDestroy() {
        for (let sub of this.messageSubscriptions) {
            sub.unsubscribe();
        }
        this.messageSubscriptions = [];
        if (this.waitForEventTimer) {
            clearTimeout(this.waitForEventTimer);
        }
        if (this.deviceStatusUpdateInterval) {
            clearInterval(this.deviceStatusUpdateInterval);
        }
    }
    async ngAfterViewInit() {
        this.audio = this.audioElement.nativeElement;
    }
    private onEvent(event: PlayerEvents, ...args: any[]) {
        if (this.stateMachine) {
            this.stateMachine.act(event, args);
        } else {
            this.log.error("web-audio: stateMachine is null!!");
        }
    }
    private onPlayerCommand(pc: PlayerCommand) {
        //console.log(`received player command ${PlayerCommands[pc.command]}`);
        switch (pc.command) {
            case PlayerCommands.Play:
                this.onEvent(PlayerEvents.Play, `${pc.streamUrl}`, pc.volume);
                break;
            case PlayerCommands.TogglePlayPause:
                this.onEvent(PlayerEvents.TogglePlayPause);
                break;
            case PlayerCommands.SetVolume:
                this.onEvent(PlayerEvents.SetVolume, pc.volume);
                break;
            case PlayerCommands.SetPosition:
                this.onEvent(PlayerEvents.Reposition, pc.position);
                break;
            case PlayerCommands.ListFinished:
                this.onEvent(PlayerEvents.ListFinished);
                break;
            case PlayerCommands.Reset:
                this.onEvent(PlayerEvents.Reset);
                break;
        }
    }
    private async enableWebAudio() {
        this.device = await this.ps.enableWebAudio();
        this.stateMachine = new FSM(this.device.displayName, this.log);
        this.initialiseFSM();
        this.log.debug("web-audio: enabled");
        this.startDeviceStatusUpdate();
    }
    private startDeviceStatusUpdate() {
        this.deviceStatusUpdateInterval = setInterval(async () => {
            await this.onDeviceStatusUpdateInterval();
        }, deviceStatusUpdateInterval);
    }
    private async onDeviceStatusUpdateInterval() {
        let state = this.stateMachine.getState();
        switch (state) {
            case PlayerStates.Fault:
            case PlayerStates.Initial:
            case PlayerStates.SilentIdle:
                break;
            default:
                let ds = new DeviceStatus();
                ds.key = this.device.key;
                ds.state = this.stateMachine.getState();
                ds.volume = this.audio.volume;
                ds.currentTime = this.audio.currentTime;
                ds.totalTime = this.audio.duration;
                await this.ps.sendWebAudioDeviceStatus(ds);
                break;
        }
    }
    private initialiseFSM() {
        this.stateMachine.addAction(PlayerStates.Initial, PlayerEvents.FaultOccurred, (s, e, args) => new Promise((r) => r(PlayerStates.Fault)));
        this.stateMachine.addAction(PlayerStates.Initial, PlayerEvents.PlayerStarted, (s, e, args) => {
            this.startWaitTimer();
            return PlayerStates.Idle;
        });
        this.stateMachine.addActions([
            PlayerStates.SilentIdle, PlayerStates.Idle,
            PlayerStates.Playing, PlayerStates.Paused, PlayerStates.WaitingNext/*, PlayerStates.WaitingAudioEnable*/
        ], PlayerEvents.Play, async (s, e, args) => {
            if (args.length === 1) {
                let parameters: any[] = args[0];
                if (parameters.length > 1) {
                    let streamUrl = <string>parameters[0];
                    let volume = <number>parameters[1];
                    if (await this.play(streamUrl, volume)) {
                        return PlayerStates.Playing;
                    } else {
                        this.log.error(`unable to play ${streamUrl}`);
                        this.enableAudioButtonVisible = true;
                        return PlayerStates.Idle;// PlayerStates.WaitingAudioEnable;
                    }

                }
            }
            this.log.warning(`web-audio: ${this.stateMachine.name}: play requires a stream url and initial volume`);
            return this.stateMachine.getState();
        });
        this.stateMachine.addActions([PlayerStates.Playing, PlayerStates.Paused], PlayerEvents.TogglePlayPause, (s, e, args) => {
            let r = this.togglePlayPause();
            return r ? PlayerStates.Playing : PlayerStates.Paused;
        });
        this.stateMachine.addActions([PlayerStates.Playing, PlayerStates.Paused], PlayerEvents.Reposition, (s, e, args) => {
            if (args.length > 0) {
                let position = <number>args[0];
                this.reposition(position);
            }
            else {
                this.log.warning(`web-audio: ${this.stateMachine.name}: reposition requires a position argument`);
            }
            return PlayerStates.Playing;// new Promise(r => r(PlayerStates.Playing));
        });
        this.stateMachine.addActions([PlayerStates.Playing, PlayerStates.Paused, PlayerStates.Idle], PlayerEvents.SetVolume, (s, e, args) => {
            if (args.length > 0) {
                let level = <number>args[0];
                this.setVolume(level);
            }
            else {
                this.log.warning(`${this.stateMachine.name}: set volume requires a level argument`);
            }
            return this.stateMachine.getState();// new Promise(r => r(this.stateMachine.getState()));
        });
        this.stateMachine.addAction(PlayerStates.Playing, PlayerEvents.PlayCompleted, (s, e, args) => {
            this.requestNext(s, e, args);
            return PlayerStates.WaitingNext;
        });
        this.stateMachine.addAction(PlayerStates.Playing, PlayerEvents.ListFinished, (s, e, args) => {
            this.stopPlaying();
            this.startWaitTimer();
            return PlayerStates.Idle;
        });
        this.stateMachine.addAction(PlayerStates.WaitingNext, PlayerEvents.ListFinished, (s, e, args) => {
            this.startWaitTimer();
            return PlayerStates.Idle;
        });
        this.stateMachine.addAction(PlayerStates.Idle, PlayerEvents.WaitTimeout, (s, e, args) => {
            return PlayerStates.SilentIdle;
        });
        this.stateMachine.addAction(PlayerStates.WaitingNext, PlayerEvents.WaitTimeout, (s, e, args) => {
            this.stopPlaying()
            this.startWaitTimer();
            return PlayerStates.Idle;
        });
        this.stateMachine.addActions([PlayerStates.Playing, PlayerStates.Paused], PlayerEvents.Reset, (s, e, args) => {
            this.stopPlaying();
            this.startWaitTimer();
            return PlayerStates.Idle;
        });
        this.stateMachine.addAction(PlayerStates.WaitingNext, PlayerEvents.Reset, (s, e, args) => {
            this.startWaitTimer();
            return PlayerStates.Idle;
        });
        //this.stateMachine.addAction(PlayerStates.WaitingAudioEnable, PlayerEvents.AudioGesture, (s, e, args) => {
        //    //this.startWaitTimer();
        //    return PlayerStates.Playing;
        //});
    }
    private startWaitTimer() {
        if (this.waitForEventTimer) {
            clearTimeout(this.waitForEventTimer);
            this.waitForEventTimer = undefined;
        }
        this.waitForEventTimer = setTimeout(() => {
            this.onEvent(PlayerEvents.WaitTimeout);
        }, waitForEventInterval);
    }
    onPlayEnded() {
        this.log.information(`web-audio: ended event`);
        this.onEvent(PlayerEvents.PlayCompleted);
    }
    //onAudioEnable() {
    //    this.enableAudioButtonVisible = false;
    //    this.audio.src = this.pendingStreamurl;
    //    this.audio.volume = this.pendingVolume;
    //    this.audio.play()
    //        .then(_ => {
    //            this.stateMachine.currentState = PlayerStates.Playing;
    //            this.log.information(`web-audio: play started after button click`);
    //        })
    //        .catch(_ => {
    //            alert("does not play!!")
    //        });
    //    //this.onEvent(PlayerEvents.Play, this.pendingStreamurl, this.pendingVolume);
    //}
    private play(streamurl: string, volume: number) {
        let currentState = this.stateMachine.getState();
        if (currentState === PlayerStates.Playing) {
            this.audio.pause();
            this.audio.currentTime = 0.0;
        }
        this.audio.src = streamurl;
        this.audio.volume = volume;
        return new Promise<Boolean>(async (resolve) => {
            try {
                var state = await this.unlockAudioContextAsync();
                this.log.information(`audio context state is ${state}`);
                this.audio.play()
                    .then(_ => {
                        resolve(true);
                    })
                    .catch(_ => {
                        let error = <any>this.audio.error;
                        if (error != null) {
                            this.log.error(`web-audio: ${error.code}, ${error.message}`);
                        } else {
                            this.log.error(`web-audio: no error indicated [_ = ${JSON.stringify(_)}]}`);
                        }
                        this.pendingStreamurl = streamurl;
                        this.pendingVolume = volume;
                        resolve(false);
                    });
            } catch(err) {
                alert(`caught error ${err}`);
            }
        });

    }
    private togglePlayPause(): boolean {
        if (this.audio.paused) {
            this.audio.play();
        } else {
            this.audio.pause();
        }
        return !this.audio.paused;
    }
    private reposition(position: number) {
        let pos = this.audio.duration * position;
        this.audio.currentTime = pos;
    }
    private setVolume(level: number) {
        this.audio.volume = level;
        this.log.information(`web-audio: setVolume(), requested ${level}, result ${this.audio.volume}`);
    }
    private stopPlaying() {
        this.audio.pause();
        this.audio.currentTime = 0.0;
    }
    private requestNext(state: PlayerStates, ev: PlayerEvents, ...args: any[]) {
        setTimeout(async () => {
            await this.ps.playNextPlaylistItem(this.device.key);
        }, 500);

        this.startWaitTimer();
    }
}
