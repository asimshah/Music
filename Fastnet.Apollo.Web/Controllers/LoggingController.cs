using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Core.Web.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Fastnet.Apollo.Web.Controllers
{
    enum Severity
    {
        Trace,
        Debug,
        Information,
        Warning,
        Error
    }
    class ClientLog
    {
        public Severity Severity { get; set; }
        public string Text { get; set; }
    }
    [Route("log")]
    [ApiController]
    public class LoggingController : BaseController
    {
        private readonly ILogger clientLogger;
        public LoggingController(ILoggerFactory lf, ILogger<LoggingController> logger, IWebHostEnvironment env) : base(logger, env)
        {
            this.clientLogger = lf.CreateLogger("Browser");
        }

        [HttpPost("message")]
        public async Task<IActionResult> LogMessage()
        {
            var cl = await this.Request.FromBody<ClientLog>();
            switch(cl.Severity)
            {
                case Severity.Debug:
                    clientLogger.Debug(FormatText(cl));
                    break;
                case Severity.Error:
                    clientLogger.Error(FormatText(cl));
                    break;
                case Severity.Information:
                    clientLogger.Information(FormatText(cl));
                    break;
                case Severity.Warning:
                    clientLogger.Warning(FormatText(cl));
                    break;
                case Severity.Trace:
                    clientLogger.Trace(FormatText(cl));
                    break;
            }
            
            return SuccessResult();
        }
        private string FormatText(ClientLog cl)
        {
            var isMobile = Request.IsMobileBrowser();
            var browser = Request.GetBrowser().ToString();
            var isIpad = Request.IsIpad();
            return $"[{browser}{(isMobile ? "/mobile": "")}{(isIpad ? "/ipad" : "")}]:{cl.Text}";
        }
    }
}