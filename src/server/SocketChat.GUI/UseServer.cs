using SocketChat.Core;

namespace SocketChat.GUI;

public class UseServer
{
    private static void Fatal(string message)
    {
        Console.Error.WriteLine("Could not connect to ${host}:${port}");
        Environment.Exit(1);
    }

    public static void Main(string[] args)
    {
        var host = args[0] ?? "localhost";
        var port = int.Parse(args[1] ?? "11000");
        var server = new Server(port, host); 
        server.Start();

        server.StartListening(message => Console.WriteLine("[Client] " + message));

        bool keepGoing = true;
        Console.WriteLine("Type here your message and press ENTER to send it!");
        while (keepGoing && server.IsClientConnected)
        {
            var message = Console.ReadLine();
            if (!string.IsNullOrEmpty(message))
            {
                bool sent = server.TrySendMessage(message);
                if (sent)
                {
                    Console.WriteLine("[Server] " + message);
                }
            }
            keepGoing = message != "exit";
        }
    }
}
