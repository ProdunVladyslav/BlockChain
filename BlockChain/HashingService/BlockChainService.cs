using BlockChain.Model;

namespace BlockChain.HashingService
{
    public class BlockChainService
    {
        public List<Block> Chain { get; private set; } // List to hold the blocks in the blockchain

        public int Difficulty = 0; // Difficulty level for mining (number of leading zeros required in the hash)

        private readonly double _targetMiningTime = 1; // Target mining time in seconds for dynamic difficulty adjustment
        private readonly int difficultyAdjustmentInterval = 10; // Amount to adjust the difficulty by when mining time is too short or too long
        public BlockChainService()
        {
            Chain = new List<Block>(); // Initialize the blockchain as an empty list
            AddGenesisBlock();
        }

        private void AddGenesisBlock()
        {
            Block genesisBlock = new Block(0, DateTime.Parse("2024-06-01T00:00:00Z"), "Genesis Block", "0", "Name", Difficulty);
            genesisBlock.Hash = HashingService.ComputeHash(genesisBlock); // Compute the hash for the genesis block
            Chain.Add(genesisBlock);
        }

        public void AddBlock(string data, string author)
        {
            Block previousBlock = Chain.Last();
            Block newBlock = new Block(previousBlock.Index + 1, DateTime.UtcNow, data, previousBlock.Hash, author, Difficulty);
            // removed first ComputeHash here
            MiningService.MineBlockMultiThreaded(newBlock, Difficulty);
            newBlock.Hash = HashingService.ComputeHash(newBlock); // ← only this one matters
            Chain.Add(newBlock);
            if (newBlock.Index % difficultyAdjustmentInterval == 0)
            {
                AdjustDifficulty(newBlock);
            }
        }

        public void PrintDifficultyHistory()
        {
            Console.WriteLine("Difficulty History:");
            for (int i = 0; i < Chain.Count; i++)
            {
                Console.WriteLine($"Block {Chain[i].Index}: Difficulty {Chain[i].DifficultyAtMining}; Mining Duration: {Chain[i].MiningDurationBlock:F2} seconds");
            }
        }

        private void AdjustDifficulty(Block newBlock)
        {
            var recentBlocks = Chain
                .Skip(Math.Max(0, Chain.Count - difficultyAdjustmentInterval))
                .Take(difficultyAdjustmentInterval)
                .ToList();

            // Use median instead of mean to ignore outliers
            var sorted = recentBlocks
                .Select(b => b.MiningDurationBlock)
                .OrderBy(t => t)
                .ToList();

            double medianMiningTime;
            int count = sorted.Count;
            if (count % 2 == 0)
                medianMiningTime = (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            else
                medianMiningTime = sorted[count / 2];

            if (medianMiningTime < _targetMiningTime)
            {
                Console.WriteLine($"Median mining time {medianMiningTime:F2}s is less than target. Increasing difficulty.");
                IncreaseDifficulty(medianMiningTime);
            }
            else if (medianMiningTime > _targetMiningTime)
            {
                Console.WriteLine($"Median mining time {medianMiningTime:F2}s is greater than target. Decreasing difficulty.");
                DecreaseDifficulty(medianMiningTime);
            }
        }

        public void IncreaseDifficulty(double medianMiningTime)
        {
            double ratio = _targetMiningTime / medianMiningTime;
            int delta = (int)Math.Min(Math.Max(1, Math.Log(ratio, 16)), 6);
            Difficulty += delta;
        }

        public void DecreaseDifficulty(double medianMiningTime)
        {
            double ratio = medianMiningTime / _targetMiningTime;
            int delta = (int)Math.Min(Math.Max(1, Math.Log(ratio, 16)), 6);
            Difficulty = Math.Max(1, Difficulty - delta);
        }

        public bool IsChainValid()
        {
            for (int i = 1; i < Chain.Count; i++)
            {
                Block currentBlock = Chain[i];
                Block previousBlock = Chain[i - 1];
                if (currentBlock.Hash != HashingService.ComputeHash(currentBlock)) // Check if the current block's hash is valid
                {
                    return false;
                }
                if (currentBlock.PreviousHash != previousBlock.Hash) // Check if the current block's previous hash matches the previous block's hash
                {
                    return false;
                }
                if (!currentBlock.Hash.StartsWith(new string('0', currentBlock.DifficultyAtMining))) // Check if the current block's hash meets the difficulty requirement
                {
                    return false;
                }
            }
            return true; // If all blocks are valid, return true
        }
    }
}
