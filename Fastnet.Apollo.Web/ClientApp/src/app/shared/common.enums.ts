
export enum LocalStorageKeys {
    browserKey, // identifies this browser to the system, i.e, to the extend that localStorage can..
    currentStyle,
    popularSettings,
    currentDevice, // i.e. for the audio controller, device-menu and device playlist components
    audioDevice, // i.e. for local web audio
}
export enum MusicStyles {
    Popular = 1,
    WesternClassical,
    Opera,
    IndianClassical,
    HindiFilms
}

export enum DeviceState {
    NotKnown,
    Playing,
    Paused,
    Idle
}
export enum PlaylistItemType {
    SingleItem = 1,
    MultipleItems = 2
}
export enum PlayerStates {
    Initial,
    SilentIdle,
    Idle,
    Playing,
    Paused,
    WaitingNext,
    Fault,
    //WaitingAudioEnable
}
export enum AudioDeviceType {
    Unknown = 0,
    DirectSoundOut = 1,
    Wasapi = 2,
    Asio = 3,
    Logitech = 4,
    Browser = 5
}

export enum PlayerCommands {
    Play = 0,
    TogglePlayPause = 1,
    SetVolume = 2,
    SetPosition = 3,
    ListFinished = 4,
    Reset = 5
}
