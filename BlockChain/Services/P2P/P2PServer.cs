using BlockChain.HashingService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain.Services.P2P
{
    public class P2PServer
    {
        private readonly BlockChainService _blockChainService;
        private readonly P2PClient _p2pClient;

        public P2PServer(BlockChainService blockChainService, P2PClient p2pClient)
        {
            _blockChainService = blockChainService;
            _p2pClient = p2pClient;
        }

        public void Start(int port)
        {
            var listener = new TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"P2P Server started on port {port}. Waiting for connections...");

            Task.Run(async () =>
            {
                while (true)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("New peer connected.");
                    // Handle the client connection in a separate task
                    _ = HandleClientAsync(client);
                }
            });
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var remoteEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            Console.WriteLine($"New peer connected from {remoteEndpoint}");

            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);

                // set a timeout so handshake-only connections don't block forever
                client.ReceiveTimeout = 3000;

                var jsonLine = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(jsonLine))
                {
                    // handshake-only connection, no transaction data
                    return;
                }

                var transaction = System.Text.Json.JsonSerializer.Deserialize<Model.Transaction>(jsonLine);

                if (transaction != null && !_blockChainService.PendingTransactions.Contains(transaction))
                {
                    _blockChainService.AddTransactionToMempool(transaction);
                    Console.WriteLine($"Transaction received from {remoteEndpoint} and added to mempool.");
                    Console.WriteLine($"[Gossip] Broadcasting transaction to other peers...");
                    _p2pClient.BroadcastTransactionAsync(transaction).Wait();
                }
            }
            catch (IOException)
            {
                // timeout or disconnect — normal for handshake connections
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling peer connection: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine($"Peer {remoteEndpoint} disconnected.");
            }
        }
    }
}
