import { Component, ElementRef, OnInit, ViewChild, Input, ViewEncapsulation } from '@angular/core';
import { DialogBase } from './dialog-base.type';

const popupZindexBase = 5000;

export type PopupCloseHandler = (arg?: any) => boolean | Promise<boolean> | void | Promise<void>;
export type PopupAfterOpenHandler = () => void;

@Component({
    selector: 'popup-dialog',
    templateUrl: './popup-dialog.component.html',
    styleUrls: ['./popup-dialog.component.scss']//,
    //encapsulation: ViewEncapsulation.None
})
export class PopupDialogComponent extends DialogBase implements OnInit {
    // rename this component to something like PopupChromeComponent
    public static openPopupsCount = 0;
    private static counter = 0;
    @Input() nocaption = false;
    @Input() caption: string;// = "no caption provided";
    @Input() warning = false;
    @Input() error = false;
    @Input() width?: number;
   
    @ViewChild('overlay', { static: false }) overlay: ElementRef;
    protected popupComponentElement: HTMLElement;
    //public isOpen: boolean = false;
    private reference: string;
    private isInitialised: boolean = false;
    private closeHandler: PopupCloseHandler;
    private widthAsSet: number | null = null;
    constructor(pdc: ElementRef) {
        super();
        this.popupComponentElement = pdc.nativeElement;
        //console.log('new PopupDialogComponent');
    }
    ngOnInit(): void {
        if (this.isInitialised === false) {
            this.reference = `pdc-${PopupDialogComponent.counter++}`;            
            this.popupComponentElement.style.display = "none";
            document.body.appendChild(this.popupComponentElement);// move myself to the body
            //console.log(`ngOnInit():moved  ${this.reference} to body`);
            this.isInitialised = true;
        }
    }
    getCaptionClass(): string {
        if (this.nocaption === false) {
            if (this.error) {
                return 'error';
            } else if (this.warning) {
                return 'warning';
            } else {
                return '';
            }
        } else {
            return 'hide';
        }
    }
    getOverlayStyle() {
        if (this.widthAsSet !== null) {
            return { width: this.widthAsSet + 'px' };
        }
        else if (this.width) {
            return {width: this.width + 'px'};
        } else {
            return {};
        }
    }
    unsetWidth() {
        this.widthAsSet = null;
    }
    setWidth(width: number) {
        this.widthAsSet = width;
    }
    /**
     * opens the popup dialog as modal "window". The onClose method is called when the popup is closed.
     * @param onClose called when the popup is called (return false to cancel closure of the popup)
     * @param afterOpen an optional method to call immediately after the popup is opened
     */
    public open(onClose: PopupCloseHandler, afterOpen: PopupAfterOpenHandler = () => { }) {
        this.reset();
        this.closeHandler = onClose;
        let zIndex = popupZindexBase + (1000 * PopupDialogComponent.openPopupsCount++);
        let overlay = this.overlay.nativeElement as HTMLDivElement;
        overlay.style.zIndex = zIndex.toString();
        this.popupComponentElement.style.display = "block";
        //this.isOpen = true;
        if (afterOpen) {
            afterOpen();
        }
    }
    /**
     * closes the popup dialog. The onClose method (passed as the first arg to the open call) is called before the popup closes.
     * @param arg an optional argument that will be passed to the onClose method
     */
    public close(arg?: any) {
        let r = this.closeHandler(arg);
        let result = r ? <boolean>r : true;
        if (result) {
            //this.isOpen = false;
            this.popupComponentElement.style.display = "none";
            let overlay = this.overlay.nativeElement as HTMLDivElement;
            overlay.style.zIndex = "0";
            PopupDialogComponent.openPopupsCount--;
        }
    }
}
