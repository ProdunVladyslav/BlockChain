using BlockChain.Model;
using System;

namespace BlockChain.Services.P2P.Handlers
{
    public class HelloHandler : MessageHandlerBase
    {
        private readonly P2PClient _p2pClient;

        public HelloHandler(P2PClient p2pClient)
        {
            _p2pClient = p2pClient;
        }

        public override object Handle(object request)
        {
            var ctx = request as MessageContext;
            if (ctx?.Message.Type == "HELLO")
            {
                if (int.TryParse(ctx.Message.Data, out int advertisedPort))
                {
                    var ip = ctx.RemoteEndpoint.Split(':')[0];
                    var peerAddr = $"{ip}:{advertisedPort}";
                    if (!_p2pClient._peers.Contains(peerAddr))
                    {
                        _p2pClient._peers.Add(peerAddr);
                        Console.WriteLine($"[P2P] Registered back-connection to {peerAddr}");
                    }
                }
                return null;   // handled, stop the chain
            }
            else
            {
                return base.Handle(request);  // "not my food" → pass to next
            }
        }
    }
}