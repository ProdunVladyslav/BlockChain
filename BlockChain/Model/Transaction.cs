using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain.Model
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        
        public Decimal Amount { get; set; }
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
