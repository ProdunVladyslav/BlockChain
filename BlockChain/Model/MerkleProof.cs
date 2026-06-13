namespace BlockChain.Model;

public class MerkleProof
{
    public Guid TransactionId { get; set; }
    public string TransactionHash { get; set; } = string.Empty;
    public string MerkleRoot { get; set; } = string.Empty;
    public int BlockIndex { get; set; }
    public string BlockHash { get; set; } = string.Empty;
    public List<ProofStep> Steps { get; set; } = new();
}

public class ProofStep
{
    public string SiblingHash { get; set; } = string.Empty;
    public bool IsLeft { get; set; }
}
