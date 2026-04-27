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
            var dataString = string.Concat(block.Transactions.Select(t => t.ToRawString())); // Convert the list of transactions to a single string representation
            string rawData = $"{block.Index}{block.Timestamp}{dataString}{block.PreviousHash}{block.Author}{block.Nonce}"; // Concatenate block properties to create a string representation of the block's data
            return ComputeHash(rawData); // Compute the hash of the raw data string
        }

        // Overloaded method to compute hash from a raw data string
        public static string ComputeHash(string rawData) 
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(rawData); // Convert the input string to a byte array using UTF-8 encoding
            byte[] bytes = SHA256.HashData(inputBytes); // Compute the SHA-256 hash of the input byte array and return the resulting hash as a byte array

            return Convert.ToHexString(bytes).ToLower(); // Convert the hash byte array to a Base64 string and return it as the final hash value
        }
    }
}
