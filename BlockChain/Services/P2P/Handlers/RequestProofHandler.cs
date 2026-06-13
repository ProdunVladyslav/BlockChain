using BlockChain.Chain;
using BlockChain.Model;
using System.Security.Cryptography;

namespace BlockChain.Services.P2P.Handlers
{
    public class RequestProofHandler : MessageHandlerBase
    {
        private readonly BlockChainService _blockChainService;
        private readonly P2PClient _p2pClient;

        public RequestProofHandler(BlockChainService blockChainService, P2PClient p2pClient)
        {
            _blockChainService = blockChainService;
            _p2pClient = p2pClient;
        }

        public override object Handle(object request)
        {
            var ctx = request as MessageContext;
            if (ctx?.Message.Type == "REQUEST_PROOF")
            {
                if (!Guid.TryParse(ctx.Message.Data, out Guid txId))
                {
                    Console.WriteLine($"Invalid REQUEST_PROOF — not a valid GUID: {ctx.Message.Data}");
                    return null;
                }

                var block = _blockChainService.Chain
                    .FirstOrDefault(b => b.Transactions.Any(t => t.Id == txId));

                if (block == null)
                {
                    Console.WriteLine($"Transaction {txId} not found in chain (requested by {ctx.RemoteEndpoint})");
                    return null;
                }

                var proof = HashingService.BuildMerkleProof(block, txId);
                if (proof == null)
                {
                    Console.WriteLine($"Could not build Merkle proof for {txId}");
                    return null;
                }

                // ── HW demo: fake mode — tamper with the MerkleRoot ─────────
                if (P2PServer.FakeMerkleMode)
                {
                    var randomHex = Convert.ToHexString(
                        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)
                    ).ToLower();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[FAKE MODE] Replacing real MerkleRoot ({proof.MerkleRoot[..16]}...) with fake ({randomHex[..16]}...)");
                    Console.ResetColor();
                    proof.MerkleRoot = randomHex;
                }

                var proofJson = System.Text.Json.JsonSerializer.Serialize(proof);
                var response = new NetworkMessage("PROOF_RESULT", proofJson);
                var responseJson = System.Text.Json.JsonSerializer.Serialize(response);

                ctx.ResponseWriter?.WriteLineAsync(responseJson).GetAwaiter().GetResult();
                Console.WriteLine($"Merkle proof sent for {txId} (block #{block.Index}) to {ctx.RemoteEndpoint}");
                return null;
            }
            return base.Handle(request);
        }
    }
}
