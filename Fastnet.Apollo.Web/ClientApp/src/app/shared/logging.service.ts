import { Injectable } from "@angular/core";
import { BaseService } from "./base-service.service";
import { HttpClient } from "@angular/common/http";
import { environment } from "../../environments/environment";
import { ILoggingService } from "../../fastnet/core/core.types";

enum Severity {
    Trace,
    Debug,
    Information,
    Warning,
    Error
}
class ClientLog {
    severity: Severity;
    text: string;
}

@Injectable()
export class LoggingService extends BaseService implements ILoggingService  {
    isMobileDevice: boolean = false;
    constructor(http: HttpClient) {
        super(http, "log");
    }
    public async trace(text: string) {
        let cl = new ClientLog();
        cl.severity = Severity.Trace;
        cl.text = text;
        return this.send(cl);
    }
    public async debug(text: string) {
        let cl = new ClientLog();
        cl.severity = Severity.Debug;
        cl.text = text;
        return this.send(cl);
    }
    public async information(text: string) {
        let cl = new ClientLog();
        cl.severity = Severity.Information;
        cl.text = text;
        return this.send(cl);
    }
    public async warning(text: string) {
        let cl = new ClientLog();
        cl.severity = Severity.Warning;
        cl.text = text;
        return this.send(cl);
    }
    public async error(text: string) {
        let cl = new ClientLog();
        cl.severity = Severity.Error;
        cl.text = text;
        return this.send(cl);
    }
    private formatMessage(text: string) {
        return `[${new Date().toISOString()}] ${text}`;
    }
    private async send(cl: ClientLog) {
        if (environment.production === false) {
            this.log(cl);
        }
        return this.postAsync("message", cl);
        //if (this.isMobileDevice) {
        //    //console.log(`${Severity[cl.severity]}: ${cl.text}`);
        //    this.log(cl);
        //    return this.postAsync("message", cl);
        //} else {
        //    return new Promise<void>(resolve => {
        //        this.log(cl);
        //        //console.log(`${Severity[cl.severity]}: ${cl.text}`);
        //        resolve();
        //    });
        //}
    }
    private log(cl: ClientLog) {
        let text = this.formatMessage(cl.text);
        switch (cl.severity) {
            case Severity.Debug:
                console.debug(`${text}`);
                break;
            case Severity.Trace:
                console.trace(`${text}`);
                break;
            case Severity.Information:
                console.info(`${text}`);
                break;
            case Severity.Warning:
                console.warn(`${text}`);
                break;
            case Severity.Error:
                console.error(`${text}`);
                break;
        }
    }
}
