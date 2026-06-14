using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlockChain.Services
{
    public class ColdWalletService
    {
        public void GenerateOfflineTransaction(string from, string to, decimal amount, decimal fee, string tokenSymbol, string privateKey, string filePath)
        {
            var tx = TransactionService.CreateTransaction(from, to, amount, fee, tokenSymbol);
            TransactionService.SignTransaction(tx, privateKey);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(tx, options);
            File.WriteAllText(filePath, json);
        }

    }
}
