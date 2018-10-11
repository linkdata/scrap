using System;
using System.Threading;

public class TestRapServer
{
    public static int Main(string[] args)
    {
        var server = new Rap.Server();
        var tcs = new CancellationTokenSource();
        tcs.CancelAfter(10 * 1000);
        server.Listen(tcs.Token).Wait();
        return 0;
    }
}
