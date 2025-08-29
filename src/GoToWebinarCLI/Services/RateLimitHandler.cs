using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GoToWebinarCLI.Services;

public class RateLimitHandler : DelegatingHandler
{
    private readonly SemaphoreSlim _semaphore = new(10);
    private readonly Queue<DateTime> _requestTimes = new();
    private readonly object _lock = new();
    private int _dailyRequestCount = 0;
    private DateTime _dailyResetTime = DateTime.UtcNow.Date.AddDays(1);
    private const int MaxRequestsPerSecond = 10;
    private const int MaxRequestsPerDay = 10000;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await ThrottleRequestAsync(cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = GetRetryAfter(response);
            await Task.Delay(retryAfter, cancellationToken);
            return await SendAsync(request, cancellationToken);
        }

        return response;
    }

    private async Task ThrottleRequestAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;

                if (now >= _dailyResetTime)
                {
                    _dailyRequestCount = 0;
                    _dailyResetTime = now.Date.AddDays(1);
                }

                if (_dailyRequestCount >= MaxRequestsPerDay)
                {
                    var waitTime = _dailyResetTime - now;
                    Task.Delay(waitTime, cancellationToken).Wait();
                    _dailyRequestCount = 0;
                }

                while (_requestTimes.Count > 0 && now - _requestTimes.Peek() > TimeSpan.FromSeconds(1))
                {
                    _requestTimes.Dequeue();
                }

                if (_requestTimes.Count >= MaxRequestsPerSecond)
                {
                    var oldestRequest = _requestTimes.Peek();
                    var waitTime = TimeSpan.FromSeconds(1) - (now - oldestRequest);
                    if (waitTime > TimeSpan.Zero)
                    {
                        Task.Delay(waitTime, cancellationToken).Wait();
                    }
                    _requestTimes.Dequeue();
                }

                _requestTimes.Enqueue(DateTime.UtcNow);
                _dailyRequestCount++;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static TimeSpan GetRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter != null)
        {
            if (response.Headers.RetryAfter.Delta.HasValue)
            {
                return response.Headers.RetryAfter.Delta.Value;
            }
            else if (response.Headers.RetryAfter.Date.HasValue)
            {
                var retryTime = response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                return retryTime > TimeSpan.Zero ? retryTime : TimeSpan.FromSeconds(5);
            }
        }

        return TimeSpan.FromSeconds(5);
    }
}
