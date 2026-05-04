using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BlockChain.Model
{
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
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public Transaction(string from, string to, decimal amount)
        {
            Id = Guid.NewGuid();
            From = from;
            To = to;
            Amount = amount;
        }

        public string ToRawString()
        {
            return $"{From}{To}{Amount}{Timestamp:O}";
        }

        public override string ToString()
        {
            return $"Transaction {Id}, From: {From}, To: {To}, Amount: {Amount}, Timestamp: {Timestamp:O}";
        }
    }
}
