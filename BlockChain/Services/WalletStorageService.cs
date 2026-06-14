using BlockChain.Model;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BlockChain.Services
{
    public class WalletStorageService
    {
        private const string WalletFilePath = "wallet.json";
        private const int SaltSize = 16;
        private const int IvSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static void SaveWallet(Wallet wallet, string password)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            var encryptedPrivateKey = EncryptPrivateKey(wallet.PrivateKey, password);

            var walletData = new WalletData
            {
                PublicKey = wallet.PublicKey,
                EncryptedPrivateKey = encryptedPrivateKey
            };

            var json = JsonSerializer.Serialize(walletData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(WalletFilePath, json);
            Console.WriteLine($"[Wallet] Encrypted wallet saved to {WalletFilePath}");
        }

        public static Wallet LoadWallet(string password, CryptoService cryptoService)
        {
            if (!File.Exists(WalletFilePath))
                throw new FileNotFoundException("Wallet file not found.", WalletFilePath);

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            var json = File.ReadAllText(WalletFilePath);
            var walletData = JsonSerializer.Deserialize<WalletData>(json);

            if (walletData == null || string.IsNullOrWhiteSpace(walletData.EncryptedPrivateKey))
                throw new InvalidOperationException("Invalid wallet file format.");

            var privateKey = DecryptPrivateKey(walletData.EncryptedPrivateKey, password);
            return new Wallet(cryptoService, walletData.PublicKey, privateKey);
        }

        public static string RevealPrivateKey(string password)
        {
            if (!File.Exists(WalletFilePath))
                throw new FileNotFoundException("Wallet file not found.", WalletFilePath);

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            var json = File.ReadAllText(WalletFilePath);
            var walletData = JsonSerializer.Deserialize<WalletData>(json);

            if (walletData == null || string.IsNullOrWhiteSpace(walletData.EncryptedPrivateKey))
                throw new InvalidOperationException("Invalid wallet file format.");

            return DecryptPrivateKey(walletData.EncryptedPrivateKey, password);
        }

        private static string EncryptPrivateKey(string privateKey, string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var iv = RandomNumberGenerator.GetBytes(IvSize);

            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = deriveBytes.GetBytes(KeySize);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(privateKey);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var combined = new byte[salt.Length + iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
            Buffer.BlockCopy(iv, 0, combined, salt.Length, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, combined, salt.Length + iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(combined);
        }

        private static string DecryptPrivateKey(string encryptedPrivateKey, string password)
        {
            var combined = Convert.FromBase64String(encryptedPrivateKey);
            if (combined.Length < SaltSize + IvSize)
                throw new InvalidOperationException("Invalid encrypted private key format.");

            var salt = new byte[SaltSize];
            var iv = new byte[IvSize];
            var cipherBytes = new byte[combined.Length - SaltSize - IvSize];

            Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(combined, SaltSize, iv, 0, IvSize);
            Buffer.BlockCopy(combined, SaltSize + IvSize, cipherBytes, 0, cipherBytes.Length);

            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = deriveBytes.GetBytes(KeySize);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private class WalletData
        {
            public string PublicKey { get; set; } = string.Empty;
            public string EncryptedPrivateKey { get; set; } = string.Empty;
        }
    }
}
