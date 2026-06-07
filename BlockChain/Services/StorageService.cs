using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BlockChain.Model;

namespace BlockChain.Services
{
    public class NetworkPassport
    {
        public string StudentId { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }

        // Unique per student: born from the genesis block of this particular machine
        // (Author = StudentId, fixed Timestamp), mined deterministically.
        public string GenesisBlockHash { get; set; } = string.Empty;

        public int ChainLength { get; set; }
        public int CompromisedBlockIndex { get; set; }
        public string CompromisedBlockHash { get; set; } = string.Empty;

        // Full forensic audit object, as required by the assignment.
        public AuditReport? AuditResult { get; set; }

        // SHA-256 over all fields above — self-sealing. Editing any field by hand
        // breaks the signature when the verifier recomputes it.
        public string PassportSignature { get; set; } = string.Empty;
    }

    public class StorageService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public void GenerateNetworkPassport(
            string studentId,
            string genesisBlockHash,
            List<Block> chain,
            AuditReport auditReport,
            Block? attackOrigin)
        {
            var passport = new NetworkPassport
            {
                StudentId = studentId,
                GeneratedAt = DateTime.UtcNow,
                GenesisBlockHash = genesisBlockHash,
                ChainLength = chain.Count,
                CompromisedBlockIndex = attackOrigin?.Index ?? -1,
                CompromisedBlockHash = attackOrigin?.Hash ?? string.Empty,
                AuditResult = auditReport
            };

            // Compute self-seal signature (SHA-256 of concatenated fields)
            passport.PassportSignature = ComputePassportSignature(passport);

            // Serialize and write to disk
            var json = JsonSerializer.Serialize(passport, JsonOptions);
            File.WriteAllText("passport.json", json);
            Console.WriteLine("[StorageService] Network passport written to passport.json");
            Console.WriteLine($"[StorageService] Signature: {passport.PassportSignature}");
        }

        // Recomputes the signature from the stored fields and compares it to the embedded one.
        // Returns true only if nothing was tampered with after sealing.
        public static bool VerifyPassport(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"[Verify] File not found: {path}");
                return false;
            }

            var passport = JsonSerializer.Deserialize<NetworkPassport>(File.ReadAllText(path));
            if (passport == null)
            {
                Console.WriteLine("[Verify] Could not read passport.");
                return false;
            }

            var expected = ComputePassportSignature(passport);
            bool ok = string.Equals(expected, passport.PassportSignature, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine(ok
                ? "[Verify] ✓ Signature valid — passport is authentic and untampered."
                : $"[Verify] ✗ Signature MISMATCH — passport was edited.\n  stored:   {passport.PassportSignature}\n  expected: {expected}");
            return ok;
        }

        private static string ComputePassportSignature(NetworkPassport passport)
        {
            // Serialize the audit deterministically so it contributes to the seal.
            var auditJson = JsonSerializer.Serialize(passport.AuditResult);
            var raw = string.Join("|",
                passport.StudentId,
                passport.GeneratedAt.ToString("O"),
                passport.GenesisBlockHash,
                passport.ChainLength,
                passport.CompromisedBlockIndex,
                passport.CompromisedBlockHash,
                auditJson);

            var bytes = Encoding.UTF8.GetBytes(raw);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLower();
        }
    }
}
