
using BlockChain;
using BlockChain.Chain;
using BlockChain.Model;
using BlockChain.Services.P2P.Handlers;
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
        public static bool FakeMerkleMode = false; // HW demo: send random MerkleRoot
        public IHandler ChainHead { get; set; }
        private readonly BlockChainService _blockChainService;
        private readonly P2PClient _p2pClient;
        private readonly StorageService _storageService;
        private readonly HashingService _hashingService;

        public P2PServer(BlockChainService blockChainService, P2PClient p2pClient,
                     StorageService storageService, HashingService hashingService)
        {
            _blockChainService = blockChainService;
            _p2pClient = p2pClient;
            _storageService = storageService;
            _hashingService = hashingService;
        }

        public void Start(int port)
        {
            // Tell the client half which port we listen on, so it can advertise
            // that address to peers during the handshake (for back-connections).
            _p2pClient.ListeningPort = port;

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
                using var writer = new StreamWriter(stream) { AutoFlush = true };
                client.ReceiveTimeout = 3000;

                var jsonLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(jsonLine)) return;

                var message = System.Text.Json.JsonSerializer.Deserialize<NetworkMessage>(jsonLine);
                if (message == null) return;

                var ctx = new MessageContext
                {
                    Message = message,
                    RemoteEndpoint = remoteEndpoint,
                    ResponseWriter = writer
                };
                ChainHead.Handle(ctx);
            }
            catch (IOException) { }  // timeout — normal for handshake connections
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
