namespace BlockChain.Model
{
    public class Block
    {
        public int Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string Data { get; set; }
        public string Author { get; set; }
        public string Hash { get; set; }
        public string PreviousHash { get; set; }
        public double DifficultyAtMining { get; set; }

        public long Nonce { get; set; } // Added Nonce property for proof-of-work

        public double MiningDurationBlock { get; set; } // Added MiningDurationBlock property to track mining time in seconds

        public Block ShallowCopy() => (Block)MemberwiseClone();

        public Block(int index, DateTime timestamp, string data, string previousHash, string author, double difficultyAtMining)
        {
            Index = index;
            Timestamp = timestamp;
            Data = data;
            PreviousHash = previousHash;
            Hash = "";
            Author = author;
            DifficultyAtMining = difficultyAtMining;
        }
        public Block() { }
    }
}
