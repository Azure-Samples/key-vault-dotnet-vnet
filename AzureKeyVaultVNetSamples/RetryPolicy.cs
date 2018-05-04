using System.Collections.Generic;
using System.Net;

namespace AzureKeyVaultVNetSamples
{
    /// <summary>
    /// Models a policy for retrying an http request, based on expected response status codes.
    /// </summary>
    public sealed class RetryPolicy
    {
        public RetryPolicy(int initialBackoff, int numAttempts, HashSet<HttpStatusCode> continueOn, HashSet<HttpStatusCode> retryOn, HashSet<HttpStatusCode> abortOn = null)
        {
            InitialBackoff = initialBackoff;
            MaxAttempts = numAttempts;
            ContinueOn = continueOn;
            RetryOn = retryOn;
            AbortOn = abortOn;
        }

        /// <summary>
        /// Initial wait following the first failed/throttled call. 
        /// The interval is doubled for each subsequent attempt.
        /// </summary>
        public int InitialBackoff { get; set; }

        /// <summary>
        /// Maximum number of attempts to try, inclusive of the first one.
        /// </summary>
        public int MaxAttempts { get; set; }

        /// <summary>
        /// Status codes considered successful (i.e. the request will not be re-attempted.)
        /// </summary>
        public HashSet<HttpStatusCode> ContinueOn { get; set; }

        /// <summary>
        /// Status codes on which to retry, after a wait.
        /// </summary>
        public HashSet<HttpStatusCode> RetryOn { get; set; }

        /// <summary>
        /// Status codes on which to abort the execution of the retry block.
        /// </summary>
        public HashSet<HttpStatusCode> AbortOn { get; set; }
    }
}
