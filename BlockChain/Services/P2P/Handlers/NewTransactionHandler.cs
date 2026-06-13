using BlockChain.Chain;
using BlockChain.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockChain.Services.P2P.Handlers
{
    public class NewTransactionHandler : MessageHandlerBase
    {
        private readonly BlockChainService _blockChainService;
        private readonly P2PClient _p2pClient;
        private static readonly HashSet<Guid> _seenTransactionIds = new HashSet<Guid>();

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
                if (transaction == null) return null;

                // Dedup by ID — prevents gossip echo loops
                lock (_seenTransactionIds)
                {
                    if (!_seenTransactionIds.Add(transaction.Id))
                    {
                        Console.WriteLine($"Transaction {transaction.Id} already processed — ignoring echo.");
                        return null;
                    }
                }

                // Only broadcast if this is a genuinely NEW transaction (not locally created)
                if (!_blockChainService.PendingTransactions.Any(t => t.Id == transaction.Id))
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
