using BlockChain.Model;
using System.Security.Cryptography;
using System.Text;

namespace BlockChain.HashingService
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
    }
}
