using BlockChain.Chain;
using BlockChain.Model;
using BlockChain.Services;
using BlockChain.Services.P2P;
using BlockChain.Services.P2P.Handlers;
using Microsoft.Extensions.DependencyInjection;

var service = new ServiceCollection();
service.AddSingleton<BlockChainService>();
service.AddSingleton<CryptoService>();
service.AddSingleton<P2PClient>();
service.AddSingleton<DisplayService>();
service.AddSingleton<P2PServer>();
service.AddSingleton<HashingService>();
service.AddSingleton<MiningService>();
service.AddSingleton<StorageService>();

var provider = service.BuildServiceProvider();

var blockChainService = provider.GetRequiredService<BlockChainService>();
var p2pServer = provider.GetRequiredService<P2PServer>();
var p2pClient = provider.GetRequiredService<P2PClient>();
var cryptoService = provider.GetRequiredService<CryptoService>();
var displayService = provider.GetRequiredService<DisplayService>();

var hello = new HelloHandler(p2pClient);
var newTx = new NewTransactionHandler(blockChainService, p2pClient);
var requestChain = new RequestChainHandler(p2pClient, blockChainService);
var newChain = new NewChainHandler(blockChainService, p2pClient, provider.GetRequiredService<StorageService>());
var newBlock = new NewBlockHandler(blockChainService, p2pClient);
var unknown = new UnknownMessageHandler();

hello.SetNext(newTx)
     .SetNext(requestChain)
     .SetNext(newChain)
     .SetNext(newBlock)
     .SetNext(unknown);

p2pServer.ChainHead = hello;

var myWallet = new Wallet(cryptoService);

// Auto-broadcast chain whenever a block is mined locally
blockChainService.BlockMined += block =>
{
    _ = p2pClient.BroadcastBlockAsync(block);
    Console.WriteLine($"[P2P] Auto-broadcast block after mining #{block.Index}");
};

// TASK 1

// Mine few bloacks to get value
//blockChainService.MineBlock(myWallet.PublicKey);
//blockChainService.MineBlock(myWallet.PublicKey);
//blockChainService.MineBlock(myWallet.PublicKey);

//var myBalance = blockChainService.GetBalance(myWallet.PublicKey);

//Console.WriteLine($"Your wallet balance after mining few blocks: {myBalance}");



//BlockChainService newChain = blockChainService.Clone();

//Console.WriteLine("Initial chain:");
//displayService.DisplayChain(blockChainService);

//newChain.MineBlock("HACKER");
//newChain.MineBlock("HACKER");

//Console.WriteLine("HACKER CHAIN:");
//displayService.DisplayChain(newChain);

//blockChainService.MineBlock(myWallet.PublicKey);

//Console.WriteLine("USER CHAIN:");
//displayService.DisplayChain(blockChainService);

//Console.WriteLine("Replacing chain with HACKER chain...");
//blockChainService.ReplaceChain(newChain.Chain);

//Console.WriteLine("USER CHAIN after replacement:");
//displayService.DisplayChain(blockChainService);


Console.WriteLine($"Your wallet address (public key): {myWallet.PublicKey}");
Console.WriteLine("Enter port of P2P server:");

if (!int.TryParse(Console.ReadLine(), out int port))
    port = 5001;

p2pServer.Start(port);

bool flag = true;

