using BlockChain.Model;

namespace BlockChain.HashingService
{
    public class MiningService
    {
        public static long MineBlockMultiThreaded(Block block, double difficulty)
        {
            int numberOfThreads = Environment.ProcessorCount; // Get the number of available CPU cores
            var threads = new Thread[numberOfThreads];
            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            long foundNonce = -1;
            using var cts = new CancellationTokenSource();

            for (int i = 0; i < numberOfThreads; i++)
            {
                int workerId = i; // capture for closure
                threads[i] = new Thread(() =>
                {
                    MineBlockThread(block, difficulty, workerId, numberOfThreads, ref foundNonce, cts);
                });
                threads[i].Start();
            }

            foreach (var thread in threads)
                thread.Join();

            if (foundNonce > -1)
            {
                Console.WriteLine($"Valid hash found with nonce: {foundNonce}");
                block.Nonce = foundNonce;
                block.MiningDurationBlock = stopWatch.Elapsed.TotalSeconds; // Store the mining duration in seconds
                block.DifficultyAtMining = difficulty;
                return foundNonce;
            }

            return -1;
        }

        static void MineBlockThread(Block block, double difficulty, long startNonce, long step, ref long foundNonce, CancellationTokenSource cts)
        {
            int wholePart = (int)difficulty; // Get the whole number part of the difficulty level
            var target = new string('0', wholePart); // Create a target string consisting of '0's based on the specified difficulty level
            double fraction = difficulty - wholePart;
            string hexChars = "0123456789abcdef";
            char fractionalChar = hexChars[15 - Math.Min(15, (int)(fraction * 16))];
            long nonce = startNonce; // Start nonce based on the thread ID to ensure different starting points for each thread
            while (!cts.Token.IsCancellationRequested)
            {
                nonce += step; // Increment the nonce value by the step size to ensure different nonces for each thread
                string rawData = $"{block.Index}{block.Timestamp}{block.Data}{block.PreviousHash}{block.Author}{nonce}";
                string hash = HashingService.ComputeHash(rawData); // Compute the hash of the block with the current nonce
                if (hash.StartsWith(target) && hash[wholePart] <= fractionalChar)
                {
                    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} found a valid hash!");
                    Interlocked.CompareExchange(ref foundNonce, nonce, -1);
                    cts.Cancel(); 
                    return; // Exit the thread once a valid hash is found
                }
            }
        }
    }
}
