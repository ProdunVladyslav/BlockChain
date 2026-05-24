using BlockChain.HashingService;
using BlockChain.Model;
using BlockChain.Services;
using BlockChain.Services.P2P;
using Microsoft.Extensions.DependencyInjection;

var service = new ServiceCollection();
service.AddSingleton<BlockChainService>();
service.AddSingleton<CryptoService>();
service.AddSingleton<P2PClient>();
service.AddSingleton<DisplayService>();
service.AddSingleton<P2PServer>();

var provider = service.BuildServiceProvider();

var blockChainService = provider.GetRequiredService<BlockChainService>();
var p2pServer = provider.GetRequiredService<P2PServer>();
var p2pClient = provider.GetRequiredService<P2PClient>();
var cryptoService = provider.GetRequiredService<CryptoService>();
var displayService = provider.GetRequiredService<DisplayService>();

var myWallet = new Wallet(cryptoService);

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
    Console.WriteLine("7 - Exit");
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
            p2pClient.ConnectAsync(nodeAddress);
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

        case "7":
            Console.WriteLine("Exiting...");
            flag = false;
            break;

        case "9":
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

        default:
            Console.WriteLine("Invalid choice. Please try again.");
            break;
    }
}