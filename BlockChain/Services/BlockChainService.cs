using BlockChain.Model;
using BlockChain.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlockChain.HashingService
{
    public class BlockChainService
    {
        public List<Block> Chain { get; private set; } // List to hold the blocks in the blockchain
        private MiningService MiningService;
        private decimal MiningReward = 50; // Reward for mining a block (not currently used in this implementation)
        private int HalvingInterval = 5; // Number of blocks after which the mining reward is halved (not currently used in this implementation)

        public double Difficulty = 1.0; // Difficulty level for mining (number of leading zeros required in the hash)

        private readonly double _targetMiningTime = 2.5; // Target mining time in seconds for dynamic difficulty adjustment
        private readonly int difficultyAdjustmentInterval = 10; // Amount to adjust the difficulty by when mining time is too short or too long

        private readonly Dictionary<string, decimal> Balances = new Dictionary<string, decimal>(); // Dictionary to track balances of public keys (not currently used in this implementation)

        public BlockChainService()
        {
            Chain = new List<Block>(); // Initialize the blockchain as an empty list
            MiningService = new MiningService();
            AddGenesisBlock();
        }

        private void AddGenesisBlock()
        {
            Block genesisBlock = new Block(0, DateTime.Parse("2024-06-01T00:00:00Z"), new List<Transaction>(), "0", "Name", Difficulty);
            genesisBlock.Hash = HashingService.ComputeHash(genesisBlock); // Compute the hash for the genesis block
            Chain.Add(genesisBlock);
        }

        public void AddBlock(List<Transaction> transactions, string minerAddress)
        {
            foreach (Transaction transaction in transactions)
            {
                var isValid = TransactionService.ValidateTransaction(transaction); // Validate each transaction in the list using the TransactionService
                if (!isValid.isValid)
                {
                    Console.WriteLine($"Invalid transaction detected: {isValid.error}");
                    return;
                }
                var senderBalance = GetBalance(transaction.From); // Get the balance of the sender's public key
                if (senderBalance < transaction.Amount)
                {
                    Console.WriteLine($"Insufficient balance for transaction from {transaction.From}");
                    return;
                }
            }

            if (Chain.Count % HalvingInterval == 0)
            {
                MiningReward /= 2; // Halve the mining reward every 5 blocks (not currently used in this implementation)
            }

            transactions.Add(new Transaction("COINBASE", minerAddress, MiningReward)); // Add a reward transaction for the miner (author) to the list of transactions

            Block previousBlock = Chain.Last();
            Block newBlock = new Block(previousBlock.Index + 1, DateTime.UtcNow, transactions, previousBlock.Hash, minerAddress, Difficulty);
            MiningService.MineBlockMultiThreaded(newBlock, Difficulty);
            newBlock.Hash = HashingService.ComputeHash(newBlock); // ← only this one matters
            Chain.Add(newBlock);
            UpdateBalances(newBlock); // Update the balances based on the transactions in the new block
            if (newBlock.Index % difficultyAdjustmentInterval == 0)
            {
                //AdjustDifficulty(newBlock);
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

        public decimal GetBalance(string publicKey)
        {
            if (Balances.ContainsKey(publicKey))
            {
                return Balances[publicKey]; // Return the cached balance if it exists
            }
            return 0; // Return 0 if the balance is not found
        }

        public decimal UnoptimisedGetBalance(string publicKey)
        {
            decimal balance = 0;
            foreach (Block block in Chain)
            {
                foreach (Transaction transaction in block.Transactions)
                {
                    if (transaction.From == publicKey)
                    {
                        balance -= transaction.Amount; // Subtract the amount from the sender's balance
                    }
                    if (transaction.To == publicKey)
                    {
                        balance += transaction.Amount; // Add the amount to the recipient's balance
                    }
                }
            }
            return balance; // Return the calculated balance
        }

        private void UpdateBalances(Block block)
        {
            foreach (Transaction transaction in block.Transactions)
            {
                if (transaction.From != "COINBASE")
                {
                    if (!Balances.ContainsKey(transaction.From))
                    {
                        Balances[transaction.From] = 0;
                    }
                    Balances[transaction.From] -= transaction.Amount; // Subtract the amount from the sender's balance
                }
                if (!Balances.ContainsKey(transaction.To))
                {
                    Balances[transaction.To] = 0;
                }
                Balances[transaction.To] += transaction.Amount; // Add the amount to the recipient's balance
            }
        }

        public void RebuildState()
        {
            Balances.Clear(); // Clear the current balances
            foreach (Block block in Chain)
            {
                UpdateBalances(block);
            }
        }

        public void ImitateFailure()
        {
            Balances.Clear(); // Clear the balances to simulate a failure in the state management
        }

        public decimal GetTotalSupply()
        {
            decimal totalSupply = 0;
            foreach (Block block in Chain)
            {
                foreach (Transaction transaction in block.Transactions)
                {
                    if (transaction.From == "COINBASE")
                    {
                        totalSupply += transaction.Amount;
                    }
                }
            }
            return totalSupply;
        }

        public void SaveToFile(string filePath)
        {
            var snapshot = new ChainSnapshot
            {
                ExportedAt = DateTime.UtcNow,
                ChainLength = Chain.Count,
                Difficulty = Difficulty,
                TotalSupply = GetTotalSupply(),
                Balances = new Dictionary<string, decimal>(Balances), // copy so it's safe
                Chain = Chain
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            string json = JsonSerializer.Serialize(snapshot, options);
            File.WriteAllText(filePath, json);

            Console.WriteLine($"Chain saved to {filePath} ({new FileInfo(filePath).Length / 1024} KB)");
        }

        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }
            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            try
            {
                var snapshot = JsonSerializer.Deserialize<ChainSnapshot>(json, options);
                if (snapshot != null)
                {
                    Chain = snapshot.Chain ?? new List<Block>();
                    Difficulty = snapshot.Difficulty;
                    Balances.Clear();
                    if (snapshot.Balances != null)
                    {
                        foreach (var kvp in snapshot.Balances)
                        {
                            Balances[kvp.Key] = kvp.Value;
                        }
                    }
                    Console.WriteLine($"Chain loaded from {filePath} with {Chain.Count} blocks and total supply of {GetTotalSupply()}");
                    Console.WriteLine($"Validating Chain: {IsChainValid()}");
                }
                else
                {
                    Console.WriteLine($"Failed to deserialize chain from {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading chain from file: {ex.Message}");
            }
        }
    }
}
