using BlockChain.Chain;
using BlockChain.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockChain.Services.P2P.Handlers
{
    public class SyncMempoolHandler : MessageHandlerBase
    {
        private readonly BlockChainService _blockChainService;

        public SyncMempoolHandler(BlockChainService blockChainService)
        {
            _blockChainService = blockChainService;
        }

        public override object Handle(object request)
        {
            var ctx = request as MessageContext;
            if (ctx?.Message.Type == "SYNC_MEMPOOL")
            {
                var transactions = System.Text.Json.JsonSerializer.Deserialize<List<Transaction>>(ctx.Message.Data);
                if (transactions == null) return null;

                int added = _blockChainService.MergeMempool(transactions);
                Console.WriteLine($"Synced {added} transactions into mempool from {ctx.RemoteEndpoint}");
                return null;
            }
            return base.Handle(request);
        }
    }
}
