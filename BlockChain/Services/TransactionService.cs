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
            if (transaction.Signature == null || transaction.Signature.Length == 0) // Assuming signature is required for a valid transaction
                return (false, "Transaction must be signed.");
            if(!cryptoService.VerifySignature(transaction.ToRawString(), transaction.Signature, transaction.From)) // Assuming From is the public key or address that can be used to verify the signature
                return (false, "Invalid transaction signature.");
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
