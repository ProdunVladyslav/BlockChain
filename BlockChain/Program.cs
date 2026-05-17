using BlockChain.HashingService;
using BlockChain.Model;
using BlockChain.Services;

var blockChainService = new BlockChainService();
var cryptoService = new CryptoService();

var alice = new Wallet(cryptoService);
var bob = new Wallet(cryptoService);
var charlie = new Wallet(cryptoService);
var miner1 = new Wallet(cryptoService);

Console.WriteLine($"Network Base Fee: {blockChainService.NetworkBaseFee}");

Transaction Signed(Wallet sender, string to, decimal amount, decimal fee)
{
    var tx = new Transaction(sender.PublicKey, to, amount, fee);
    TransactionService.SignTransaction(tx, sender.PrivateKey);
    return tx;
}

// ── Fund miner ────────────────────────────────────────────────────────────
for (int i = 0; i < 8; i++)
    blockChainService.MineBlock(miner1.PublicKey); // 8 × 50 = 400

blockChainService.AddTransactionToMempool(Signed(miner1, alice.PublicKey, 150m, fee: 1m));
blockChainService.AddTransactionToMempool(Signed(miner1, charlie.PublicKey, 150m, fee: 1m));
blockChainService.MineBlock(miner1.PublicKey);

Console.WriteLine($"\nAfter funding:");
Console.WriteLine($"  Alice:   {blockChainService.GetBalance(alice.PublicKey)}");
Console.WriteLine($"  Charlie: {blockChainService.GetBalance(charlie.PublicKey)}");
Console.WriteLine($"  Miner1:  {blockChainService.GetBalance(miner1.PublicKey)}");

// ── Demo 1: Fee below NetworkBaseFee rejected ─────────────────────────────
Console.WriteLine("\n=== DEMO 1: Fee below NetworkBaseFee is rejected ===");
var lowFeeTx = new Transaction(alice.PublicKey, bob.PublicKey, 10m, fee: 0.5m);
TransactionService.SignTransaction(lowFeeTx, alice.PrivateKey);
blockChainService.AddTransactionToMempool(lowFeeTx);
Console.WriteLine($"Pending after low-fee attempt: {blockChainService.PendingTransactions.Count} (expected 0)");

// ── Demo 2: Tip priority ordering ────────────────────────────────────────
Console.WriteLine("\n=== DEMO 2: Tip priority ordering ===");
blockChainService.AddTransactionToMempool(Signed(alice, bob.PublicKey, 10m, fee: 1m)); // tip = 0
blockChainService.AddTransactionToMempool(Signed(alice, bob.PublicKey, 10m, fee: 5m)); // tip = 4
blockChainService.AddTransactionToMempool(Signed(charlie, bob.PublicKey, 10m, fee: 3m)); // tip = 2

Console.WriteLine("Queued: alice fee=1 (tip=0), alice fee=5 (tip=4), charlie fee=3 (tip=2)");
Console.WriteLine($"Pending: {blockChainService.PendingTransactions.Count} (expected 3)");

decimal minerBefore = blockChainService.GetBalance(miner1.PublicKey);
blockChainService.MineBlock(miner1.PublicKey);
decimal minerAfter = blockChainService.GetBalance(miner1.PublicKey);

var tipBlock = blockChainService.Chain.Last();
var coinbase = tipBlock.Transactions.First(t => t.From == "COINBASE");
Console.WriteLine($"\nCoinbase reward: {coinbase.Amount} (expected 56 = 50 base + tips 4+2+0)");
Console.WriteLine($"Miner earned:    {minerAfter - minerBefore} (expected 56)");

Console.WriteLine("\nBlock transactions ordered by tip desc:");
foreach (var tx in tipBlock.Transactions.Where(t => t.From != "COINBASE"))
{
    decimal tip = tx.Fee - blockChainService.NetworkBaseFee;
    Console.WriteLine($"  Amount: {tx.Amount}, Fee: {tx.Fee}, Tip: {tip}, Burned: {blockChainService.NetworkBaseFee}");
}

// ── Demo 3: Burn audit ────────────────────────────────────────────────────
Console.WriteLine("\n=== DEMO 3: Burned fees audit ===");
var explorer = new BlockChainExplorer(blockChainService);

decimal burned = explorer.GetTotalBurnedFees();
decimal emitted = blockChainService.GetTotalSupply();
decimal actualSupply = explorer.GetActualTotalSupply();

int nonCoinbaseTxCount = blockChainService.Chain
    .Skip(1)
    .SelectMany(b => b.Transactions)
    .Count(t => t.From != "COINBASE");

Console.WriteLine($"Non-coinbase tx count:           {nonCoinbaseTxCount}");
Console.WriteLine($"Total emitted (COINBASE sum):    {emitted}");
Console.WriteLine($"Total burned (BaseFee × txs):   {burned} (expected {nonCoinbaseTxCount} × {blockChainService.NetworkBaseFee} = {nonCoinbaseTxCount * blockChainService.NetworkBaseFee})");
Console.WriteLine($"Actual supply in circulation:   {actualSupply} (emitted - burned)");

decimal walletSum = blockChainService.GetBalance(alice.PublicKey)
                  + blockChainService.GetBalance(bob.PublicKey)
                  + blockChainService.GetBalance(charlie.PublicKey)
                  + blockChainService.GetBalance(miner1.PublicKey);

