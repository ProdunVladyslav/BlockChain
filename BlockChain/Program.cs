using BlockChain.HashingService;
using BlockChain.Model;
using BlockChain.Services;
using System.Numerics;

var blockChainService = new BlockChainService();
var cryptoService = new CryptoService();
var displayService = new DisplayService();

var alice = new Wallet(cryptoService);
var bob = new Wallet(cryptoService);
var charlie = new Wallet(cryptoService);
var miner1 = new Wallet(cryptoService);
var miner2 = new Wallet(cryptoService);

// Helper — creates and signs a transaction in one call
Transaction Signed(Wallet sender, string to, decimal amount)
{
    var tx = new Transaction(sender.PublicKey, to, amount);
    TransactionService.SignTransaction(tx, sender.PrivateKey);
    return tx;
}

blockChainService.AddBlock(new List<Transaction>(), miner1.PublicKey);
blockChainService.AddBlock(new List<Transaction>(), miner2.PublicKey);
blockChainService.AddBlock(new List<Transaction>(), miner1.PublicKey);
blockChainService.AddBlock(new List<Transaction>(), miner2.PublicKey);

blockChainService.AddBlock(new List<Transaction>
{
    Signed(miner1, alice.PublicKey,   100m),
    Signed(miner2, bob.PublicKey,     100m),
}, miner1.PublicKey);

blockChainService.AddBlock(new List<Transaction>
{
    Signed(miner1, charlie.PublicKey, 100m),
}, miner2.PublicKey);

blockChainService.AddBlock(new List<Transaction> { Signed(alice, bob.PublicKey, 10.00m) }, miner1.PublicKey);

displayService.DisplayChain(blockChainService);

// ── Phase 1: mine reward blocks ──────────────────────────────────────────
blockChainService.AddBlock(new List<Transaction>(), miner1.PublicKey);
blockChainService.AddBlock(new List<Transaction>(), miner2.PublicKey);
blockChainService.AddBlock(new List<Transaction>(), miner1.PublicKey);
blockChainService.AddBlock(new List<Transaction>(), miner2.PublicKey);

// ── Phase 2: seed users ──────────────────────────────────────────────────
blockChainService.AddBlock(new List<Transaction>
{
    Signed(miner1, alice.PublicKey,  100m),
    Signed(miner2, bob.PublicKey,    100m),
}, miner1.PublicKey);

blockChainService.AddBlock(new List<Transaction>
{
    Signed(miner1, charlie.PublicKey, 100m),
}, miner2.PublicKey);

// ── Phase 3: randomised blocks up to 10 000 total ───────────────────────
var rng = new Random(42);
var wallets = new[] { alice, bob, charlie, miner1, miner2 };
var miners = new[] { miner1, miner2 };
const int TargetBlocks = 100;
const int MaxTxPerBlock = 5;
const decimal MinSend = 1m;

while (blockChainService.Chain.Count < TargetBlocks)
{
    var txList = new List<Transaction>();
    int txCount = rng.Next(1, MaxTxPerBlock + 1);

    for (int i = 0; i < txCount; i++)
    {
        // pick a random sender that actually has funds
        var candidates = wallets
            .Where(w => blockChainService.GetBalance(w.PublicKey) > MinSend)
            .ToList();

        if (candidates.Count == 0) break;   // no one can send — mine empty

        var sender = candidates[rng.Next(candidates.Count)];
        var balance = blockChainService.GetBalance(sender.PublicKey);

        // pick a different random recipient
        var recipient = wallets
            .Where(w => w.PublicKey != sender.PublicKey)
            .ElementAt(rng.Next(wallets.Length - 1));

        // send between MinSend and half the sender's balance (keep it sane)
        decimal maxSend = Math.Floor(balance / 2m);
        if (maxSend < MinSend) continue;

        decimal amount = Math.Round(
             MinSend + (maxSend - MinSend) * (decimal)rng.NextDouble(), 2);

        txList.Add(Signed(sender, recipient.PublicKey, amount));
    }

    var miner = miners[rng.Next(miners.Length)];
    blockChainService.AddBlock(txList, miner.PublicKey);
}

var stopWatch = System.Diagnostics.Stopwatch.StartNew();
var aliceBalance = blockChainService.UnoptimisedGetBalance(alice.PublicKey);
stopWatch.Stop();
Console.WriteLine($"Alice's balance (unoptimised): {aliceBalance}, Time taken: {stopWatch.ElapsedMilliseconds} ms");

stopWatch.Restart();
var bobBalance = blockChainService.GetBalance(alice.PublicKey);
stopWatch.Stop();
Console.WriteLine($"Alice's balance (optimised): {bobBalance}, Time taken: {stopWatch.ElapsedMilliseconds} ms");

blockChainService.SaveToFile("C:\\Users\\produ\\Downloads\\chain.txt");

Console.WriteLine("Saved to file.");

var restoredChain = new BlockChainService();
restoredChain.LoadFromFile("C:\\Users\\produ\\Downloads\\chain.txt");

Console.WriteLine("Restored from file.");

aliceBalance = restoredChain.GetBalance(alice.PublicKey);
Console.WriteLine($"Alice's balance (restored): {aliceBalance}");