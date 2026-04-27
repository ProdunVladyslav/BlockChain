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
                Console.WriteLine($"Previous Hash: {block.PreviousHash}");
                Console.WriteLine($"Hash: {block.Hash}");
                Console.WriteLine($"Author: {block.Author}");
                var transactions = block.Transactions;
                foreach ( var transaction in transactions)
                {
                    Console.WriteLine($"  Transaction: {transaction.ToString()}");
                }
                Console.WriteLine(new string('-', 50)); // Separator for better readability
            }
        }
    }
}