Console.WriteLine($"\nWallet balances:");
Console.WriteLine($"  Alice:   {blockChainService.GetBalance(alice.PublicKey)}");
Console.WriteLine($"  Bob:     {blockChainService.GetBalance(bob.PublicKey)}");
Console.WriteLine($"  Charlie: {blockChainService.GetBalance(charlie.PublicKey)}");
Console.WriteLine($"  Miner1:  {blockChainService.GetBalance(miner1.PublicKey)}");
Console.WriteLine($"  Sum:     {walletSum}");

// Burned fees left wallets but exist in no wallet — correct invariant is:
// walletSum + burned == actualSupply  (i.e. all coins are either held or burned)
Console.WriteLine($"\nAccounting invariant (walletSum + burned == actualSupply):");
Console.WriteLine($"  {walletSum} + {burned} = {walletSum + burned} == {actualSupply} → {walletSum + burned == actualSupply}");

// ── Demo 4: Per-block breakdown ───────────────────────────────────────────
Console.WriteLine("\n=== DEMO 4: Per-block fee breakdown ===");
foreach (var block in blockChainService.Chain.Skip(1))
{
    var nonCoinbaseTxs = block.Transactions.Where(t => t.From != "COINBASE").ToList();
    var cb = block.Transactions.FirstOrDefault(t => t.From == "COINBASE");
    decimal totalFeePaid = nonCoinbaseTxs.Sum(t => t.Fee);
    decimal blockBurned = nonCoinbaseTxs.Count * blockChainService.NetworkBaseFee;
    decimal blockTips = nonCoinbaseTxs.Sum(t => t.Fee - blockChainService.NetworkBaseFee);
    decimal cbAmount = cb?.Amount ?? 0;
    decimal baseMiningReward = cbAmount - blockTips;
    Console.WriteLine($"  Block {block.Index,2}: txs={nonCoinbaseTxs.Count}, " +
                      $"feePaid={totalFeePaid}, burned={blockBurned}, " +
                      $"tips={blockTips}, coinbase={cbAmount} " +
                      $"(base {baseMiningReward} + tips {blockTips})");
}

// ── Demo 5: Find transaction by ID ───────────────────────────────────────
Console.WriteLine("\n=== DEMO 5: Find transaction by ID ===");
var targetTx = tipBlock.Transactions.First(t => t.From != "COINBASE");
var (foundBlock, foundTx) = explorer.FindTransactionLocation(targetTx.Id);
Console.WriteLine($"Searched for tx ID: {targetTx.Id}");
Console.WriteLine($"Found in block:     {foundBlock?.Index} (expected {tipBlock.Index})");
Console.WriteLine($"Amount: {foundTx?.Amount}, Fee: {foundTx?.Fee}, Tip: {foundTx?.Fee - blockChainService.NetworkBaseFee}");

// ── Demo 6: Address history ───────────────────────────────────────────────
Console.WriteLine("\n=== DEMO 6: Alice's transaction history ===");
var aliceHistory = explorer.GetAddressHistory(alice.PublicKey);
foreach (var tx in aliceHistory)
{
    string direction = tx.From == alice.PublicKey ? "SENT" : "RECV";
    Console.WriteLine($"  [{direction}] Amount: {tx.Amount}, Fee: {tx.Fee}, Tip: {tx.Fee - blockChainService.NetworkBaseFee}");
}
Console.WriteLine($"Total txs involving Alice: {aliceHistory.Count}");

// ── Demo 7: Largest transaction ───────────────────────────────────────────
Console.WriteLine("\n=== DEMO 7: Largest transaction ===");
var largest = explorer.GetLargestTransaction();
Console.WriteLine($"Largest tx: Amount={largest?.Amount}, Fee={largest?.Fee}");
Console.WriteLine($"  From: {(largest?.From == "COINBASE" ? "COINBASE" : largest?.From[..20] + "...")}");
Console.WriteLine($"  To:   {largest?.To[..20]}...");

Console.WriteLine("\n=== GAP DIAGNOSTIC ===");
decimal totalSentByUsers = blockChainService.Chain.Skip(1)
    .SelectMany(b => b.Transactions)
    .Where(t => t.From != "COINBASE")
    .Sum(t => t.Amount + t.Fee);

decimal totalReceivedByUsers = blockChainService.Chain.Skip(1)
    .SelectMany(b => b.Transactions)
    .Where(t => t.From != "COINBASE")
    .Sum(t => t.Amount);

decimal totalCoinbase = blockChainService.GetTotalSupply();

Console.WriteLine($"Total sent by users (amount+fee): {totalSentByUsers}");
Console.WriteLine($"Total received by users (amount): {totalReceivedByUsers}");
Console.WriteLine($"Total coinbase emitted:           {totalCoinbase}");
Console.WriteLine($"Fee paid total:                   {totalSentByUsers - totalReceivedByUsers}");
Console.WriteLine($"Tips to miners:                   {totalSentByUsers - totalReceivedByUsers - burned}");
Console.WriteLine($"Expected wallet sum:              {totalCoinbase - burned}");
Console.WriteLine($"Actual wallet sum:                {walletSum}");
Console.WriteLine($"Difference:                       {totalCoinbase - burned - walletSum}");