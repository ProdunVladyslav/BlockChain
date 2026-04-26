using BlockChain.Model;

namespace BlockChain.HashingService
{
    public class MiningService
    {
        public static long MineBlock(Block block, int difficulty)
        {
            var target = new string('0', difficulty); // Create a target string consisting of '0's based on the specified difficulty level

            var stopWatch = System.Diagnostics.Stopwatch.StartNew(); // Start a stopwatch to measure mining duration
            while (true)
            {
                block.Nonce++; // Increment the nonce value to try a new hash
                string hash = HashingService.ComputeHash(block); // Compute the hash of the block with the current nonce
                if (block.Nonce % 100_000 == 0)
                {
                    Console.Write(".");
                }

                if (hash.StartsWith(target))
                {
                    Console.WriteLine("Found a valid hash!");
                    stopWatch.Stop(); // Stop the stopwatch once a valid hash is found
                    block.MiningDurationBlock = stopWatch.Elapsed.TotalSeconds; // Store the mining duration in seconds
                    return block.Nonce;
                }
            }
        }

        public static long MineBlockMultiThreaded(Block block, int difficulty)
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

        static void MineBlockThread(Block block, int difficulty, long startNonce, long step, ref long foundNonce, CancellationTokenSource cts)
        {
            var target = new string('0', difficulty); // Create a target string consisting of '0's based on the specified difficulty level
            long nonce = startNonce; // Start nonce based on the thread ID to ensure different starting points for each thread
            while (!cts.Token.IsCancellationRequested)
            {
                nonce += step; // Increment the nonce value by the step size to ensure different nonces for each thread
                string rawData = $"{block.Index}{block.Timestamp}{block.Data}{block.PreviousHash}{block.Author}{nonce}";
                string hash = HashingService.ComputeHash(rawData); // Compute the hash of the block with the current nonce
                if (hash.StartsWith(target))
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
