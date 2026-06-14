using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BlockChain.Model
{
    public enum TransactionType
    {
        DEFAULT,
        MINT_NFT,
    }
    public class Transaction
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        [JsonPropertyName("from")]
        public string From { get; set; }
        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("signature")]
        public byte[] Signature { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("tokenSymbol")]
        public string TokenSymbol { get; set; }

        [JsonPropertyName("fee")]
        public decimal Fee { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("nftDataUrl")]
        public string? NftDataUrl { get; set; }

        public TransactionType Type { get; set; }

        public Transaction(string from, string to, decimal amount, decimal fee, int lockTime = 0, string tokenSymbol = "MAIN", TransactionType type = TransactionType.DEFAULT, string? nftDataUrl = null)
        {
            Id = Guid.NewGuid();
            From = from;
            To = to;
            Amount = amount;
            Fee = fee;
            LockTime = lockTime;
            TokenSymbol = tokenSymbol;
            Type = type;
            NftDataUrl = nftDataUrl;
        }

        [JsonPropertyName("lockTime")]
        public int LockTime { get; set; } = 0;

        public string ToRawString()
        {
            return $"{From}{To}{Amount}{Fee}{LockTime}{Timestamp:O}{TokenSymbol}{Type}{NftDataUrl}";
        }

        public override bool Equals(object? obj)
        {
            if (obj is Transaction other)
                return Id == other.Id;
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"Transaction {Id}, From: {From}, To: {To}, Token: {TokenSymbol}, Amount: {Amount}, Fee: {Fee}, LockTime: {LockTime}, Timestamp: {Timestamp:O}";
        }

    }
}
