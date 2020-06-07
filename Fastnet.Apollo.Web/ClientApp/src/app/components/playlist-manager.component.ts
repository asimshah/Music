import { Component, OnInit, ViewChild } from '@angular/core';
import { PopupDialogComponent } from '../../fastnet/controls/popup-dialog.component';
import { DialogResult } from '../../fastnet/core/core.types';

@Component({
  selector: 'playlist-manager',
  templateUrl: './playlist-manager.component.html',
  styleUrls: ['./playlist-manager.component.scss']
})
export class PlaylistManagerComponent implements OnInit {
   @ViewChild(PopupDialogComponent, { static: false }) popup: PopupDialogComponent;
  constructor() { }

  ngOnInit() {
  }
   open() {

      this.popup.open((r: DialogResult) => {
         //this.isReady = false;
         //onClose(r);
      });
   }
}
