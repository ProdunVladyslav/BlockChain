using BlockChain.HashingService;
using BlockChain.Model;
using BlockChain.Services;

var blockChainService = new BlockChainService();
var cryptoService = new CryptoService();

// Pre-generated wallets for the session
var wallets = new Dictionary<string, Wallet>(StringComparer.OrdinalIgnoreCase);
var pendingTransactions = new List<Transaction>();

Console.WriteLine("Blockchain CLI started.");
Console.WriteLine("No wallets exist yet. Use option [5] to create one.\n");

bool running = true;
while (running)
{
    PrintMenu();
    string input = Console.ReadLine()?.Trim() ?? string.Empty;

    switch (input)
    {
        case "1":
            HandleAddTransaction();
            break;
        case "2":
            HandleMineBlock();
            break;
        case "3":
            HandlePrintChain();
            break;
        case "4":
            HandleValidate();
            break;
        case "5":
            HandleCreateWallet();
            break;
        case "6":
            HandleCheckBalance();
            break;
        case "7":
            HandleShowPending();
            break;
        case "8":
            HandleAnalyzeChain();
            break;
        case "0":
            running = false;
            Console.WriteLine("Exiting. Goodbye.");
            break;
        default:
            Console.WriteLine("Unknown option. Please try again.");
            break;
    }
}

// --- Menu printer ---

void PrintMenu()
{
    Console.WriteLine();
    Console.WriteLine("=== Blockchain Menu ===");
    Console.WriteLine("[1] Add transaction to pending list");
    Console.WriteLine("[2] Mine block (includes all pending transactions)");
    Console.WriteLine("[3] Print blockchain");
    Console.WriteLine("[4] Validate chain (IsChainValid)");
    Console.WriteLine("[5] Create new wallet");
    Console.WriteLine("[6] Check wallet balance");
    Console.WriteLine("[7] Show pending transactions");
    Console.WriteLine("[8] Analyze chain (detailed error report)");
    Console.WriteLine("[0] Exit");
    Console.Write("Choose: ");
}

// --- Option handlers ---

void HandleCreateWallet()
{
    Console.Write("Enter a name/label for this wallet: ");
    string label = Console.ReadLine()?.Trim() ?? string.Empty;

    if (string.IsNullOrWhiteSpace(label))
    {
        Console.WriteLine("Wallet label cannot be empty.");
        return;
    }

    if (wallets.ContainsKey(label))
    {
        Console.WriteLine($"A wallet with label '{label}' already exists.");
        return;
    }

    var wallet = new Wallet(cryptoService);
    wallets[label] = wallet;
    Console.WriteLine($"Wallet '{label}' created.");
    Console.WriteLine($"  Public key (short): {wallet.PublicKey[..24]}...");
}

void HandleAddTransaction()
{
    if (wallets.Count == 0)
    {
        Console.WriteLine("No wallets available. Create at least two wallets first (option [5]).");
        return;
    }

    Console.WriteLine("Available wallets: " + string.Join(", ", wallets.Keys));

    Console.Write("Sender wallet label: ");
    string senderLabel = Console.ReadLine()?.Trim() ?? string.Empty;

    if (!wallets.TryGetValue(senderLabel, out Wallet? senderWallet))
    {
        Console.WriteLine($"Wallet '{senderLabel}' not found.");
        return;
    }

    Console.Write("Recipient wallet label: ");
    string recipientLabel = Console.ReadLine()?.Trim() ?? string.Empty;

    if (!wallets.TryGetValue(recipientLabel, out Wallet? recipientWallet))
    {
        Console.WriteLine($"Wallet '{recipientLabel}' not found.");
        return;
    }

    if (senderLabel.Equals(recipientLabel, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Sender and recipient cannot be the same wallet.");
        return;
    }

    Console.Write($"Amount (network base fee is {blockChainService.NetworkBaseFee}): ");
    if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal amount) || amount <= 0)
    {
        Console.WriteLine("Invalid amount. Must be a positive number.");
        return;
    }

    Console.Write($"Fee (minimum {blockChainService.NetworkBaseFee}): ");
    if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal fee) || fee < 0)
    {
        Console.WriteLine("Invalid fee. Must be a non-negative number.");
        return;
    }

    try
    {
        var tx = new Transaction(senderWallet.PublicKey, recipientWallet.PublicKey, amount, fee);
        TransactionService.SignTransaction(tx, senderWallet.PrivateKey);
        pendingTransactions.Add(tx);
        Console.WriteLine($"Transaction added to pending list: {senderLabel} -> {recipientLabel}, amount={amount}, fee={fee}");
        Console.WriteLine($"Pending count: {pendingTransactions.Count}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to create transaction: {ex.Message}");
    }
}

