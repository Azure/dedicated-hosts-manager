using Polly;
using System;
using System.Threading.Tasks;

namespace DedicatedHostsManager.DedicatedHostEngine
{
    public class Helper
    {
        private const int DefaultNetworkFailureRetryCount = 2;

        public static Task<TResponse> ExecuteAsyncWithRetry<TException, TResponse>(
                Func<Task<TResponse>> funcToexecute,
                Action<string> logHandler, 
                Func<TException, bool> exceptionFilter = null,
                int retryCount = DefaultNetworkFailureRetryCount) where TException : Exception
        {
            return Policy.Handle<TException>(ce => exceptionFilter != null ? exceptionFilter(ce) : true)
                          .WaitAndRetryAsync(
                                     retryCount,
                                     r => TimeSpan.FromSeconds(2 * r),
                                     (ex, ts, r) => logHandler($"Attempt #{r}/{retryCount}. Will try again in {ts.TotalSeconds} seconds. Exception={ex}"))
                          .ExecuteAsync(() => funcToexecute());
        }
    }
}
