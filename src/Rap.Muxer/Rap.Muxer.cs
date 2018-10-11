using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Rap
{
    public class Muxer
    {
        public Muxer()
        {
        }

        public async Task Run(CancellationToken cancelToken, TcpClient tcpClient)
        {
            try
            {
                using (cancelToken.Register(tcpClient.Close))
                using (tcpClient)
                using (var stream = tcpClient.GetStream())
                {
                    using (var sr = new StreamReader(stream))
                    using (var sw = new StreamWriter(stream))
                    {
                        await sw.WriteLineAsync("Hi. This is x2 TCP/IP easy-to-use server").ConfigureAwait(false);
                        await sw.FlushAsync().ConfigureAwait(false);
                        var data = default(string);
                        while (!((data = await sr.ReadLineAsync().ConfigureAwait(false)).Equals("exit", StringComparison.OrdinalIgnoreCase)))
                        {
                            await sw.WriteLineAsync(data).ConfigureAwait(false);
                            await sw.FlushAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (System.IO.IOException)
            {
                if (!cancelToken.IsCancellationRequested)
                    throw;
            }
        }
    }
}