using BlockChain.Chain;
using BlockChain.Model;
using System;
using System.Linq;

namespace BlockChain.Services.P2P.Handlers
{
    public class NewChainHandler : MessageHandlerBase
    {
        private readonly BlockChainService _blockChainService;
        private readonly P2PClient _p2pClient;
        private readonly StorageService _storageService;

        public NewChainHandler(BlockChainService blockChainService, P2PClient p2pClient, StorageService storageService)
        {
            _blockChainService = blockChainService;
            _p2pClient = p2pClient;
            _storageService = storageService;
        }

        public override object Handle(object request)
        {
            var ctx = request as MessageContext;
            if (ctx?.Message.Type == "NEW_CHAIN")
            {
                var newChain = System.Text.Json.JsonSerializer.Deserialize<List<Block>>(ctx.Message.Data);
                if (newChain != null)
                {
                    // ── Forensic audit before accepting chain ─────────────────
                    var audit = _blockChainService.RunFullAudit(newChain);
                    if (!audit.IsChainValid)
                    {
                        var attackOrigin = _blockChainService.FindAttackOrigin(audit, newChain);
                        var forensicReport = _blockChainService.GenerateForensicReport(audit, attackOrigin);
                        _storageService.GenerateNetworkPassport(
                            _blockChainService.StudentId,
                            _blockChainService.Chain.First().Hash,
                            newChain, audit, attackOrigin);
                        Console.WriteLine(forensicReport);
                        Console.WriteLine($"[CONSENSUS] REJECTED chain from {ctx.RemoteEndpoint}: " +
                                          $"{audit.CompromisedBlockIndexes.Count} compromised block(s).");
                        return null;
                    }

                    // ── Audit passed — apply consensus rules (longest / most work) ──
                    int before = _blockChainService.Chain.Count;
                    _blockChainService.ReplaceChain(newChain);
                    int after = _blockChainService.Chain.Count;
                    if (after != before)
                    {
                        Console.WriteLine($"[CONSENSUS] Accepted chain from {ctx.RemoteEndpoint}. Length {before} → {after}.");
                        _p2pClient.BroadcastChainAsync(_blockChainService.Chain).GetAwaiter().GetResult();
                    }
                    else
                    {
                        Console.WriteLine($"[CONSENSUS] Chain from {ctx.RemoteEndpoint} did not win (not longer / less work). Kept ours ({before}).");
                    }
                }
                return null;
            }
            return base.Handle(request);
        }
    }
}
