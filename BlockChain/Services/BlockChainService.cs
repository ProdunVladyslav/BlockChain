using BlockChain.Model;
using BlockChain.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlockChain.Chain
{
    public class BlockChainService
    {
        public List<Block> Chain { get; private set; } // List to hold the blocks in the blockchain
        private MiningService _miningService;
        public decimal _miningReward = 50m; // Reward for mining a block (not currently used in this implementation)
        private int _halvingInterval = 5; // Number of blocks after which the mining reward is halved (not currently used in this implementation)

        public double Difficulty = 1.0; // Difficulty level for mining (number of leading zeros required in the hash)
        public readonly int MaxTransactionsPerBlock = 100; // Maximum number of transactions allowed in a single block to prevent excessively large blocks
        public decimal NetworkBaseFee { get; set; } = 1.0m;

        public List<Transaction> PendingTransactions = new List<Transaction>(); // List to hold pending transactions that have not yet been included in a block

        private readonly double _targetMiningTime = 5; // Target mining time in seconds for dynamic difficulty adjustment
        private readonly int difficultyAdjustmentInterval = 10; // Amount to adjust the difficulty by when mining time is too short or too long
        private readonly int _rateLimitPerSender = 3; // Maximum number of pending transactions allowed per sender to prevent spamming
        private readonly int _ttlTransactionsSeconds = 10; // Time-to-live for transactions in the mempool (not currently implemented, but could be used to remove old transactions)

        private readonly Dictionary<string, decimal> Balances = new Dictionary<string, decimal>(); // Dictionary to track balances of public keys (not currently used in this implementation)

        // Identity baked into the genesis block. All nodes run by the same student must share it
        // (set the STUDENT_ID env var identically on all 3 terminals) so their genesis — and thus
        // their whole chain — matches and can be shared. Different students get a different genesis.
        public string StudentId { get; }

        public BlockChainService()
        {
            StudentId = Environment.GetEnvironmentVariable("STUDENT_ID") ?? "STUDENT_DEFAULT";
            Chain = new List<Block>(); // Initialize the blockchain as an empty list
            _miningService = new MiningService();
            AddGenesisBlock();
        }

        private void AddGenesisBlock()
        {
            var genesisTransactions = new List<Transaction>();
            // Author = StudentId so the genesis hash is unique per student but identical across
            // that student's own nodes. Mined deterministically so the hash is reproducible.
            Block genesisBlock = new Block(0, DateTime.Parse("2024-06-01T00:00:00Z"), genesisTransactions, "0", StudentId, Difficulty);
            genesisBlock.MerkleRoot = HashingService.BuildMerkleRoot(genesisTransactions);
            MiningService.MineBlockDeterministic(genesisBlock, Difficulty);
            Chain.Add(genesisBlock);
        }

        public void AddTransactionToMempool(Transaction transaction)
        {

            if (transaction.Fee < NetworkBaseFee)
            {
                Console.WriteLine($"Transaction from {transaction.From} rejected: Fee {transaction.Fee} is below the network base fee of {NetworkBaseFee}.");
                return;
            }

            if (transaction.From != "COINBASE")
            {
                var balance = GetBalance(transaction.From);
                var totalPendingAmount = PendingTransactions.Where(x => x.From == transaction.From).Sum(x => x.Amount + x.Fee);
                //if (balance < totalPendingAmount + transaction.Amount + transaction.Fee)
                //{
                //    Console.WriteLine($"Transaction from {transaction.From} rejected: Insufficient funds.");
                //    return;
                //}
            }

            var isValid = TransactionService.ValidateTransaction(transaction); // Validate the transaction using the TransactionService
            var rateLimited = PendingTransactions.Where(x => x.From == transaction.From).Count() >= _rateLimitPerSender; // Simple rate limit: max 5 pending transactions per sender
            if (isValid.isValid && !rateLimited)
            {
                PendingTransactions.Add(transaction);
            }
            else if (rateLimited)
            {
                throw new InvalidOperationException("Spam detected.");
            }
            else
            {
                Console.WriteLine($"Invalid transaction from {transaction.From} rejected: {isValid.error}");
            }
        }

        public event Action<Block>? BlockMined;

        public void MineBlock(string minerPublicKey)
        {
            EvictStaleTransactions(_ttlTransactionsSeconds); // Remove old transactions from the mempool before mining a new block
            var transactionsToInclude = new List<Transaction>(PendingTransactions.OrderByDescending(x => x.Fee).Take(MaxTransactionsPerBlock)); // Create a copy of the pending transactions to include in the new block
            var totalFees = transactionsToInclude.Sum(t => (t.Fee - NetworkBaseFee)); // Calculate the total fees from the transactions to include in the block

            var totalReward = _miningReward + totalFees; // Calculate the total reward for mining the block (base reward + transaction fees)

            var rewardTransaction = new Transaction
            (
                from: "COINBASE",
                to: minerPublicKey,
                amount: totalReward,
                fee: 0m
            );

            transactionsToInclude.Add(rewardTransaction);

            var lastBlock = Chain.Last();
            Block newBlock = new Block(lastBlock.Index + 1, DateTime.UtcNow, transactionsToInclude, lastBlock.Hash, minerPublicKey, Difficulty);
            newBlock.MerkleRoot = HashingService.BuildMerkleRoot(transactionsToInclude); // Compute the Merkle root for the transactions in the new block

            MiningService.MineBlockMultiThreaded(newBlock, Difficulty); // Mine the new block using the MiningService
            Chain.Add(newBlock); // Add the new block to the blockchain
            PendingTransactions.RemoveAll(t => transactionsToInclude.Contains(t)); // Remove the included transactions from the mempool
            UpdateBalances(newBlock); // Update the balances based on the transactions in the new block

            BlockMined?.Invoke(newBlock);

            if (newBlock.Index % difficultyAdjustmentInterval == 0)
            {
                AdjustDifficulty();
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

        private void AdjustDifficulty()
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

        public bool IsChainValid() => IsChainValid(Chain);

        public bool IsChainValid(List<Block> chain)
        {
            var tempBalances = new Dictionary<string, decimal>();

            // 1. Empty or missing genesis is invalid
            if (chain == null || chain.Count == 0) return false;

            // 2. Genesis must match ours — otherwise this is a foreign chain,
            //    not a fork of the same network
            if (chain[0].Hash != Chain[0].Hash) return false;

            // 3. Walk every non-genesis block
            for (int i = 1; i < chain.Count; i++)
            {
                Block currentBlock = chain[i];
                Block previousBlock = chain[i - 1];

                // a. Index must be sequential
                if (currentBlock.Index != previousBlock.Index + 1) return false;

                // b. Hash must match the block's actual contents (no tampering)
                if (currentBlock.Hash != HashingService.ComputeHash(currentBlock)) return false;

                // c. PreviousHash must link to the previous block (no gaps / swaps)
                if (currentBlock.PreviousHash != previousBlock.Hash) return false;

                // d. Hash must meet the difficulty target that was declared at mining
                if (!MeetsDifficulty(currentBlock)) return false;

                // e. Every non-coinbase transaction must have a valid signature
                int coinbaseCount = 0;
                foreach (var tx in currentBlock.Transactions)
                {
                    if (tx.From == "COINBASE")
                    {
                        coinbaseCount++;
                        continue; // coinbase has no signature to verify
                    }

                    if (tx.From != "COINBASE")
                    {
                        decimal senderBalance = tempBalances.ContainsKey(tx.From) ? tempBalances[tx.From] : GetBalance(tx.From);
                        if (senderBalance < tx.Amount + tx.Fee) return false; // Insufficient funds
                        tempBalances[tx.From] = senderBalance - (tx.Amount + tx.Fee); // Deduct from sender's balance
                    }

                    var (isValid, _) = TransactionService.ValidateTransaction(tx);
                    if (!isValid)
                    {
                        // Log security alert to file
                        LogSecurityAlert(tx);
                        return false;
                    }

                    if (!tempBalances.ContainsKey(tx.From))
                    {
                        tempBalances[tx.From] = 0;
                    }
                    if (!tempBalances.ContainsKey(tx.To))
                    {
                        tempBalances[tx.To] = 0;
                    }

                    tempBalances[tx.To] += tx.Amount;
                }

                // f. Exactly one coinbase per block (no double-rewarding)
                if (coinbaseCount != 1) return false;
            }

            return true;
        }

        public List<string> AnalyzeChain()
        {
            var errors = new List<string>();

            for (int i = 1; i < Chain.Count; i++)
            {
                Block currentBlock = Chain[i];
                Block previousBlock = Chain[i - 1];

                // hsah doesn't match data
                string expectedHash = HashingService.ComputeHash(currentBlock);
                if (currentBlock.Hash != expectedHash)
                {
                    errors.Add($"error in block{currentBlock.Index}: " +
                               $"hash doesent meed data (Data/Timestamp/Nonce changed). " +
                               $"Expected: {expectedHash[..12]}... Found: {currentBlock.Hash[..Math.Min(12, currentBlock.Hash.Length)]}...");
                }

                // doesn't meet difficulty
                int wholePart = (int)currentBlock.DifficultyAtMining;
                double fraction = currentBlock.DifficultyAtMining - wholePart;
                string hexChars = "0123456789abcdef";
                char fractionalChar = hexChars[15 - Math.Min(15, (int)(fraction * 16))];

                bool meetsLeadingZeros = currentBlock.Hash.Length > wholePart &&
                                         currentBlock.Hash.StartsWith(new string('0', wholePart));
                bool meetsFractional = meetsLeadingZeros && currentBlock.Hash[wholePart] <= fractionalChar;

                if (!meetsLeadingZeros || !meetsFractional)
                {
                    errors.Add($"errror in block {currentBlock.Index}: " +
                               $"Hash does not meet current difficulty (Difficulty = {currentBlock.DifficultyAtMining:F2}). " +
                               $"Hash: {currentBlock.Hash[..Math.Min(16, currentBlock.Hash.Length)]}...");
                }

                // broken chain
                if (currentBlock.PreviousHash != previousBlock.Hash)
                {
                    errors.Add($"error in block {currentBlock.Index}: " +
                               $"Broke chain (PreviousHash doesnt match with {currentBlock.PreviousHash}). " +
                               $"Expected: {previousBlock.Hash[..12]}... Found: {currentBlock.PreviousHash[..Math.Min(12, currentBlock.PreviousHash.Length)]}...");
                }
            }

            if (errors.Count == 0)
                Console.WriteLine("Everything's fine with chain");
            else
                errors.ForEach(e => Console.WriteLine($"error {e}"));

            return errors;
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
                    Balances[transaction.From] -= (transaction.Amount + transaction.Fee); // not + fee - NetworkBaseFee// Subtract the amount from the sender's balance
                    if (Balances[transaction.From] < 0)
                        throw new InvalidOperationException($"Negative balance for {transaction.From} after processing transaction {transaction.Id}");
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

        private void RevertBalances(Block block)
        {
            foreach (Transaction transaction in block.Transactions)
            {
                if (transaction.From != "COINBASE")
                {
                    if (!Balances.ContainsKey(transaction.From))
                        Balances[transaction.From] = 0;

                    // Undo the deduction: give the sender back what they spent
                    Balances[transaction.From] += (transaction.Amount + transaction.Fee);
                }

                if (!Balances.ContainsKey(transaction.To))
                    Balances[transaction.To] = 0;

                // Undo the credit: take back what the recipient received
                Balances[transaction.To] -= transaction.Amount;
            }
        }

        public void ReplaceChain(List<Block> newChain)
        {
            if (newChain.Count <= Chain.Count) return;
            if (!IsChainValid(newChain)) return;
            if (newChain.Sum(x => x.DifficultyAtMining) <= Chain.Sum(x => x.DifficultyAtMining)) return;

            Balances.Clear();
            foreach (var block in newChain)
            {
                UpdateBalances(block);
            }

            var minedTxIds = new HashSet<Guid>(Chain.SelectMany(b => b.Transactions).Select(t => t.Id));
            PendingTransactions.RemoveAll(t => minedTxIds.Contains(t.Id));
            Chain = newChain;
        }

        private static int FindForkIndex(List<Block> oldChain, List<Block> newChain)
        {
            int min = Math.Min(oldChain.Count, newChain.Count);
            for (int i = 0; i < min; i++)
            {
                if (oldChain[i].Hash != newChain[i].Hash)
                    return i;
            }
            return min; // one chain is a strict prefix of the other — no real fork
        }

        // Add this to BlockChainService
        public BlockChainService Clone()
        {
            var clone = new BlockChainService(); // builds with a fresh genesis

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Deep-copy the chain via JSON round-trip
            string chainJson = JsonSerializer.Serialize(Chain, options);
            clone.Chain = JsonSerializer.Deserialize<List<Block>>(chainJson, options) ?? new List<Block>();

            // Deep-copy mempool
            string mempoolJson = JsonSerializer.Serialize(PendingTransactions, options);
            clone.PendingTransactions = JsonSerializer.Deserialize<List<Transaction>>(mempoolJson, options)
                                        ?? new List<Transaction>();

            clone.Difficulty = Difficulty;
            clone.NetworkBaseFee = NetworkBaseFee;
            clone.RebuildState(); // rebuild Balances from the copied chain

            return clone;
        }

        private static void LogSecurityAlert(Transaction transaction)
        {
            var alertMessage = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] SECURITY ALERT: Invalid transaction detected!\n" +
                              $"  From: {transaction.From}\n" +
                              $"  To: {transaction.To}\n" +
                              $"  Amount: {transaction.Amount}\n" +
                              $"  Fee: {transaction.Fee}\n" +
                              $"  Timestamp: {transaction.Timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                              new string('-', 50) + "\n";

            File.AppendAllText("security_alerts.txt", alertMessage);
            Console.WriteLine("Security alert logged to security_alerts.txt");
        }

        private static void AddViolation(AuditReport report, int index, string detail)
        {
            if (!report.CompromisedBlockIndexes.Contains(index))
                report.CompromisedBlockIndexes.Add(index);

            if (!report.ViolationDetails.TryGetValue(index, out var details))
            {
                details = new List<string>();
                report.ViolationDetails[index] = details;
            }
            details.Add(detail);

            report.IsChainValid = false;
        }

        private static bool MeetsDifficulty(Block block)
        {
            int wholePart = (int)block.DifficultyAtMining;
            double fraction = block.DifficultyAtMining - wholePart;
            string hexChars = "0123456789abcdef";
            char fractionalChar = hexChars[15 - Math.Min(15, (int)(fraction * 16))];

            if (block.Hash.Length <= wholePart) return false;
            if (!block.Hash.StartsWith(new string('0', wholePart))) return false;
            if (block.Hash[wholePart] > fractionalChar) return false;

            return true;
        }

        public AuditReport RunFullAudit(List<Block> chain)
        {
            var report = new AuditReport
            {
                CompromisedBlockIndexes = new List<int>(),
                IsChainValid = true,
                ViolationDetails = new Dictionary<int, List<string>>()
            };

            for (int i = 0; i < chain.Count; i++)
            {
                // 1
                if (i > 0 && chain[i].PreviousHash != chain[i - 1].Hash)
                    AddViolation(report, i, "Invalid PrevHash");

                // 2
                if (chain[i].MerkleRoot != HashingService.BuildMerkleRoot(chain[i].Transactions))
                    AddViolation(report, i, "Invalid MerkleRoot");

                // 3
                if (!MeetsDifficulty(chain[i]))
                    AddViolation(report, i, "Invalid Difficulty");
            }

            return report;
        }

        public Block FindAttackOrigin(AuditReport report, List<Block> chain)
        {
            foreach (var entry in report.ViolationDetails.OrderBy(x => x.Key))
            {
                if (entry.Value.Any(v => v != "Invalid PrevHash"))
                    return chain[entry.Key];
            }
            return null;
        }

        public string GenerateForensicReport(AuditReport report, Block attackOrigin)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== FORENSIC AUDIT REPORT ===");
            sb.AppendLine($"Chain status: {(report.IsChainValid ? "VALID" : "COMPROMISED")}");

            if (attackOrigin != null)
                sb.AppendLine($"Attack origin: Block #{attackOrigin.Index} (timestamp: {attackOrigin.Timestamp:yyyy-MM-dd HH:mm:ss})");
            else
                sb.AppendLine("Attack origin: none identified");

            sb.AppendLine($"Total affected blocks: {report.CompromisedBlockIndexes.Count}");
            sb.AppendLine();

            sb.AppendLine("VIOLATION LOG:");
            foreach (var entry in report.ViolationDetails.OrderBy(x => x.Key))
            {
                foreach (var violation in entry.Value)
                    sb.AppendLine($"[Block #{entry.Key}] {Explain(violation)}");
            }

            return sb.ToString();
        }

        private static string Explain(string violation) => violation switch
        {
            "Invalid MerkleRoot" => "MerkleRoot mismatch — transactions were tampered",
            "Invalid Hash" => "Hash does not match block contents — data was altered",
            "Invalid Difficulty" => "Hash does not meet difficulty — block was not re-mined",
            "Invalid PrevHash" => "PrevHash mismatch — inherited from the attacked block",
            _ => violation
        };

        public int EvictStaleTransactions(int maxAgeSeconds)
        {
            int before = PendingTransactions.Count;
            PendingTransactions.RemoveAll(t => t.Timestamp < DateTime.UtcNow.AddSeconds(-maxAgeSeconds));
            int after = PendingTransactions.Count;
            return before - after; // number of evicted transactions
        }

        public bool ValidateAndRebuildState()
        {
            Balances.Clear();
            try
            {
                foreach (var block in Chain)
                {
                    UpdateBalances(block);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during state rebuild: {ex.Message}");
                Balances.Clear();
                return false;
            }
            return true;
        }
    }
}
