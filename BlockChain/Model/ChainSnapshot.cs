using BlockChain.Model;
using System.Text.Json.Serialization;

public class ChainSnapshot
{
    [JsonPropertyName("exportedAt")] 
    public DateTime ExportedAt { get; set; }
    [JsonPropertyName("chainLength")] 
    public int ChainLength { get; set; }
    [JsonPropertyName("difficulty")] 
    public double Difficulty { get; set; }
    [JsonPropertyName("totalSupply")] 
    public decimal TotalSupply { get; set; }
    [JsonPropertyName("balances")] 
    public Dictionary<string, decimal> Balances { get; set; } = new();
    [JsonPropertyName("chain")] 
    public List<Block> Chain { get; set; } = new();
}