using BlockChain.Chain;
using BlockChain.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockChain.Services.P2P.Handlers
{
    public class RequestMempoolHandler : MessageHandlerBase
    {
        private readonly BlockChainService _blockChainService;

        public RequestMempoolHandler(BlockChainService blockChainService)
        {
            _blockChainService = blockChainService;
        }

        public override object Handle(object request)
        {
            var ctx = request as MessageContext;
            if (ctx?.Message.Type == "REQUEST_MEMPOOL")
            {
                var mempool = _blockChainService.PendingTransactions;
                var json = System.Text.Json.JsonSerializer.Serialize(mempool);
                var response = new NetworkMessage("SYNC_MEMPOOL", json);
                var responseJson = System.Text.Json.JsonSerializer.Serialize(response);

                ctx.ResponseWriter?.WriteLineAsync(responseJson).GetAwaiter().GetResult();
                Console.WriteLine($"Mempool sent to {ctx.RemoteEndpoint} ({mempool.Count} txs)");
                return null;
            }
            return base.Handle(request);
        }
    }
}
