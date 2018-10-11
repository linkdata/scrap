using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rap
{
    public class Server
    {
        public Server()
        {
        }

        public Task Listen()
        {
            return Listen(CancellationToken.None);
        }
        public Task Listen(CancellationToken cancelToken)
        {
            return Listen(cancelToken, IPAddress.Any, 10111);
        }

        public Task Listen(ushort port)
        {
            return Listen(CancellationToken.None, IPAddress.Any, port);
        }

        public Task Listen(string address, ushort port)
        {
            return Listen(CancellationToken.None, address, port);
        }

        public async Task Listen(CancellationToken cancelToken, string address, ushort port)
        {
            IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync(address).ConfigureAwait(false);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            await Listen(cancelToken, ipAddress, port).ConfigureAwait(false);
        }

        public Task Listen(CancellationToken cancelToken, IPAddress ipAddress, ushort port)
        {
            return Listen(cancelToken, new IPEndPoint(ipAddress, port));
        }

        public async Task Listen(CancellationToken cancelToken, IPEndPoint localEndPoint)
        {
            var listener = new TcpListener(localEndPoint);
            listener.Start();
            Console.WriteLine($"listening on {localEndPoint}");
            using (cancelToken.Register(listener.Stop))
            {
                try
                {
                    var muxerTasks = new List<Task>();
                    while (!cancelToken.IsCancellationRequested)
                    {
                        var tcpClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                        if (tcpClient != null)
                        {
                            muxerTasks.Add((new Muxer()).Run(cancelToken, tcpClient));
                        }
                    }
                    await Task.WhenAll(muxerTasks.ToArray()).ConfigureAwait(false);
                }
                catch (InvalidOperationException)
                {
                    if (!cancelToken.IsCancellationRequested)
                        throw;
                }
            }
        }
    }
}
