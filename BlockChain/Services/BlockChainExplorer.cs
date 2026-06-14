using BlockChain.Chain;
using BlockChain.Model;

namespace BlockChain.Services
{
    public class BlockChainExplorer
    {
        public BlockChainService blockChain { get; set; }
        public BlockChainExplorer(BlockChainService blocks)
        {
            bool isValid = blocks?.IsChainValid() ?? false;
            if (!isValid)
            {
                throw new ArgumentException("The provided blockchain is not valid.");
            }
            blockChain = blocks;
        }

        public decimal GetTotalTransactionVolume()
        {
            return blockChain.Chain
                .Sum(block => block.Transactions.Sum(t => t.Amount));
        }

        public Transaction? GetLargestTransaction()
        {
            return blockChain.Chain.SelectMany(block => block.Transactions)
                .OrderByDescending(t => t.Amount)
                .FirstOrDefault();
        }

        public List<Transaction> GetAddressHistory(string address)
        {
            return blockChain.Chain.SelectMany(block => block.Transactions)
                .Where(t => t.From == address || t.To == address)
                .ToList();
        }

        public decimal GetTotalBurnedFees()
        {
            return blockChain.Chain
                .Skip(1)
                .SelectMany(b => b.Transactions)
                .Where(t => t.From != "COINBASE")
                .Sum(t => blockChain.NetworkBaseFee);
        }

        public decimal GetActualTotalSupply()
        {
            decimal totalEmitted = blockChain.Chain
                .SelectMany(b => b.Transactions)
                .Where(t => t.From == "COINBASE")
                .Sum(t => t.Amount);

            decimal totalTips = blockChain.Chain
                .Skip(1)
                .SelectMany(b => b.Transactions)
                .Where(t => t.From != "COINBASE")
                .Sum(t => t.Fee - blockChain.NetworkBaseFee);

            return totalEmitted - GetTotalBurnedFees() - totalTips;
        }

        public (Block? block, Transaction? tx) FindTransactionLocation(Guid txId)
        {

            Transaction? foundTx = blockChain.Chain.SelectMany(block => block.Transactions).FirstOrDefault(t => t.Id == txId);

            if(foundTx == null)
            {
                return (null, null);
            }

            Block? foundBlock = blockChain.Chain.FirstOrDefault(block => block.Transactions.Any(t => t.Id == txId));
            return (foundBlock, foundTx);
        }

        public Transaction? FindTransactionById(string txId)
        {
            Guid.TryParse(txId, out var txGuid);
            Transaction? tx = blockChain.Chain.SelectMany(b => b.Transactions).FirstOrDefault(t => t.Id == txGuid);

            if (tx == null)
            {
                tx = blockChain.PendingTransactions.FirstOrDefault(t => t.Id == txGuid);
            }
            return tx;
        }

        public Block? FindBlockByTransactionId(string txId)
        {
            Guid.TryParse(txId, out var txGuid);
            Transaction? tx = blockChain.Chain.SelectMany(b => b.Transactions).FirstOrDefault(t => t.Id == txGuid);

            if (tx == null)
                return null;

            Block? foundBlock = blockChain.Chain.FirstOrDefault(block => block.Transactions.Any(t => t.Id == txGuid));
            return foundBlock;
        }

        public List<Transaction> GetTransactionHistory(string address)
        {
            List<Transaction> txs = blockChain.Chain.SelectMany(b => b.Transactions)
                .Where(tx => tx.From == address || tx.To == address)
                .OrderByDescending(tx => tx.Timestamp)
                .ToList();

            return txs;
        }

        public List<string> GetOwnedNFTs(string address)
        {
            var owned = new HashSet<string>();
            var txs = blockChain.Chain.SelectMany(b => b.Transactions)
                .Where(t => t.Type == TransactionType.MINT_NFT && !string.IsNullOrWhiteSpace(t.NftDataUrl))
                .OrderBy(t => t.Timestamp)
                .ToList();

            foreach (var tx in txs)
            {
                if (tx.To == address)
                    owned.Add(tx.NftDataUrl!);
                if (tx.From == address)
                    owned.Remove(tx.NftDataUrl!);
            }

            return owned.ToList();
        }

        public decimal GetTotalFeesEarned(string minerAddress)
        {
            return blockChain.Chain
                .Where(b => b.Author == minerAddress)
                .SelectMany(b => b.Transactions)
                .Where(tx => tx.From != "COINBASE")
                .Sum(tx => tx.Fee);
        }
    }
}
