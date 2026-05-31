using BlockChain.Model;

namespace BlockChain.Services
{
    public static class TransactionService
    {
        private static readonly CryptoService cryptoService;
        static TransactionService()
        {
            cryptoService = new CryptoService();
        }

        public static Transaction CreateTransaction(string from, string to, decimal amount, decimal fee)
        {
            var tx = new Transaction(from, to, amount, fee);
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
            if (transaction.From == "COINBASE")
            {
                // Coinbase transactions don't need signature validation
                if(transaction.Fee < 0) return (false, "Transaction fee must be non-negative.");
                return (true, string.Empty);
            }
            if (transaction.Signature == null || transaction.Signature.Length == 0)
                return (false, "Transaction must be signed.");
            if(!cryptoService.VerifySignature(transaction.ToRawString(), transaction.Signature, transaction.From))
                return (false, "Invalid transaction signature.");
            if(transaction.Fee < 0) return (false, "Transaction fee must be non-negative.");
            return (true, string.Empty);
        }

        public static void SignTransaction(Transaction transaction, string privateKey)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null.");
            if (string.IsNullOrWhiteSpace(privateKey))
                throw new ArgumentException("Private key cannot be empty.", nameof(privateKey));
            transaction.Signature = cryptoService.SignData(transaction.ToRawString(), privateKey);
        }
    }
}
