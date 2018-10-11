using System;
using System.Threading;

public class TestRapServer
{
    public static int Main(string[] args)
    {
        var server = new Rap.Server();
        server.Listen().Wait();
        return 0;
    }
}
