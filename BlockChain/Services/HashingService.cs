using BlockChain.Model;
using System.Security.Cryptography;
using System.Text;

namespace BlockChain.Chain
{
    public class HashingService
    {
        // Method to compute the hash of a block
        public static string ComputeHash(Block block)
        {
            string rawData = $"{block.Index}{block.Timestamp}{block.MerkleRoot}{block.PreviousHash}{block.Author}{block.Nonce}"; // Concatenate block properties to create a string representation of the block's data
            return ComputeHash(rawData); // Compute the hash of the raw data string
        }

        // Overloaded method to compute hash from a raw data string
        public static string ComputeHash(string rawData)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(rawData); // Convert the input string to a byte array using UTF-8 encoding
            byte[] bytes = SHA256.HashData(inputBytes); // Compute the SHA-256 hash of the input byte array and return the resulting hash as a byte array

            return Convert.ToHexString(bytes).ToLower(); // Convert the hash byte array to a Base64 string and return it as the final hash value
        }

        public static string BuildMerkleRoot(List<Transaction> transactions)
        {
            if (transactions == null || transactions.Count == 0)
                return string.Empty;

            var hashAllTransactions = transactions.Select(t => ComputeHash(t.ToRawString())).ToList(); // Compute the hash of each transaction and store them in a list
            while (hashAllTransactions.Count > 1)
            {
                var tempList = new List<string>();
                for (int i = 0; i < hashAllTransactions.Count; i += 2)
                {
                    if (i + 1 < hashAllTransactions.Count)
                    {
                        // If there is a pair of hashes, concatenate them and compute the hash of the concatenated string
                        tempList.Add(ComputeHash(hashAllTransactions[i] + hashAllTransactions[i + 1]));
                    }
                    else
                    {
                        // If there is an odd number of hashes, duplicate the last hash and compute the hash of the concatenated string
                        tempList.Add(hashAllTransactions[i]);
                    }
                }
                hashAllTransactions = tempList; // Update the list of hashes with the newly computed hashes
            }
            return hashAllTransactions[0]; // Return the final hash, which is the Merkle root of the transaction
        }

        /// <summary>
        /// Builds a Merkle proof that a specific transaction is contained in a block.
        /// Returns null if the transaction is not found.
        /// </summary>
        public static MerkleProof BuildMerkleProof(Block block, Guid transactionId)
        {
            var txIndex = block.Transactions.FindIndex(t => t.Id == transactionId);
            if (txIndex == -1) return null;

            var tx = block.Transactions[txIndex];
            var txHash = ComputeHash(tx.ToRawString());

            // Build all levels of the Merkle tree
            var levels = new List<List<string>>();
            var currentLevel = block.Transactions.Select(t => ComputeHash(t.ToRawString())).ToList();
            levels.Add(currentLevel.ToList());

            while (currentLevel.Count > 1)
            {
                var nextLevel = new List<string>();
                for (int i = 0; i < currentLevel.Count; i += 2)
                {
                    if (i + 1 < currentLevel.Count)
                        nextLevel.Add(ComputeHash(currentLevel[i] + currentLevel[i + 1]));
                    else
                        nextLevel.Add(currentLevel[i]); // carry forward odd element
                }
                levels.Add(nextLevel.ToList());
                currentLevel = nextLevel;
            }

            // Walk from leaf up, collecting sibling hashes
            var steps = new List<ProofStep>();
            int idx = txIndex;

            for (int level = 0; level < levels.Count - 1; level++)
            {
                var sibling = TryGetSibling(levels[level], idx);
                if (sibling != null)
                {
                    steps.Add(new ProofStep
                    {
                        SiblingHash = sibling,
                        // If idx is odd → sibling is the left child → concatenate sibling before our hash
                        // If idx is even → sibling is the right child → concatenate our hash before sibling
                        IsLeft = idx % 2 == 1
                    });
                }
                idx /= 2; // move to parent index at next level
            }

            return new MerkleProof
            {
                TransactionId = transactionId,
                TransactionHash = txHash,
                MerkleRoot = levels[^1][0],
                BlockIndex = block.Index,
                BlockHash = block.Hash,
                Steps = steps
            };
        }

        /// <summary>
        /// Verifies a Merkle proof against a known transaction.
        /// </summary>
        public static bool VerifyMerkleProof(MerkleProof proof, Transaction transaction)
        {
            var txHash = ComputeHash(transaction.ToRawString());
            if (txHash != proof.TransactionHash) return false;

            var currentHash = txHash;
            foreach (var step in proof.Steps)
            {
                currentHash = step.IsLeft
                    ? ComputeHash(step.SiblingHash + currentHash)
                    : ComputeHash(currentHash + step.SiblingHash);
            }

            return currentHash == proof.MerkleRoot;
        }

        private static string TryGetSibling(List<string> level, int idx)
        {
            if (idx % 2 == 0)
                return idx + 1 < level.Count ? level[idx + 1] : null;
            else
                return level[idx - 1];
        }
    }
}
