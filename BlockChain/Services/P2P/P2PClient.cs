using BlockChain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BlockChain.Services.P2P
{
    public class P2PClient
    {
        public readonly List<string> _peers = new List<string>(); // This will hold the connected peers, using their address as the key
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
                Console.WriteLine($"Successfully connected to {peerAddress}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to {peerAddress}: {ex.Message}");
            }
        }

        public async Task BroadcastTransactionAsync(Transaction transaction)
        {
            var jsonTransaction = System.Text.Json.JsonSerializer.Serialize(transaction);

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
                        await writer.WriteLineAsync(jsonTransaction);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting transaction: {ex.Message}");
            }
        }
    }
}
