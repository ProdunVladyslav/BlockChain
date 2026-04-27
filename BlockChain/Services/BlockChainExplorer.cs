using BlockChain.HashingService;
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
    }
}
