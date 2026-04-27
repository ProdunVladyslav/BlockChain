using BlockChain.HashingService;
using BlockChain.Model;
using BlockChain.Services;

var displayService = new DisplayService();
var blockChainService = new BlockChainService();

// Mine a few blocks

blockChainService.AddBlock(new List<Transaction>
{
    new("Alice", "Bob",     50.00m),
    new("Bob",   "Charlie", 25.00m),
}, "Miner1");

blockChainService.AddBlock(new List<Transaction>
{
    new("Charlie", "Dave",  10.00m),
    new("Dave",    "Alice",  5.00m),
    new("Alice",   "Eve",    3.50m),
}, "Miner2");

blockChainService.AddBlock(new List<Transaction>
{
    new("Eve",   "Frank", 100.00m),
    new("Frank", "Alice",  20.00m),
}, "Miner1");

// Display the chain

displayService.DisplayChain(blockChainService);

// Validate + build explorer

var isValid = blockChainService.IsChainValid();
Console.WriteLine($"\nChain valid: {isValid}");

var explorer = new BlockChainExplorer(blockChainService);

// Total volume

var totalVolume = explorer.GetTotalTransactionVolume();
Console.WriteLine($"\nTotal transaction volume: {totalVolume:C}");

// Largest transaction

var largest = explorer.GetLargestTransaction();
Console.WriteLine($"Largest transaction: {largest?.From} to {largest?.To}: {largest?.Amount:C}");

// Address history

var aliceHistory = explorer.GetAddressHistory("Alice");
Console.WriteLine($"\nAlice's transactions ({aliceHistory.Count}):");
foreach (var tx in aliceHistory)
    Console.WriteLine($"  {tx.From} to {tx.To}: {tx.Amount:C}");

// Find transaction by ID

var targetTxId = blockChainService.Chain[1].Transactions[0].Id;
var (foundBlock, foundTx) = explorer.FindTransactionLocation(targetTxId);
Console.WriteLine($"\nLookup TX {targetTxId}:");
Console.WriteLine(foundBlock is not null
    ? $"  Found in block {foundBlock.Index} - {foundTx!.From} to {foundTx.To}: {foundTx.Amount:C}"
    : "  Not found.");

// Tamper test - explorer should reject the mutated chain

Console.WriteLine("\n-- Tampering with block 1 --");
blockChainService.Chain[1].Transactions[0] = new Transaction("Alice", "Bob", 999.00m);

isValid = blockChainService.IsChainValid();
Console.WriteLine($"Chain valid after tamper: {isValid}");

try
{
    var explorerOnTamperedChain = new BlockChainExplorer(blockChainService);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Explorer rejected tampered chain: {ex.Message}");
}
//long i = 0;

//while (i < 100_000)
//{
//    blockChainService.AddBlock("First Block", "Alex");
//    if(i % 10 == 0)
//    {
//        Console.WriteLine($"Difficulty: {blockChainService.Difficulty}");
//        Console.WriteLine($"Block hash: {blockChainService.Chain.Last().Hash}");
//        Console.WriteLine($"Time taken to mine block: {blockChainService.Chain.Last().MiningDurationBlock} seconds");
//        blockChainService.PrintDifficultyHistory();
//    }
//    i++;
//}