while (flag)
{
    Console.WriteLine("\nMain menu:");
    Console.WriteLine("1 - Connect to another node");
    Console.WriteLine("2 - Create and broadcast a transaction");
    Console.WriteLine("3 - Show mem-pool");
    Console.WriteLine("4 - Mine block");
    Console.WriteLine("5 - See block chain");
    Console.WriteLine("6 - Balance");
    Console.WriteLine("7 - Save chain to file");
    Console.WriteLine("8 - Load chain from file");
    Console.WriteLine("9 - Exit");
    Console.WriteLine("0 - Run Fork Auditor simulation");
    Console.WriteLine("a - Simulate Hacker Attack");
    Console.WriteLine("s - Request chain from peer");
    Console.WriteLine("t - Run forensic audit test (Task 1 final test)");
    Console.WriteLine("h - Homework tests");

    Console.Write("Enter your choice: ");

    switch (Console.ReadLine())
    {
        case "1":

            Console.WriteLine("Enter the address of the peer to connect to (e.g., 127.0.0.1:5001):");
            var nodeAddress = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(nodeAddress) || !nodeAddress.Contains(':'))
            {
                Console.WriteLine("Invalid address format. Please use IP:Port format.");
                break;
            }
            await p2pClient.ConnectAsync(nodeAddress);
            break;

        case "2":
            Console.Write("Enter an address of receiver: ");
            var toAddress = Console.ReadLine();

            Console.Write("Enter the amount to send: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                Console.WriteLine("Invalid amount.");
                break;
            }

            Console.Write("Enter the fee: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal fee))
            {
                Console.WriteLine("Invalid fee.");
                break;
            }

            try
            {
                var transaction = TransactionService.CreateTransaction(myWallet.PublicKey, toAddress, amount, fee);
                TransactionService.SignTransaction(transaction, myWallet.PrivateKey);
                blockChainService.AddTransactionToMempool(transaction);
                await p2pClient.BroadcastTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating transaction: {ex.Message}");
            }
            break;

        case "3":
            if (blockChainService.PendingTransactions.Count == 0)
            {
                Console.WriteLine("Mem-pool is empty.");
                break;
            }
            Console.WriteLine($"Mem-pool ({blockChainService.PendingTransactions.Count} transactions):");
            foreach (var tx in blockChainService.PendingTransactions)
                Console.WriteLine($"  {tx.From[..16]}... -> {tx.To[..16]}..., amount={tx.Amount}, fee={tx.Fee}");
            break;


        case "4":
            blockChainService.MineBlock(myWallet.PublicKey);
            break;

        case "5":
            displayService.DisplayChain(blockChainService);
            break;

        case "6":
            var balance = blockChainService.GetBalance(myWallet.PublicKey);
            Console.WriteLine($"Your balance: {balance}");
            break;

        case "9":
            Console.WriteLine("Exiting...");
            flag = false;
            break;

        case "7":
            Console.Write("Enter file path to save (e.g. chain.json): ");
            var savePath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(savePath))
            {
                Console.WriteLine("Invalid path.");
                break;
            }
            try
            {
                blockChainService.SaveToFile(savePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving chain: {ex.Message}");
            }
            break;

        case "8":
            Console.Write("Enter file path to load (e.g. chain.json): ");
            var loadPath = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(loadPath))
            {
                Console.WriteLine("Invalid path.");
                break;
            }
            try
            {
                blockChainService.LoadFromFile(loadPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading chain: {ex.Message}");
            }
            break;

        case "0":
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  FORK AUDITOR — Симуляція мережевого розколу");
            Console.WriteLine("=================================================");

            // ── Phase 1: Build shared history ────────────────────────────────
            Console.WriteLine("\n[Phase 1] Building shared history on both chains...");
            blockChainService.MineBlock(myWallet.PublicKey); // ensure user has funds

            Console.WriteLine($"Chain length:   {blockChainService.Chain.Count}");
            Console.WriteLine($"Your balance:   {blockChainService.GetBalance(myWallet.PublicKey)}");

            // ── Phase 2: Clone at fork point ─────────────────────────────────
            // fakeNode and blockChainService now share IDENTICAL history
            Console.WriteLine("\n[Phase 2] Cloning chain — this is the fork moment...");
            var fakeNode9 = blockChainService.Clone();
            Console.WriteLine($"Fork point: both chains share {blockChainService.Chain.Count} blocks.");
            Console.WriteLine($"Genesis match: {fakeNode9.Chain[0].Hash == blockChainService.Chain[0].Hash}");

            // ── Phase 3: User side — create real transactions, mine them ─────
            // These transactions will end up ONLY in the user's fork.
            // After the reorg they will be erased → triggers Task 3.
            Console.WriteLine("\n[Phase 3] User side: injecting real transactions into our fork...");

            var aliceAddress = "Alice_ReceiverWallet";
            var bobAddress = "Bob_ReceiverWallet";

            var tx9a = TransactionService.CreateTransaction(myWallet.PublicKey, aliceAddress, 20m, 2m);
            TransactionService.SignTransaction(tx9a, myWallet.PrivateKey);
            blockChainService.AddTransactionToMempool(tx9a);

            var tx9b = TransactionService.CreateTransaction(myWallet.PublicKey, bobAddress, 15m, 2m);
            TransactionService.SignTransaction(tx9b, myWallet.PrivateKey);
            blockChainService.AddTransactionToMempool(tx9b);

            Console.WriteLine($"  tx1 → Alice: 20 coins  (id: {tx9a.Id})");
            Console.WriteLine($"  tx2 → Bob:   15 coins  (id: {tx9b.Id})");

            // Mine a block that includes both transactions
            blockChainService.MineBlock(myWallet.PublicKey);
            Console.WriteLine($"Block mined. Alice balance: {blockChainService.GetBalance(aliceAddress)}, " +
                              $"Bob balance: {blockChainService.GetBalance(bobAddress)}");

            // Mine one more user block for a deeper reorg
            blockChainService.MineBlock(myWallet.PublicKey);
            Console.WriteLine($"User chain length: {blockChainService.Chain.Count}");

            // ── Phase 4: Hacker side — mine more blocks, ignore our txs ──────
            // fakeNode has NO knowledge of tx9a or tx9b — they live only in our fork.
            Console.WriteLine("\n[Phase 4] Hacker side: mining competing chain without our transactions...");
            var hackerAddr = "HackerWallet";

            // Beat user chain in BOTH length and cumulative difficulty
            while (fakeNode9.Chain.Count <= blockChainService.Chain.Count ||
                   fakeNode9.Chain.Sum(b => b.DifficultyAtMining)
                       <= blockChainService.Chain.Sum(b => b.DifficultyAtMining))
            {
                fakeNode9.MineBlock(hackerAddr);
            }
            fakeNode9.MineBlock(hackerAddr); // one extra for good measure

            Console.WriteLine($"Hacker chain length:      {fakeNode9.Chain.Count}  " +
                              $"(diff: {fakeNode9.Chain.Sum(b => b.DifficultyAtMining):F2})");
            Console.WriteLine($"Our chain length:         {blockChainService.Chain.Count}  " +
                              $"(diff: {blockChainService.Chain.Sum(b => b.DifficultyAtMining):F2})");

            // ── Snapshot balances for our own comparison printout ────────────
            decimal snap_user = blockChainService.GetBalance(myWallet.PublicKey);
            decimal snap_alice = blockChainService.GetBalance(aliceAddress);
            decimal snap_bob = blockChainService.GetBalance(bobAddress);

            // ── Display chains before the swap ───────────────────────────────
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  YOUR CHAIN — before ReplaceChain");
            Console.WriteLine("=================================================");
            displayService.DisplayChain(blockChainService);

            Console.WriteLine("\n=================================================");
            Console.WriteLine("  HACKER CHAIN — incoming from network");
            Console.WriteLine("=================================================");
            displayService.DisplayChain(fakeNode9);

            // ── Phase 5: Trigger consensus — watch the auditor messages fire ─
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  >>> CALLING ReplaceChain — auditor output below <<<");
            Console.WriteLine("=================================================\n");

            blockChainService.ReplaceChain(fakeNode9.Chain);

            // ── Display chain after the swap ─────────────────────────────────
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  YOUR CHAIN — after ReplaceChain");
            Console.WriteLine("=================================================");
            displayService.DisplayChain(blockChainService);

            // ── Summary ───────────────────────────────────────────────────────
            bool swapped9 = blockChainService.Chain.Last().Author == hackerAddr;
            if (swapped9)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ Reorg completed. Summary of damage:");
                Console.ResetColor();

                Console.WriteLine($"  Your wallet:  {snap_user}  → {blockChainService.GetBalance(myWallet.PublicKey)}");
                Console.WriteLine($"  Alice:        {snap_alice} → {blockChainService.GetBalance(aliceAddress)}  (payment erased)");
                Console.WriteLine($"  Bob:          {snap_bob}   → {blockChainService.GetBalance(bobAddress)}  (payment erased)");
                Console.WriteLine($"  HackerWallet: {blockChainService.GetBalance(hackerAddr)} (rewarded for longer chain)");

                Console.WriteLine("\n  Verify with UnoptimisedGetBalance (must match cached):");
                Console.WriteLine($"  Your wallet (recomputed): {blockChainService.UnoptimisedGetBalance(myWallet.PublicKey)}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n✗ Chain was NOT replaced — hacker chain didn't beat ours.");
                Console.ResetColor();
            }
            break;

        case "s":
            Console.WriteLine("Enter the address of the peer to request chain from (e.g., 127.0.0.1:5001):");
            var chainNodeAddress = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(chainNodeAddress) || !chainNodeAddress.Contains(':'))
            {
                Console.WriteLine("Invalid address format. Please use IP:Port format.");
                break;
            }
            var parts = chainNodeAddress.Split(':');
            await p2pClient.RequestChainAsync(parts[0], int.Parse(parts[1]));
            Console.WriteLine("Chain request sent.");
            break;

        case "a":
            Console.WriteLine("HACKER ATTACK SIMULATION");
            var lastBlock = blockChainService.Chain.Last();
            var firstTx = lastBlock.Transactions.FirstOrDefault(t => t.From != "COINBASE");
            firstTx.Amount = 1_000_000m; // inflate the amount to a huge value
            lastBlock.Nonce = 0; // reset nonce to force re-mining

            var miningService = provider.GetRequiredService<MiningService>();
            var hashingService = provider.GetRequiredService<HashingService>();

            // Re-mine the block with the modified transaction
            MiningService.MineBlockMultiThreaded(lastBlock, blockChainService.Difficulty);

            blockChainService.SaveToFile("chain.json");
            break;

        case "t":
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  TASK 1 FINAL TEST — Forensic Audit Demo");
            Console.WriteLine("=================================================");

            // ── Phase 1: build a fresh isolated chain with 6 blocks ──────────
            Console.WriteLine("\n[Phase 1] Mining 6 blocks on a local test chain...");
            var testService = blockChainService.Clone(); // isolated copy, doesn't touch real chain
            var testKey = myWallet.PublicKey;

            // give the miner a real COINBASE balance first, then add user txs
            testService.MineBlock(testKey); // block 1
            testService.MineBlock(testKey); // block 2
            testService.MineBlock(testKey); // block 3 ← attack target
            testService.MineBlock(testKey); // block 4
            testService.MineBlock(testKey); // block 5
            testService.MineBlock(testKey); // block 6

            Console.WriteLine($"  Chain length: {testService.Chain.Count} blocks (genesis + 6)");
            for (int i = 0; i < testService.Chain.Count; i++)
            {
                var b = testService.Chain[i];
                Console.WriteLine($"  Block #{b.Index}  hash={b.Hash[..16]}...  txs={b.Transactions.Count}");
            }

            // ── Phase 2: tamper block #3 directly (no re-mining) ─────────────
            Console.WriteLine("\n[Phase 2] Tampering block #3 — inflating a COINBASE reward directly...");
            var attackBlock = testService.Chain.FirstOrDefault(b => b.Index == 3);
            if (attackBlock == null)
            {
                Console.WriteLine("  ERROR: block #3 not found. Aborting.");
                break;
            }

            var victimTx = attackBlock.Transactions.FirstOrDefault(t => t.From == "COINBASE");
            if (victimTx == null)
            {
                Console.WriteLine("  ERROR: no COINBASE transaction in block #3. Aborting.");
                break;
            }

            decimal originalAmount = victimTx.Amount;
            victimTx.Amount = 999_999m; // inflate without re-mining — MerkleRoot will mismatch
            Console.WriteLine($"  Block #3 COINBASE tx: {originalAmount} → {victimTx.Amount}");
            Console.WriteLine($"  Hash unchanged (attacker forgot to re-mine): {attackBlock.Hash[..16]}...");

            // ── Phase 3: RunFullAudit ─────────────────────────────────────────
            Console.WriteLine("\n[Phase 3] Running RunFullAudit...");
            var auditReport = blockChainService.RunFullAudit(testService.Chain);

            Console.WriteLine($"  IsChainValid        : {auditReport.IsChainValid}");
            Console.WriteLine($"  Compromised blocks  : [{string.Join(", ", auditReport.CompromisedBlockIndexes.Select(i => $"#{i}"))}]");
            foreach (var kv in auditReport.ViolationDetails.OrderBy(x => x.Key))
                foreach (var v in kv.Value)
                    Console.WriteLine($"    [Block #{kv.Key}] {v}");

            // ── Phase 4: FindAttackOrigin ─────────────────────────────────────
            Console.WriteLine("\n[Phase 4] Running FindAttackOrigin...");
            var origin = blockChainService.FindAttackOrigin(auditReport, testService.Chain);
            if (origin != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  >>> Attack origin identified: Block #{origin.Index} <<<");
                Console.ResetColor();
                Console.WriteLine($"  Timestamp : {origin.Timestamp:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Hash      : {origin.Hash[..16]}...");
                Console.WriteLine($"  Txs       : {origin.Transactions.Count}");
                Console.WriteLine($"  Correct?  : {(origin.Index == 3 ? "✅ YES — block #3 correctly identified!" : "❌ NO — wrong block identified!")}");
            }
            else
            {
                Console.WriteLine("  FindAttackOrigin returned null — no non-PrevHash violation found.");
            }

            // ── Phase 5: GenerateForensicReport ──────────────────────────────
            Console.WriteLine("\n[Phase 5] Running GenerateForensicReport...");
            var forensicText = blockChainService.GenerateForensicReport(auditReport, origin);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(forensicText);
            Console.ResetColor();

            // ── Summary ───────────────────────────────────────────────────────
            bool testPassed = origin?.Index == 3 && !auditReport.IsChainValid;
            Console.ForegroundColor = testPassed ? ConsoleColor.Green : ConsoleColor.Yellow;
            Console.WriteLine(testPassed
                ? "=== TEST PASSED: system correctly detected and located the 51% attack ==="
                : "=== TEST FAILED: check the audit logic above ===");
            Console.ResetColor();
            break;

        case "h":
            bool hwFlag = true;
            while (hwFlag)
            {
                Console.WriteLine("\nHomework menu:");
                Console.WriteLine("1 - Стейт, TTL та Антиспам");
                Console.WriteLine("9 - Back to main menu");
                Console.Write("Pick HW: ");
                switch (Console.ReadLine())
                {
                    case "1":
                        Console.WriteLine("\nRunning HW3: State, TTL, Anti-Spam");

                        // --- Scenario 1: ValidateAndRebuildState ---
                        var svc1 = blockChainService.Clone();
                        svc1.MineBlock(myWallet.PublicKey);
                        svc1.MineBlock(myWallet.PublicKey);

                        var txRebuild = TransactionService.CreateTransaction(myWallet.PublicKey, "Alice", 10m, 1m);
                        TransactionService.SignTransaction(txRebuild, myWallet.PrivateKey);
                        svc1.AddTransactionToMempool(txRebuild);
                        svc1.MineBlock(myWallet.PublicKey);

                        Console.WriteLine($"Balance before failure: {svc1.GetBalance(myWallet.PublicKey)}");
                        svc1.ImitateFailure();
                        Console.WriteLine($"Balance after failure: {svc1.GetBalance(myWallet.PublicKey)}");

                        bool rebuilt = svc1.ValidateAndRebuildState();
                        Console.WriteLine($"ValidateAndRebuildState returned: {rebuilt}");
                        Console.WriteLine($"Balance after rebuild: {svc1.GetBalance(myWallet.PublicKey)}");

                        // --- Scenario 2: EvictStaleTransactions ---
                        var svc2 = blockChainService.Clone();
                        var oldTx1 = new Transaction("Sender1", "Receiver", 1m, 0.5m) { Timestamp = DateTime.UtcNow.AddSeconds(-120) };
                        var oldTx2 = new Transaction("Sender2", "Receiver", 2m, 0.5m) { Timestamp = DateTime.UtcNow.AddSeconds(-120) };
                        var recentTx = new Transaction("Sender3", "Receiver", 3m, 0.5m) { Timestamp = DateTime.UtcNow };

                        svc2.PendingTransactions.Add(oldTx1);
                        svc2.PendingTransactions.Add(oldTx2);
                        svc2.PendingTransactions.Add(recentTx);

                        Console.WriteLine($"Pending before eviction: {svc2.PendingTransactions.Count}");
                        int evicted = svc2.EvictStaleTransactions(60);
                        Console.WriteLine($"Evicted {evicted} stale transactions");
                        Console.WriteLine($"Pending after eviction: {svc2.PendingTransactions.Count}");

                        // --- Scenario 3: Anti-Spam ---
                        var svc3 = blockChainService.Clone();
                        svc3.MineBlock(myWallet.PublicKey);

                        string victim = "SpamVictim";
                        int added = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            var spamTx = TransactionService.CreateTransaction(myWallet.PublicKey, victim, 1m, 1.0m);
                            TransactionService.SignTransaction(spamTx, myWallet.PrivateKey);
                            try
                            {
                                svc3.AddTransactionToMempool(spamTx);
                                added++;
                                Console.WriteLine($"Tx {i+1} added to mempool");
                            }
                            catch (InvalidOperationException ex)
                            {
                                Console.WriteLine($"Tx {i+1} rejected: {ex.Message}");
                            }
                        }
                        Console.WriteLine($"Total added: {added}, final pending count: {svc3.PendingTransactions.Count}");
                        break;

                    case "9":
                        hwFlag = false;
                        break;

                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
            break;

        default:
            Console.WriteLine("Invalid choice. Please try again.");
            break;
    }
}