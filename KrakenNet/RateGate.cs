using System;
using System.Threading;
using System.Threading.Tasks;

namespace KrakenNet
{
    public sealed class RateGate
    {
        // Note: SemaphoreSlim only needs to be disposed when we use its wait handle,
        // and we do not use it here.
        private readonly SemaphoreSlim _semaphore;

        public RateGate(int occurences, TimeSpan period)
        {
            if (occurences <= 0)
                throw new ArgumentOutOfRangeException(nameof(occurences), "Number of occurences must be greater than 0");

            if (period <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(period), "Period must be strictly positive");

            Occurences = occurences;
            Period = period;

            _semaphore = new SemaphoreSlim(occurences, occurences);
        }

        public int Occurences { get; }
        public TimeSpan Period { get; }

        public Task WaitForLimitAsync() => WaitForLimitAsync(Timeout.Infinite);

        public async Task<bool> WaitForLimitAsync(int timeoutMs)
        {
            if (!await _semaphore.WaitAsync(timeoutMs))
                return false;

            Task ignored = Task.Delay(Period).ContinueWith(t => _semaphore.Release());
            return true;
        }
    }
}