void HandleMineBlock()
{
    if (wallets.Count == 0)
    {
        Console.WriteLine("No wallets available. Create a miner wallet first (option [5]).");
        return;
    }

    Console.WriteLine("Available wallets: " + string.Join(", ", wallets.Keys));
    Console.Write("Miner wallet label: ");
    string minerLabel = Console.ReadLine()?.Trim() ?? string.Empty;

    if (!wallets.TryGetValue(minerLabel, out Wallet? minerWallet))
    {
        Console.WriteLine($"Wallet '{minerLabel}' not found.");
        return;
    }

    // Add pending transactions to the mempool before mining
    int accepted = 0;
    int rejected = 0;
    foreach (var tx in pendingTransactions)
    {
        int beforeCount = blockChainService.PendingTransactions.Count;
        blockChainService.AddTransactionToMempool(tx);
        if (blockChainService.PendingTransactions.Count > beforeCount)
            accepted++;
        else
            rejected++;
    }

    pendingTransactions.Clear();
    Console.WriteLine($"Submitted {accepted + rejected} transaction(s) to mempool: {accepted} accepted, {rejected} rejected.");

    Console.WriteLine("Mining block... (this may take a moment)");
    blockChainService.MineBlock(minerWallet.PublicKey);

    var newBlock = blockChainService.Chain.Last();
    Console.WriteLine($"Block #{newBlock.Index} mined. Hash: {newBlock.Hash[..16]}...");
    Console.WriteLine($"Transactions in block: {newBlock.Transactions.Count}");
}

void HandlePrintChain()
{
    Console.WriteLine($"\n=== Blockchain ({blockChainService.Chain.Count} blocks) ===");
    foreach (var block in blockChainService.Chain)
    {
        Console.WriteLine($"  Block #{block.Index}");
        Console.WriteLine($"    Hash:     {block.Hash[..16]}...");
        Console.WriteLine($"    PrevHash: {block.PreviousHash[..Math.Min(16, block.PreviousHash.Length)]}...");
        Console.WriteLine($"    Txs:      {block.Transactions.Count}");
        Console.WriteLine($"    Difficulty at mining: {block.DifficultyAtMining:F2}");
    }
}

void HandleValidate()
{
    bool valid = blockChainService.IsChainValid();
    Console.WriteLine(valid
        ? "Chain is valid."
        : "Chain is INVALID. Use option [8] for a detailed report.");
}

void HandleCheckBalance()
{
    if (wallets.Count == 0)
    {
        Console.WriteLine("No wallets available.");
        return;
    }

    Console.WriteLine("Available wallets: " + string.Join(", ", wallets.Keys));
    Console.Write("Wallet label: ");
    string label = Console.ReadLine()?.Trim() ?? string.Empty;

    if (!wallets.TryGetValue(label, out Wallet? wallet))
    {
        Console.WriteLine($"Wallet '{label}' not found.");
        return;
    }

    decimal balance = blockChainService.GetBalance(wallet.PublicKey);
    Console.WriteLine($"Balance of '{label}': {balance}");
}

void HandleShowPending()
{
    if (pendingTransactions.Count == 0)
    {
        Console.WriteLine("No pending transactions in local list.");
    }
    else
    {
        Console.WriteLine($"Pending transactions ({pendingTransactions.Count}):");
        foreach (var tx in pendingTransactions)
        {
            Console.WriteLine($"  {tx.From[..16]}... -> {tx.To[..16]}..., amount={tx.Amount}, fee={tx.Fee}");
        }
    }

    if (blockChainService.PendingTransactions.Count > 0)
    {
        Console.WriteLine($"Mempool (not yet mined): {blockChainService.PendingTransactions.Count} transaction(s)");
    }
}

void HandleAnalyzeChain()
{
    Console.WriteLine("\n=== Chain analysis ===");
    var errors = blockChainService.AnalyzeChain();
    if (errors.Count == 0)
        Console.WriteLine("No errors detected. Chain is healthy.");
    else
        Console.WriteLine($"Total errors found: {errors.Count}");
}