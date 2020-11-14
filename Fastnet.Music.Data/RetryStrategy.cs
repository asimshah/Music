using Fastnet.Core;
using Fastnet.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;

namespace Fastnet.Music.Data
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class RetryStrategy : SqlServerRetryingExecutionStrategy
    {
        private readonly ILogger log;
        public RetryStrategy( ExecutionStrategyDependencies dependencies, int maxRetryCount) : base(dependencies, maxRetryCount)
        {
            log = ApplicationLoggerFactory.CreateLogger<RetryStrategy>();
        }

        public int RetryNumber { get; set; } = 0;
        protected override void OnRetry()
        {
            RetryNumber++;
            base.OnRetry();
        }
        protected override bool ShouldRetryOn(Exception exception)
        {
            log.Debug($"Exception of type {exception.GetType().Name}, \"{exception.Message}\"");
            //Debug.WriteLine($"ShouldRetryOn() called with {exception.GetType().Name}, retry number is {RetryNumber}");
            return true;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
