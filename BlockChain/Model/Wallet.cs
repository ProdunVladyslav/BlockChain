using BlockChain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BlockChain.Model
{
    public class Wallet
    {
        [JsonPropertyName("publicKey")]
        public string PublicKey { get; set; } // The public key can be shared freely and is used to receive funds
        [JsonPropertyName("privateKey")]
        public string PrivateKey { get; set; } // The private key should be kept secret and is used to sign transactions
        public Wallet(CryptoService cryptoService)
        {
            var keys = cryptoService.GenerateKeyPair(); // Generate a new key pair when the wallet is created
            PublicKey = keys.publicKey;
            PrivateKey = keys.privateKey;
        }
    }
}
