namespace SocketChat.Core;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Server
{
    private const string DEFAULT_HOST_NAME = "localhost";
    private const int BUFFER_SIZE = 1024;
    private const int MAX_CONNECTIONS = 10;

    //private Socket clientHandler;
    private List<Socket> clientHandlerList; //lista di tutti i client connessi
    private int clientCount = 0;
    private byte[] buffer;
    private Socket listener; //da togliere 

    public int Port { get; init; }
    public string HostName { get; init; }
    //public bool IsClientConnected => clientHandler is not null && clientHandler.Connected;
    public List<bool> clientStatusList; //lista di tutti gli status dei vari client

    public Server(int port, string hostName)
    {
        this.Port = port;
        this.HostName = hostName ?? DEFAULT_HOST_NAME;
        ClearBuffer();
    }

    private void ClearBuffer()
    {
        this.buffer = new byte[BUFFER_SIZE];
    }

    // Configura l'ascolto e attende l'arrivo di un client
    public void Start()
    {
        var hostIP = Dns.GetHostEntry(HostName).AddressList[0];
        var localEndpoint = new IPEndPoint(hostIP, Port);

        listener = new Socket(hostIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(localEndpoint);
        listener.Listen(MAX_CONNECTIONS);

        // Blocca finché un client non si connette
        clientHandler = listener.Accept();
    }

    public bool TrySendMessage(string msg)
    {
        if (!IsClientConnected)
        {
            return false;
        }

        try
        {
            byte[] raw = Encoding.UTF8.GetBytes(msg);
            int sent = clientHandler.Send(raw);
            return sent == raw.Length;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    // Ascolto in background speculare a quello del Client
    public void StartListening(Action<string> onMessageReceived)
    {
        Task.Run(() =>
        {
            ClearBuffer();

            while (IsClientConnected)
            {
                try
                {
                    int receivedBytes = clientHandler.Receive(this.buffer);
                    if (receivedBytes == 0)
                    {
                        // Il client si è disconnesso
                        return;
                    }

                    string decodedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    onMessageReceived(decodedMessage);
                }
                catch (SocketException)
                {
                    // Connessione interrotta o socket chiuso
                    return;
                }
            }
        });
    }

    public void Close()
    {
        try
        {
            if (clientHandler is not null && clientHandler.Connected)
            {
                clientHandler.Shutdown(SocketShutdown.Both);
                clientHandler.Close();
            }
        }
        catch { }

        listener?.Close();
    }
}