import { BrowserModule, HAMMER_GESTURE_CONFIG, HammerGestureConfig } from '@angular/platform-browser';
import { NgModule, APP_INITIALIZER } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { ControlsModule } from '../fastnet/controls/controls.module';
import { ParameterService, ParameterServiceFactory } from './shared/parameter.service';
import { MessageService } from './shared/message.service';
import { PlayerService } from './shared/player.service';
import { LoggingService } from './shared/logging.service';
import { SettingsComponent } from './settings/settings.component';
import { SlidingPanelComponent } from './components/sliding-panel.component';
import { SlidingPanelsComponent } from './components/sliding-panels.component';
import { MenuPanelComponent } from './components/menu-panel.component';
import { AudioControllerComponent } from './components/audio-controller.component';
import { DeviceMenuComponent } from './components/device-menu.component';
import { DevicePlaylistComponent } from './components/device-playlist.component';
import { WebAudioComponent } from './components/web-audio.component';
import { HighlightedTextComponent } from './components/highlighted-text.component';
import { LibraryService } from './shared/library.service';
import { DefaultCatalogComponent } from './catalog/default-catalog/default-catalog.component';
import { WesternClassicalCatalogComponent } from './catalog/western-classical-catalog/western-classical-catalog.component';
import { PopularCatalogComponent } from './catalog/popular-catalog/popular-catalog.component';
import { CommandPanelComponent } from './catalog/command-panel/command-panel.component';
import { WesternClassicalTagEditorComponent } from './catalog/western-classical-catalog/western-classical-tag-editor/western-classical-tag-editor.component';
import { MultipleValueEditorComponent } from './catalog/multiple-values-editor/multiple-values-editor.component';
import { MusicfileEditorComponent } from './catalog/musicfile-editor/musicfile-editor.component';
import { PopularTagEditorComponent } from './catalog/popular-catalog/popular-tag-editor/popular-tag-editor.component';
import { IndianClassicalCatalogComponent } from './catalog/indian-classical-catalog/indian-classical-catalog.component';
import { PlaylistManagerComponent } from './components/playlist-manager.component';

export class CustomHammerConfig extends HammerGestureConfig {
    overrides = <any>{
        'pinch': { enable: false },
        'rotate': { enable: false }
    }
}

@NgModule({
    declarations: [
        AppComponent,
        HomeComponent,
        SettingsComponent,
        SlidingPanelComponent,
        SlidingPanelsComponent,
        CommandPanelComponent,
        MenuPanelComponent,
        AudioControllerComponent,
        DeviceMenuComponent,
        DevicePlaylistComponent,
        WebAudioComponent,
        HighlightedTextComponent,
        DefaultCatalogComponent,
        MultipleValueEditorComponent,
        MusicfileEditorComponent,
        WesternClassicalCatalogComponent,
        WesternClassicalTagEditorComponent,
        PopularCatalogComponent,
        PopularTagEditorComponent,
        IndianClassicalCatalogComponent,
        PlaylistManagerComponent
    ],
    imports: [
        BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
        HttpClientModule,
        FormsModule,
        ControlsModule,
        RouterModule.forRoot([
            { path: '', component: HomeComponent, pathMatch: 'full' },
        ])
    ],
    providers: [
        {
            provide: HAMMER_GESTURE_CONFIG,
            useClass: CustomHammerConfig
        },
        ParameterService,
        {
            provide: APP_INITIALIZER,
            useFactory: ParameterServiceFactory,
            deps: [ParameterService],
            multi: true
        },
        MessageService,
        LibraryService,
        PlayerService,
        LoggingService
    ],
    entryComponents: [DefaultCatalogComponent, WesternClassicalCatalogComponent, PopularCatalogComponent, IndianClassicalCatalogComponent],
    bootstrap: [AppComponent]
})
export class AppModule { }
