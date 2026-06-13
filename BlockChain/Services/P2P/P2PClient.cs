using BlockChain.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain.Services.P2P
{
    public class P2PClient
    {
        public readonly List<string> _peers = new List<string>();
        public readonly List<string> _peersToRemove = new List<string>();
        private const string PeersFilePath = "peers.json";


        public int ListeningPort { get; set; }

        public void SavePeersToFile()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_peers);
                File.WriteAllText(PeersFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving peers: {ex.Message}");
            }
        }

        public List<string> LoadPeersFromFile()
        {
            try
            {
                if (!File.Exists(PeersFilePath)) return new List<string>();
                var json = File.ReadAllText(PeersFilePath);
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading peers: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task ConnectAsync(string peerAddress)
        {
            peerAddress = peerAddress?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(peerAddress))
            {
                Console.WriteLine("Address cannot be empty.");
                return;
            }

            if (_peers.Contains(peerAddress))
            {
                Console.WriteLine($"Already connected to {peerAddress}");
                return;
            }

            var parts = peerAddress.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
            {
                Console.WriteLine("Invalid address format. Use ip:port (e.g. 127.0.0.1:5001)");
                return;
            }

            try
            {
                using var testClient = new TcpClient();
                await testClient.ConnectAsync(parts[0], port);
                _peers.Add(peerAddress);

                var hello = new NetworkMessage("HELLO", ListeningPort.ToString());
                var helloJson = System.Text.Json.JsonSerializer.Serialize(hello);
                await using var stream = testClient.GetStream();
                await using var writer = new StreamWriter(stream) { AutoFlush = true };
                await writer.WriteLineAsync(helloJson);

                Console.WriteLine($"Successfully connected to {peerAddress} (advertised our port {ListeningPort})");
                SavePeersToFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to {peerAddress}: {ex.Message}");
            }
        }

        public async Task BroadcastTransactionAsync(Transaction transaction)
        {
            var jsonTransaction = System.Text.Json.JsonSerializer.Serialize(transaction);
            var message = new NetworkMessage("NEW_TRANSACTION", jsonTransaction);
            var jsonMessage = System.Text.Json.JsonSerializer.Serialize(message);

            try
            {
                foreach (var peer in _peers)
                {
                    var parts = peer.Split(':');
                    if (parts.Length > 0)
                    {
                        var ip = parts[0];
                        var port = int.Parse(parts[1]);

                        using var client = new TcpClient();
                        await client.ConnectAsync(ip, port);
                        await using var stream = client.GetStream();
                        await using var writer = new StreamWriter(stream) { AutoFlush = true };
                        await writer.WriteLineAsync(jsonMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting transaction: {ex.Message}");
            }
        }

        public async Task RequestChainAsync(string ip, int port)
        {
            var message = new NetworkMessage("REQUEST_CHAIN", "");
            var jsonMessage = System.Text.Json.JsonSerializer.Serialize(message);

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(ip, port);
                await using var stream = client.GetStream();
                await using var writer = new StreamWriter(stream) { AutoFlush = true };
                await writer.WriteLineAsync(jsonMessage);
                Console.WriteLine($"Chain requested from {ip}:{port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting chain from {ip}:{port}: {ex.Message}");
            }
        }

        public async Task<MerkleProof> RequestProofAsync(string ip, int port, Guid txId)
        {
            var message = new NetworkMessage("REQUEST_PROOF", txId.ToString());
            var jsonMessage = System.Text.Json.JsonSerializer.Serialize(message);

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(ip, port);
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                await writer.WriteLineAsync(jsonMessage);
                Console.WriteLine($"Proof requested for tx {txId} from {ip}:{port}");

                var responseLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(responseLine)) return null;

                var response = System.Text.Json.JsonSerializer.Deserialize<NetworkMessage>(responseLine);
                if (response == null || response.Type != "PROOF_RESULT") return null;

                var proof = System.Text.Json.JsonSerializer.Deserialize<MerkleProof>(response.Data);
                return proof;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting proof from {ip}:{port}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Requests a block header (MerkleRoot + BlockHash) from a peer for cross-verification.
        /// Returns null on failure. Response format: "Index|MerkleRoot|BlockHash"
        /// </summary>
        public async Task<string[]> RequestHeaderAsync(string ip, int port, int blockIndex)
        {
            var message = new NetworkMessage("REQUEST_HEADER", blockIndex.ToString());
            var jsonMessage = System.Text.Json.JsonSerializer.Serialize(message);

            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(ip, port);
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                await writer.WriteLineAsync(jsonMessage);
                Console.WriteLine($"Header requested for block #{blockIndex} from {ip}:{port}");

                var responseLine = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(responseLine)) return null;

                var response = System.Text.Json.JsonSerializer.Deserialize<NetworkMessage>(responseLine);
                if (response == null || response.Type != "HEADER_RESULT") return null;

                // Format: Index|MerkleRoot|BlockHash
                var parts = response.Data.Split('|');
                if (parts.Length < 3) return null;
                return parts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting header from {ip}:{port}: {ex.Message}");
                return null;
            }
        }

        public async Task BroadcastChainAsync(List<Block> chain)
        {
            var jsonChain = System.Text.Json.JsonSerializer.Serialize(chain);
            var message = new NetworkMessage("NEW_CHAIN", jsonChain);
            await SendMessage(message);
        }

        public async Task BroadcastBlockAsync(Block block)
        {
            var jsonBlock = System.Text.Json.JsonSerializer.Serialize(block);
            var message = new NetworkMessage("NEW_BLOCK", jsonBlock);
            
            await SendMessage(message);
        }

        private async Task SendMessage(NetworkMessage message)
        {
            var jsonMessage = System.Text.Json.JsonSerializer.Serialize(message);

            foreach (var peer in _peers)
            {
                try
                {
                    var parts = peer.Split(':');
                    if (parts.Length > 0)
                    {
                        var ip = parts[0];
                        var port = int.Parse(parts[1]);

                        using var client = new TcpClient();
                        await client.ConnectAsync(ip, port);
                        await using var stream = client.GetStream();
                        await using var writer = new StreamWriter(stream) { AutoFlush = true };
                        await writer.WriteLineAsync(jsonMessage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message to {peer}: {ex.Message}");
                    _peersToRemove.Add(peer);
                }
            }
            foreach (var peer in _peersToRemove)
            {
                _peers.Remove(peer);
                Console.WriteLine($"Removed peer {peer} due to connection issues.");
            }
        }
    }
}
