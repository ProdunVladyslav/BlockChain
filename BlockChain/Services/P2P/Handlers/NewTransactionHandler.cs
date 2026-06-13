using BlockChain.Chain;
using BlockChain.Model;
using System;

namespace BlockChain.Services.P2P.Handlers
{
    public class NewTransactionHandler : MessageHandlerBase
    {
        private readonly BlockChainService _blockChainService;

        public NewTransactionHandler(BlockChainService blockChainService)
        {
            _blockChainService = blockChainService;
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
                }
                return null;
            }
            return base.Handle(request);
        }
    }
}
