using BlockChain.Chain;
using BlockChain.Model;
using System;

namespace BlockChain.Services.P2P.Handlers
{
    public class NewTransactionHandler : MessageHandlerBase
    {
        private readonly BlockChainService _blockChainService;
        private readonly P2PClient _p2pClient;

        public NewTransactionHandler(BlockChainService blockChainService, P2PClient p2pClient)
        {
            _blockChainService = blockChainService;
            _p2pClient = p2pClient;
        }

        public override object Handle(object request)
        {
            var ctx = request as MessageContext;
            if (ctx?.Message.Type == "NEW_TRANSACTION")
            {
                var transaction = System.Text.Json.JsonSerializer.Deserialize<Transaction>(ctx.Message.Data);
                if (transaction != null && !_blockChainService.PendingTransactions.Contains(transaction))
                {
                    _blockChainService.AddTransactionToMempool(transaction);
                    Console.WriteLine($"Transaction received from {ctx.RemoteEndpoint} and added to mempool.");
                    Console.WriteLine("[Gossip] Пересилаю транзакцію іншим вузлам...");
                    _p2pClient.BroadcastTransactionAsync(transaction).GetAwaiter().GetResult();
                }
                return null;
            }
            return base.Handle(request);
        }
    }
}
