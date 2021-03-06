// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.SqlDatabase.ElasticScale
{
    using System;
    using System.Threading;

    internal partial class TransientFaultHandling
    {
        /// <summary>
        /// Defines a callback delegate that will be invoked whenever a retry condition is encountered.
        /// </summary>
        /// <param name="retryCount">The current retry attempt count.</param>
        /// <param name="lastException">The exception that caused the retry conditions to occur.</param>
        /// <param name="delay">The delay that indicates how long the current thread will be suspended before the next iteration is invoked.</param>
        /// <returns><see langword="true"/> if a retry is allowed; otherwise, <see langword="false"/>.</returns>
        internal delegate bool ShouldRetry(int retryCount, Exception lastException, out TimeSpan delay);

        /// <summary>
        /// Represents a retry strategy that determines the number of retry attempts and the interval between retries.
        /// </summary>
        internal abstract class RetryStrategy
        {
            #region Public members

            /// <summary>
            /// Represents the default number of retry attempts.
            /// </summary>
            public static readonly int DefaultClientRetryCount = 10;

            /// <summary>
            /// Represents the default amount of time used when calculating a random delta in the exponential delay between retries.
            /// </summary>
            public static readonly TimeSpan DefaultClientBackoff = TimeSpan.FromSeconds(10.0);

            /// <summary>
            /// Represents the default maximum amount of time used when calculating the exponential delay between retries.
            /// </summary>
            public static readonly TimeSpan DefaultMaxBackoff = TimeSpan.FromSeconds(30.0);

            /// <summary>
            /// Represents the default minimum amount of time used when calculating the exponential delay between retries.
            /// </summary>
            public static readonly TimeSpan DefaultMinBackoff = TimeSpan.FromSeconds(1.0);

            /// <summary>
            /// Represents the default interval between retries.
            /// </summary>
            public static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromSeconds(1.0);

            /// <summary>
            /// Represents the default time increment between retry attempts in the progressive delay policy.
            /// </summary>
            public static readonly TimeSpan DefaultRetryIncrement = TimeSpan.FromSeconds(1.0);

            /// <summary>
            /// Represents the default flag indicating whether the first retry attempt will be made immediately,
            /// whereas subsequent retries will remain subject to the retry interval.
            /// </summary>
            public static readonly bool DefaultFirstFastRetry = true;

            #endregion

            private static Lazy<RetryStrategy> s_noRetry = new Lazy<RetryStrategy>(() => new FixedInterval(0, DefaultRetryInterval), LazyThreadSafetyMode.PublicationOnly);
            private static Lazy<RetryStrategy> s_defaultFixed = new Lazy<RetryStrategy>(() => new FixedInterval(DefaultClientRetryCount, DefaultRetryInterval), LazyThreadSafetyMode.PublicationOnly);

            private static Lazy<RetryStrategy> s_defaultProgressive = new Lazy<RetryStrategy>(() => new Incremental(DefaultClientRetryCount,
                DefaultRetryInterval, DefaultRetryIncrement), LazyThreadSafetyMode.PublicationOnly);

            private static Lazy<RetryStrategy> s_defaultExponential = new Lazy<RetryStrategy>(() => new ExponentialBackoff(DefaultClientRetryCount,
                DefaultMinBackoff, DefaultMaxBackoff, DefaultClientBackoff), LazyThreadSafetyMode.PublicationOnly);

            /// <summary>
            /// Returns a default policy that performs no retries, but invokes the action only once.
            /// </summary>
            public static RetryStrategy NoRetry
            {
                get { return s_noRetry.Value; }
            }

            /// <summary>
            /// Returns a default policy that implements a fixed retry interval configured with the <see cref="RetryStrategy.DefaultClientRetryCount"/> and <see cref="RetryStrategy.DefaultRetryInterval"/> parameters.
            /// The default retry policy treats all caught exceptions as transient errors.
            /// </summary>
            public static RetryStrategy DefaultFixed
            {
                get { return s_defaultFixed.Value; }
            }

            /// <summary>
            /// Returns a default policy that implements a progressive retry interval configured with the <see cref="RetryStrategy.DefaultClientRetryCount"/>, <see cref="RetryStrategy.DefaultRetryInterval"/>, and <see cref="RetryStrategy.DefaultRetryIncrement"/> parameters.
            /// The default retry policy treats all caught exceptions as transient errors.
            /// </summary>
            public static RetryStrategy DefaultProgressive
            {
                get { return s_defaultProgressive.Value; }
            }

            /// <summary>
            /// Returns a default policy that implements a random exponential retry interval configured with the <see cref="RetryStrategy.DefaultClientRetryCount"/>, <see cref="RetryStrategy.DefaultMinBackoff"/>, <see cref="RetryStrategy.DefaultMaxBackoff"/>, and <see cref="RetryStrategy.DefaultClientBackoff"/> parameters.
            /// The default retry policy treats all caught exceptions as transient errors.
            /// </summary>
            public static RetryStrategy DefaultExponential
            {
                get { return s_defaultExponential.Value; }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="RetryStrategy"/> class. 
            /// </summary>
            /// <param name="name">The name of the retry strategy.</param>
            /// <param name="firstFastRetry">true to immediately retry in the first attempt; otherwise, false. The subsequent retries will remain subject to the configured retry interval.</param>
            protected RetryStrategy(string name, bool firstFastRetry)
            {
                this.Name = name;
                this.FastFirstRetry = firstFastRetry;
            }

            /// <summary>
            /// Gets or sets a value indicating whether the first retry attempt will be made immediately,
            /// whereas subsequent retries will remain subject to the retry interval.
            /// </summary>
            public bool FastFirstRetry { get; set; }

            /// <summary>
            /// Gets the name of the retry strategy.
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Returns the corresponding ShouldRetry delegate.
            /// </summary>
            /// <returns>The ShouldRetry delegate.</returns>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
                Justification = "Needs to be a new instance each time.")]
            public abstract ShouldRetry GetShouldRetry();
        }
    }
}