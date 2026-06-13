using BlockChain.Chain;
using BlockChain.Model;

namespace BlockChain.Services.P2P.Handlers
{
    public class RequestHeaderHandler : MessageHandlerBase
    {
        private readonly BlockChainService _blockChainService;

        public RequestHeaderHandler(BlockChainService blockChainService)
        {
            _blockChainService = blockChainService;
        }

        public override object Handle(object request)
        {
            var ctx = request as MessageContext;
            if (ctx?.Message.Type == "REQUEST_HEADER")
            {
                if (!int.TryParse(ctx.Message.Data, out int blockIndex))
                {
                    Console.WriteLine($"Invalid REQUEST_HEADER — not a valid block index: {ctx.Message.Data}");
                    return null;
                }

                var block = _blockChainService.Chain.FirstOrDefault(b => b.Index == blockIndex);
                if (block == null)
                {
                    Console.WriteLine($"Block #{blockIndex} not found (requested by {ctx.RemoteEndpoint})");
                    return null;
                }

                var headerData = $"{block.Index}|{block.MerkleRoot}|{block.Hash}";
                var response = new NetworkMessage("HEADER_RESULT", headerData);
                var responseJson = System.Text.Json.JsonSerializer.Serialize(response);

                ctx.ResponseWriter?.WriteLineAsync(responseJson).GetAwaiter().GetResult();
                Console.WriteLine($"Header sent for block #{blockIndex} to {ctx.RemoteEndpoint}");
                return null;
            }
            return base.Handle(request);
        }
    }
}
