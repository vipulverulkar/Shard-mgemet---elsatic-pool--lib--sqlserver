// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.SqlDatabase.ElasticScale
{
    using System;

    internal partial class TransientFaultHandling
    {
        /// <summary>
        /// A retry strategy with backoff parameters for calculating the exponential delay between retries.
        /// </summary>
        internal class ExponentialBackoff : RetryStrategy
        {
            private readonly int _retryCount;
            private readonly TimeSpan _minBackoff;
            private readonly TimeSpan _maxBackoff;
            private readonly TimeSpan _deltaBackoff;

            /// <summary>
            /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class. 
            /// </summary>
            public ExponentialBackoff()
                : this(DefaultClientRetryCount, DefaultMinBackoff, DefaultMaxBackoff, DefaultClientBackoff)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class with the specified retry settings.
            /// </summary>
            /// <param name="retryCount">The maximum number of retry attempts.</param>
            /// <param name="minBackoff">The minimum backoff time</param>
            /// <param name="maxBackoff">The maximum backoff time.</param>
            /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
            public ExponentialBackoff(int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
                : this(null, retryCount, minBackoff, maxBackoff, deltaBackoff, DefaultFirstFastRetry)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class with the specified name and retry settings.
            /// </summary>
            /// <param name="name">The name of the retry strategy.</param>
            /// <param name="retryCount">The maximum number of retry attempts.</param>
            /// <param name="minBackoff">The minimum backoff time</param>
            /// <param name="maxBackoff">The maximum backoff time.</param>
            /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
            public ExponentialBackoff(string name, int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff,
                TimeSpan deltaBackoff)
                : this(name, retryCount, minBackoff, maxBackoff, deltaBackoff, DefaultFirstFastRetry)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ExponentialBackoff"/> class with the specified name, retry settings, and fast retry option.
            /// </summary>
            /// <param name="name">The name of the retry strategy.</param>
            /// <param name="retryCount">The maximum number of retry attempts.</param>
            /// <param name="minBackoff">The minimum backoff time</param>
            /// <param name="maxBackoff">The maximum backoff time.</param>
            /// <param name="deltaBackoff">The value that will be used to calculate a random delta in the exponential delay between retries.</param>
            /// <param name="firstFastRetry">true to immediately retry in the first attempt; otherwise, false. The subsequent retries will remain subject to the configured retry interval.</param>
            public ExponentialBackoff(string name, int retryCount, TimeSpan minBackoff, TimeSpan maxBackoff,
                TimeSpan deltaBackoff, bool firstFastRetry)
                : base(name, firstFastRetry)
            {
                Guard.ArgumentNotNegativeValue(retryCount, "retryCount");
                Guard.ArgumentNotNegativeValue(minBackoff.Ticks, "minBackoff");
                Guard.ArgumentNotNegativeValue(maxBackoff.Ticks, "maxBackoff");
                Guard.ArgumentNotNegativeValue(deltaBackoff.Ticks, "deltaBackoff");
                Guard.ArgumentNotGreaterThan(minBackoff.TotalMilliseconds, maxBackoff.TotalMilliseconds, "minBackoff");

                _retryCount = retryCount;
                _minBackoff = minBackoff;
                _maxBackoff = maxBackoff;
                _deltaBackoff = deltaBackoff;
            }

            /// <summary>
            /// Returns the corresponding ShouldRetry delegate.
            /// </summary>
            /// <returns>The ShouldRetry delegate.</returns>
            public override ShouldRetry GetShouldRetry()
            {
                return delegate (int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
                {
                    if (currentRetryCount < _retryCount)
                    {
                        var random = new Random();

                        var delta =
                            (int)
                                ((Math.Pow(2.0, currentRetryCount) - 1.0) *
                                 random.Next((int)(_deltaBackoff.TotalMilliseconds * 0.8),
                                     (int)(_deltaBackoff.TotalMilliseconds * 1.2)));
                        var interval =
                            (int)
                                Math.Min(checked(_minBackoff.TotalMilliseconds + delta),
                                    _maxBackoff.TotalMilliseconds);

                        retryInterval = TimeSpan.FromMilliseconds(interval);

                        return true;
                    }

                    retryInterval = TimeSpan.Zero;
                    return false;
                };
            }
        }
    }
}
