
using System;
using System.Collections.Generic;

namespace MarketInsight.Shared.Utils
{
    /// <summary>
    /// Lightweight retry/backoff helpers that don't force a Polly dependency
    /// in shared code. Use these delay sequences in your own retry loops.
    /// </summary>
    public static class RetryPolicy
    {
        /// <summary>
        /// Decorrelated jitter backoff (V2) as popularized by Polly.
        /// See: https://aws.amazon.com/blogs/architecture/exponential-backoff-and-jitter/
        /// </summary>
        public static IEnumerable<TimeSpan> DecorrelatedJitterBackoffV2(
            TimeSpan medianFirstRetryDelay,
            int retryCount,
            TimeSpan? maxDelay = null)
        {
            var rand = new Random();
            var sleep = medianFirstRetryDelay.TotalMilliseconds;
            var cap = (maxDelay ?? TimeSpan.FromSeconds(30)).TotalMilliseconds;

            for (int i = 0; i < retryCount; i++)
            {
                // exponential growth with jitter
                sleep = Math.Min(cap, rand.Next((int)medianFirstRetryDelay.TotalMilliseconds, (int)(sleep * 3)));
                yield return TimeSpan.FromMilliseconds(sleep);
            }
        }
    }
}
