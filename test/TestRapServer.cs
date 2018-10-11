public class TestRapServer
{
    public static int Main(string[] args)
    {
        Rap.AsynchronousSocketListener.StartListening();
        return 0;
    }
}
