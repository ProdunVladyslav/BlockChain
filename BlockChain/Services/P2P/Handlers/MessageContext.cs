using BlockChain.Model;

namespace BlockChain.Services.P2P.Handlers
{
    public class MessageContext
    {
        public NetworkMessage Message { get; set; }
        public string RemoteEndpoint { get; set; }
    }
}
