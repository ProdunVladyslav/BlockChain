using BlockChain.Model;
using BlockChain.Services;

namespace BlockChain.HashingService
{
    public class BlockChainService
    {
        public List<Block> Chain { get; private set; } // List to hold the blocks in the blockchain

        public double Difficulty = 1.0; // Difficulty level for mining (number of leading zeros required in the hash)

        private readonly double _targetMiningTime = 2.5; // Target mining time in seconds for dynamic difficulty adjustment
        private readonly int difficultyAdjustmentInterval = 10; // Amount to adjust the difficulty by when mining time is too short or too long

        public BlockChainService()
        {
            Chain = new List<Block>(); // Initialize the blockchain as an empty list
            AddGenesisBlock();
        }

        private void AddGenesisBlock()
        {
            Block genesisBlock = new Block(0, DateTime.Parse("2024-06-01T00:00:00Z"), new List<Transaction>(), "0", "Name", Difficulty);
            genesisBlock.Hash = HashingService.ComputeHash(genesisBlock); // Compute the hash for the genesis block
            Chain.Add(genesisBlock);
        }

        public void AddBlock(List<Transaction> transactions, string author)
        {
            foreach (Transaction transaction in transactions)
            {
                var isValid = TransactionService.ValidateTransaction(transaction); // Validate each transaction in the list using the TransactionService
                if (!isValid.isValid)
                {
                    Console.WriteLine($"Invalid transaction detected: {isValid.error}");
                    return;
                }
            }

            //transactions.Add(new Transaction("COINBASE", author, 0)); // Add a reward transaction for the miner (author) to the list of transactions

            Block previousBlock = Chain.Last();
            Block newBlock = new Block(previousBlock.Index + 1, DateTime.UtcNow, transactions, previousBlock.Hash, author, Difficulty);
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

            double oldDifficulty = Difficulty;
            double ratio = _targetMiningTime / medianMiningTime;

            // Limit to max +0.25 / -0.25 difficulty levels per adjustment
            double maxDifficulty = oldDifficulty + 0.25;
            double minDifficulty = Math.Max(1.0, oldDifficulty - 0.25);

            Difficulty = Math.Clamp(oldDifficulty * ratio, minDifficulty, maxDifficulty);

            Console.WriteLine($"Median: {medianMiningTime:F2}s, Target: {_targetMiningTime}s, Difficulty adjusted from {oldDifficulty:F2} to {Difficulty:F2}");
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
                int wholePart = (int)currentBlock.DifficultyAtMining;
                double fraction = currentBlock.DifficultyAtMining - wholePart;
                string hexChars = "0123456789abcdef";
                char fractionalChar = hexChars[15 - Math.Min(15, (int)(fraction * 16))];
                if (!currentBlock.Hash.StartsWith(new string('0', wholePart)) || currentBlock.Hash[wholePart] > fractionalChar) // Check if the current block's hash meets the difficulty requirement
                {
                    return false;
                }
            }
            return true; // If all blocks are valid, return true
        }
    }
}
