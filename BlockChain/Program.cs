using BlockChain.HashingService;

var dislayService = new DisplayService();
var blockChainService = new BlockChainService();

long i = 0;

while (i < 100)
{
    blockChainService.AddBlock("First Block", "Alex");
    if(i % 10 == 0)
    {
        Console.WriteLine($"Difficulty: {blockChainService.Difficulty}");
        Console.WriteLine($"Block hash: {blockChainService.Chain.Last().Hash}");
        Console.WriteLine($"Time taken to mine block: {blockChainService.Chain.Last().MiningDurationBlock} seconds");
        blockChainService.PrintDifficultyHistory();
    }
    i++;
}



//Console.ForegroundColor = ConsoleColor.Cyan;

//dislayService.DisplayChain(blockChainService);

//if (blockChainService.IsChainValid())
//{
//    Console.ForegroundColor = ConsoleColor.Green;
//    Console.WriteLine("Blockchain is valid.");
//}
//else
//{
//    Console.ForegroundColor = ConsoleColor.Red;
//    Console.WriteLine("Blockchain is invalid.");
//}

//blockChainService.Chain[1].Data = "Tampered Data"; // Tampering with the blockchain
//Console.ForegroundColor = ConsoleColor.Cyan;

//dislayService.DisplayChain(blockChainService);

//if (blockChainService.IsChainValid())
//{
//    Console.ForegroundColor = ConsoleColor.Green;
//    Console.WriteLine("Blockchain is valid.");
//}
//else
//{
//    Console.ForegroundColor = ConsoleColor.Red;
//    Console.WriteLine("Blockchain is invalid.");
//}

//int numOfDifificulties = 4; // Number of difficulty levels to test

//for (int difficulty = 1; difficulty <= numOfDifificulties; difficulty++)
//{
//    var sw = System.Diagnostics.Stopwatch.StartNew(); // Start a stopwatch to measure the time taken for mining
//    blockChainService.AddBlock("Test Block 1", "Tester"); // Add a test block to the blockchain
//    sw.Stop(); // Stop the stopwatch after mining is complete
//    Console.WriteLine($"Difficulty: {difficulty}, Time taken: {sw.ElapsedMilliseconds} ms, Nonce: {blockChainService.Chain.TakeLast(1).FirstOrDefault()?.Nonce}"); // Output the difficulty level and the time taken for mining
//    blockChainService.IncreaseDifficulty(); // Increase the difficulty level for the next iteration
//}

//var sw = System.Diagnostics.Stopwatch.StartNew();
//BlockChainService bcs = new BlockChainService();
//bcs.AddBlock(str)
//sw.Stop();
//Console.WriteLine($"Word: {word}, Time taken: {sw.ElapsedMilliseconds} ms, Nonce: {bcs.Chain.TakeLast(1).FirstOrDefault()?.Nonce}, Hash: {bcs.Chain.TakeLast(1).FirstOrDefault()?.Hash}");

