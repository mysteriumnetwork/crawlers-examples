using System;
using System.Threading.Tasks;

namespace Crawler
{
    class Link
    {
        public string uri;
        public int depth;

        public Link(string uri, int depth)
        {
            this.uri = uri;
            this.depth = depth;
        }
    }

    public static class RetryHelper
    {
        public static async Task RetryOnExceptionAsync(
            int times, TimeSpan delay, Func<Task> operation)
        {
            if (times <= 0)
                throw new ArgumentOutOfRangeException(nameof(times));

            var attempts = 0;
            do
            {
                try
                {
                    attempts++;
                    await operation();
                    break;
                }
                catch
                {
                    Console.WriteLine($"Exception on attempt {attempts} of {times}. Will retry after sleeping for {delay}.");
                    if (attempts == times)
                        throw;

                    await Task.Delay(delay);
                }
            } while (true);
        }
    }
}
