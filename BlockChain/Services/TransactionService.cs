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

        public static Transaction CreateTransaction(string from, string to, decimal amount, decimal fee, string tokenSymbol = "MAIN", TransactionType type = TransactionType.DEFAULT, string nftDataUrl = null)
        {
            if (type == TransactionType.MINT_NFT)
            {
                if (string.IsNullOrWhiteSpace(nftDataUrl))
                {
                    throw new ArgumentException("NFT data URL must be provided for MINT_NFT transactions.", nameof(nftDataUrl));
                }
                if (amount != 1)
                {
                    throw new ArgumentException("Amount must be one for MINT_NFT transactions.", nameof(amount));
                }
            }
            var tx = new Transaction(from, to, amount, fee, tokenSymbol: tokenSymbol, type: type, nftDataUrl: nftDataUrl);
            var validation = ValidateTransaction(tx, false);
            if (!validation.isValid)
            {
                throw new InvalidOperationException(validation.error);
            }
            return tx;
        }


        public static (bool isValid, string error) ValidateTransaction(Transaction transaction, bool checkSignature = true)
        {
            if (transaction == null)
                return (false, "Transaction cannot be null.");
            if (string.IsNullOrWhiteSpace(transaction.From))
                return (false, "Sender address cannot be empty.");
            if (string.IsNullOrWhiteSpace(transaction.To))
                return (false, "Recipient address cannot be empty.");
            if (transaction.Amount <= 0) return (false, "Amount must be greater than zero.");

            if (transaction.Type == TransactionType.MINT_NFT)
            {
                if (string.IsNullOrWhiteSpace(transaction.NftDataUrl))
                    return (false, "NFT data URL must be provided for MINT_NFT transactions.");
                if (transaction.Amount != 1)
                    return (false, "Amount must be exactly one for MINT_NFT transactions.");
            }

            if (transaction.From == "COINBASE" || transaction.From == "MINT")
            {
                // Coinbase and mint transactions don't need signature validation
                if(transaction.Fee < 0) return (false, "Transaction fee must be non-negative.");
                return (true, string.Empty);
            }


            if (checkSignature)
            {
                if (transaction.Signature == null || transaction.Signature.Length == 0)
                    return (false, "Transaction must be signed.");
                if (!cryptoService.VerifySignature(transaction.ToRawString(), transaction.Signature, transaction.From))
                    return (false, "Invalid transaction signature.");
            }
            
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
