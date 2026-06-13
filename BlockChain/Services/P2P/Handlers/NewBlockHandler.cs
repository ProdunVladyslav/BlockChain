using BlockChain.Chain;
using BlockChain.Model;
using System;
using System.Linq;

namespace BlockChain.Services.P2P.Handlers
{
    public class NewBlockHandler : MessageHandlerBase
    {
        private readonly BlockChainService _blockChainService;
        private readonly P2PClient _p2pClient;

        public NewBlockHandler(BlockChainService blockChainService, P2PClient p2pClient)
        {
            _blockChainService = blockChainService;
            _p2pClient = p2pClient;
        }

        public override object Handle(object request)
        {
            var ctx = request as MessageContext;
            if (ctx?.Message.Type == "NEW_BLOCK")
            {
                var newBlock = System.Text.Json.JsonSerializer.Deserialize<Block>(ctx.Message.Data);
                if (newBlock != null)
                {
                    var lastBlock = _blockChainService.Chain.LastOrDefault();
                    var hash = HashingService.ComputeHash(newBlock);
                    var transactionsValid = newBlock.Transactions.All(tx => TransactionService.ValidateTransaction(tx).isValid);
                    if (hash == newBlock.Hash && lastBlock != null && newBlock.PreviousHash == lastBlock.Hash && transactionsValid)
                    {
                        _blockChainService.ApplyBlock(newBlock);
                        Console.WriteLine($"New block received from {ctx.RemoteEndpoint} and added to chain. Height is now {_blockChainService.Chain.Count}.");
                        _p2pClient.BroadcastBlockAsync(newBlock).GetAwaiter().GetResult();
                    }
                    else
                    {
                        Console.WriteLine($"Received invalid block from {ctx.RemoteEndpoint}. Hash or previous hash mismatch. Requesting chain...");
                        var ip = ctx.RemoteEndpoint.Split(':')[0];
                        var peerEntry = _p2pClient._peers.FirstOrDefault(p => p.StartsWith(ip + ":"));
                        if (peerEntry != null)
                        {
                            var parts = peerEntry.Split(':');
                            _p2pClient.RequestChainAsync(parts[0], int.Parse(parts[1])).GetAwaiter().GetResult();
                        }
                        else
                        {
                            Console.WriteLine($"Cannot request chain — no listening port known for {ip}");
                        }
                    }
                }
                return null;
            }
            return base.Handle(request);
        }
    }
}
