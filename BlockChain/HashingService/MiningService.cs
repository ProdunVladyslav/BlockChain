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

                if(hash.StartsWith(target)) {
                    Console.WriteLine("Found a valid hash!");
                    stopWatch.Stop(); // Stop the stopwatch once a valid hash is found
                    block.MiningDurationBlock = stopWatch.Elapsed.TotalSeconds; // Store the mining duration in seconds
                    return block.Nonce;
                }
            }
        }

        public static long MineBlockMultiThreaded(Block block, int difficulty)
        {
            var target = new string('0', difficulty);
            int processorCount = Environment.ProcessorCount;
            long startNonce = block.Nonce;
            long foundNonce = -1;
            object lockObj = new();

            var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            using var cts = new CancellationTokenSource();

            try
            {
                Parallel.For(0, processorCount, new ParallelOptions
                {
                    MaxDegreeOfParallelism = processorCount,
                    CancellationToken = cts.Token
                },
                workerId =>
                {
                    long localNonce = startNonce + workerId;

                    while (!cts.Token.IsCancellationRequested)
                    {
                        var localBlock = block.ShallowCopy();
                        localBlock.Nonce = localNonce;

                        string hash = HashingService.ComputeHash(localBlock);

                        if (localNonce % 100_000 == 0)
                            Console.Write(".");

                        if (hash.StartsWith(target))
                        {
                            lock (lockObj)
                            {
                                if (foundNonce == -1 || localNonce < foundNonce)
                                    foundNonce = localNonce;
                            }
                            cts.Cancel();
                            return;
                        }

                        localNonce += processorCount; // stride by processor count so threads don't overlap
                    }
                });
            }
            catch (OperationCanceledException) { }

            if (foundNonce != -1)
            {
                block.Nonce = foundNonce;
                stopWatch.Stop();
                block.MiningDurationBlock = stopWatch.Elapsed.TotalSeconds;
                Console.WriteLine($"\nFound a valid hash! Nonce: {foundNonce} Seconds taken to mine: {block.MiningDurationBlock:F2}");
            }

            return block.Nonce;
        }
    }
}
