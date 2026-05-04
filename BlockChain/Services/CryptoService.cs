using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain.Services
{
    public class CryptoService
    {
        // Generate a new RSA key pair
        public (string publicKey, string privateKey) GenerateKeyPair()
        {
            using (var rsa = RSA.Create())
            {
                var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey()); // In a real application, you'd want to securely store the private key
                var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey()); // The public key can be shared freely
                return (publicKey, privateKey);
            }
        }

        // Sign data with a private key
        public byte[] SignData(string data, string privateKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _); // Load the private key
                var dataBytes = Encoding.UTF8.GetBytes(data); // Sign the data using SHA256 and PKCS#1 v1.5 padding
                return rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1); // Return the signature as a byte array
            }
        }

        // Verify a signature with a public key
        public bool VerifySignature(string data, byte[] signature, string publicKey)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _); // Load the public key
                var dataBytes = Encoding.UTF8.GetBytes(data); // Verify the signature using the same hash and padding as signing
                return rsa.VerifyData(dataBytes, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1); // Return true if the signature is valid
            }
        }
    }
}
