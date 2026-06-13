using System;

namespace BlockChain.Services.P2P.Handlers
{
    public class UnknownMessageHandler : MessageHandlerBase
    {
        public override object Handle(object request)
        {
            var ctx = request as MessageContext;
            if (ctx != null)
            {
                Console.WriteLine($"Unknown message type '{ctx.Message.Type}' from {ctx.RemoteEndpoint}.");
            }
            return null;
        }
    }
}
