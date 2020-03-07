import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, EMPTY, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
//import 'rxjs/add/operator/catch';
class DataResult {
    data: any;
    exceptionMessage: string;
    message: string;
    success: boolean;
}
export class ServiceError {
    query: string;
    exception: string;
    message: string;
}
export abstract class BaseService {
    constructor(protected http: HttpClient, private urlPrefix: string) { }
    protected async getAsync<T>(url: string, onServiceError?: (se: ServiceError) => void) : Promise<T> {
        return new Promise<T>(resolve => {
            this.get<T>(url, onServiceError)
                .subscribe((d: T) => {
                    resolve(d as T);
                })
        });
    }
    protected async postAsync<T, R>(url: string, data: T, onServiceError?: (se: ServiceError) => void): Promise<R> {
        return new Promise<R>(resolve => {
            this.post<T, R>(url, data, onServiceError)
                .subscribe((d: R) => {
                    resolve(d as R);
                })
        });
    }
    protected post<T, R>(url: string, data: T, onServiceError?: (se: ServiceError) => void): Observable<R> {
        return new Observable<R>((observer) => {
            this.postResponse<DataResult, T>(url, data)
                .subscribe((dr) => {
                    if (dr === null) {
                        observer.next();
                    }
                    else if (dr.success) {
                        observer.next(dr.data);
                    } else {
                        this.handleFailedDataResult(dr, url, onServiceError);
                        //let se = new ServiceError();
                        //se.exception = dr.exceptionMessage;
                        //se.message = dr.message;
                        //se.query = url;
                        //if (onerror) {
                        //    onServiceError(se);
                        //} else {
                        //    this.defaultServiceErrorHandler(se);
                        //}
                        observer.error("fastnet service error");
                    }
                })
        });
    }
    protected get<T>(url: string, onServiceError?: (se: ServiceError) => void): Observable<T> {
        return new Observable<T>((observer) => {
            this.getResponse<DataResult>(url)
                .subscribe((dr) => {
                    if (dr === null) {
                        observer.next();
                    }
                    else if (dr.success) {
                        observer.next(dr.data);
                    } else {
                        this.handleFailedDataResult(dr, url, onServiceError);
                        //let se = new ServiceError();
                        //se.exception = dr.exceptionMessage;
                        //se.message = dr.message;
                        //se.query = url;
                        //if (onerror) {
                        //    onServiceError(se);
                        //} else {
                        //    this.defaultServiceErrorHandler(se);
                        //}
                        observer.error("fastnet service error");
                    }
                }) 
        });
    }
    private getResponse<DataResult>(url: string) {
        url = `${this.urlPrefix}/${url}`;
        return this.http.get<DataResult>(url)
            .pipe(
            catchError((err: HttpErrorResponse) => {
                if (err.error instanceof Error) {
                    //A client-side or network error occurred.
                    alert(`query: ${url}\nerror: ${err.error.message}`);
                } else {
                    //Backend returns unsuccessful response codes such as 404, 500 etc.
                    alert(`query: ${url}\nreturned status code: ${err.status}`);
                }
                return EMPTY;// Observable.empty<DataResult>();
            }));
    }
    protected postResponse<DataResult, T>(url: string, data: T): Observable<DataResult> {
        console.log(`post response`);
        url = `${this.urlPrefix}/${url}`;
        return this.http.post<DataResult>(url, data)
            .pipe(
            catchError((err: HttpErrorResponse) => {
                if (err.error instanceof Error) {
                    //A client-side or network error occurred.
                    alert(`query: ${url}\nerror: ${err.error.message}`);
                } else {
                    //Backend returns unsuccessful response codes such as 404, 500 etc.
                    alert(`query: ${url}\nreturned status code: ${err.status}`);
                }
                return EMPTY;// Observable.empty<DataResult>();
            }));
    }
    private defaultServiceErrorHandler(se: ServiceError) {
        let errorText = `query: ${se.query}\nmessage: ${se.message}`;
        if (se.exception) {
            errorText = errorText + `\nexception: ${se.exception}`;
        }
        alert(errorText);
    }
    private handleFailedDataResult(dr: DataResult, query: string, onServiceError?: (se: ServiceError) => void) {
        let se = new ServiceError();
        se.exception = dr.exceptionMessage;
        se.message = dr.message;
        se.query = query;
        if (onServiceError) {
            onServiceError(se);
        } else {
            this.defaultServiceErrorHandler(se);
        }
    }
}
