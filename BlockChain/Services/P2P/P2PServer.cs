using BlockChain.HashingService;
using BlockChain.Model;
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
        private readonly StorageService _storageService;

        public P2PServer(BlockChainService blockChainService, P2PClient p2pClient, StorageService storageService)
        {
            _blockChainService = blockChainService;
            _p2pClient = p2pClient;
            _storageService = storageService;
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

                // set a timeout so handshake-only connections don't block forever
                client.ReceiveTimeout = 3000;

                var jsonLine = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(jsonLine))
                {
                    // handshake-only connection, no transaction data
                    return;
                }

                var message = System.Text.Json.JsonSerializer.Deserialize<NetworkMessage>(jsonLine);

                if (message == null) return;

                switch (message.Type)
                {
                    case "HELLO":
                        // Peer is telling us the port IT listens on. Combine with the IP
                        // we see on the socket and register it, so WE can push back to it.
                        if (int.TryParse(message.Data, out int advertisedPort))
                        {
                            var ip = remoteEndpoint.Split(':')[0];
                            var peerAddr = $"{ip}:{advertisedPort}";
                            if (!_p2pClient._peers.Contains(peerAddr))
                            {
                                _p2pClient._peers.Add(peerAddr);
                                Console.WriteLine($"[P2P] Registered back-connection to {peerAddr}");
                            }
                        }
                        break;

                    case "NEW_TRANSACTION":
                        var transaction = System.Text.Json.JsonSerializer.Deserialize<Model.Transaction>(message.Data);
                        if (transaction != null && !_blockChainService.PendingTransactions.Contains(transaction))
                        {
                            _blockChainService.AddTransactionToMempool(transaction);
                            Console.WriteLine($"Transaction received from {remoteEndpoint} and added to mempool.");
                        }
                        break;

                    case "REQUEST_CHAIN":
                        Console.WriteLine($"Chain request received from {remoteEndpoint}. Broadcasting current chain...");
                        await _p2pClient.BroadcastChainAsync(_blockChainService.Chain);
                        break;

                    case "NEW_CHAIN":
                        var newChain = System.Text.Json.JsonSerializer.Deserialize<List<Block>>(message.Data);
                        if (newChain != null)
                        {
                            // ── Forensic audit before accepting chain ─────────────────
                            var audit = _blockChainService.RunFullAudit(newChain);
                            if (!audit.IsChainValid)
                            {
                                // Tampered chain detected — generate evidence and REFUSE it.
                                var attackOrigin = _blockChainService.FindAttackOrigin(audit, newChain);
                                var forensicReport = _blockChainService.GenerateForensicReport(audit, attackOrigin);
                                _storageService.GenerateNetworkPassport(
                                    _blockChainService.StudentId,
                                    _blockChainService.Chain.First().Hash,
                                    newChain, audit, attackOrigin);
                                Console.WriteLine(forensicReport);
                                Console.WriteLine($"[CONSENSUS] REJECTED chain from {remoteEndpoint}: " +
                                                  $"{audit.CompromisedBlockIndexes.Count} compromised block(s).");
                                break; // never replace our chain with a compromised one
                            }

                            // ── Audit passed — apply consensus rules (longest / most work) ──
                            int before = _blockChainService.Chain.Count;
                            _blockChainService.ReplaceChain(newChain);
                            int after = _blockChainService.Chain.Count;
                            if (after != before)
                            {
                                Console.WriteLine($"[CONSENSUS] Accepted chain from {remoteEndpoint}. Length {before} → {after}.");
                                // Gossip flood: forward to OUR peers so the block reaches the whole
                                // network even through nodes the miner never connected to directly.
                                // Terminates naturally: a peer that already has it won't re-accept,
                                // so it won't forward again.
                                await _p2pClient.BroadcastChainAsync(_blockChainService.Chain);
                            }
                            else
                                Console.WriteLine($"[CONSENSUS] Chain from {remoteEndpoint} did not win (not longer / less work). Kept ours ({before}).");
                        }
                        break;

                    default:
                        Console.WriteLine($"Unknown message type '{message.Type}' from {remoteEndpoint}.");
                        break;
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
