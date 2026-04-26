namespace BlockChain.HashingService
{
    public class DisplayService
    {
        public void DisplayChain(BlockChainService blockChain)
        {
            foreach (var block in blockChain.Chain)
            {
                Console.WriteLine($"Index: {block.Index}");
                Console.WriteLine($"Timestamp: {block.Timestamp}");
                Console.WriteLine($"Data: {block.Data}");
                Console.WriteLine($"Previous Hash: {block.PreviousHash}");
                Console.WriteLine($"Hash: {block.Hash}");
                Console.WriteLine($"Author: {block.Author}");
                Console.WriteLine(new string('-', 50)); // Separator for better readability
            }
        }
    }
}
