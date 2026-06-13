using BlockChain.Chain;
using System;
using System.Threading.Tasks;

namespace BlockChain.Services.P2P.Handlers
{
    public class RequestChainHandler : MessageHandlerBase
    {
        private readonly P2PClient _p2pClient;
        private readonly BlockChainService _blockChainService;

        public RequestChainHandler(P2PClient p2pClient, BlockChainService blockChainService)
        {
            _p2pClient = p2pClient;
            _blockChainService = blockChainService;
        }

        public override object Handle(object request)
        {
            var ctx = request as MessageContext;
            if (ctx?.Message.Type == "REQUEST_CHAIN")
            {
                Console.WriteLine($"Chain request received from {ctx.RemoteEndpoint}. Broadcasting current chain...");
                _p2pClient.BroadcastChainAsync(_blockChainService.Chain).GetAwaiter().GetResult();
                return null;
            }
            return base.Handle(request);
        }
    }
}
