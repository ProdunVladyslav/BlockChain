using System.Text.Json.Serialization;

namespace BlockChain.Model
{
    public class Block
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonPropertyName("transactions")]
        public List<Transaction> Transactions { get; set; }
        [JsonPropertyName("author")]
        public string Author { get; set; }
        [JsonPropertyName("hash")]
        public string Hash { get; set; }
        [JsonPropertyName("previousHash")]
        public string PreviousHash { get; set; }
        [JsonPropertyName("difficultyAtMining")]
        public double DifficultyAtMining { get; set; }

        [JsonPropertyName("nonce")]
        public long Nonce { get; set; } // Added Nonce property for proof-of-work

        [JsonPropertyName("minimgDurationBlock")]
        public double MiningDurationBlock { get; set; } // Added MiningDurationBlock property to track mining time in seconds

        public Block ShallowCopy() => (Block)MemberwiseClone();

        public Block(int index, DateTime timestamp, List<Transaction> transactions, string previousHash, string author, double difficultyAtMining)
        {
            Index = index;
            Timestamp = timestamp;
            Transactions = transactions;
            PreviousHash = previousHash;
            Hash = "";
            Author = author;
            DifficultyAtMining = difficultyAtMining;
        }
        public Block() { }
    }
}
