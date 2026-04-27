using BlockChain.Model;

namespace BlockChain.Services
{
    public static class TransactionService
    {
        public static Transaction CreateTransction(string from, string to, decimal amount)
        {
            var tx = new Transaction(from, to, amount);
            var validation = ValidateTransaction(tx);
            if (!validation.isValid)
            {
                throw new InvalidOperationException(validation.error);
            }
            return tx;
        }

        public static (bool isValid, string error) ValidateTransaction(Transaction transaction)
        {
            if (transaction == null)
                return (false, "Transaction cannot be null.");
            if (string.IsNullOrWhiteSpace(transaction.From))
                return (false, "Sender address cannot be empty.");
            if (string.IsNullOrWhiteSpace(transaction.To))
                return (false, "Recipient address cannot be empty.");
            if (transaction.Amount <= 0) return (false, "Amount must be greater than zero.");
            return (true, string.Empty);
        }
    }
}
