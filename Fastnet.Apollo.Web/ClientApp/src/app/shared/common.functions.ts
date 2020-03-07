import { retry } from "rxjs/operators";

export function log(text: string): void {
    let now = new Date();
    console.log(`${now.toLocaleTimeString()}.${pad(now.getMilliseconds(), 3)}: ${text}`);
}
function pad(n: any, width: any) {
    let z = '0';//z || '0';
    n = n + '';
    return n.length >= width ? n : new Array(width - n.length + 1).join(z) + n;
}
export function getLocalStorageValue(key: string, defaultValue : string): string {
    let value = localStorage.getItem(key);
    return value ? value : defaultValue;
}
export function setLocalStorageValue(key: string, value: string) {
    localStorage.setItem(key, value);
    return;
}
export function removeLocalStorageValue(key: string) {
    localStorage.removeItem(key);
    return;
}
////localStorage.removeItem(this.storedDeviceKey);
