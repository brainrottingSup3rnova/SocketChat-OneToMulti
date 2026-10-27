namespace SocketChat.Core;

using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

public class Server
{
    private const string DEFAULT_HOST_NAME = "localhost";
    private const int BUFFER_SIZE = 1024;
    private const int MAX_CONNECTIONS = 10;
    private List<Socket> clientConnected;
    private int clientCount = 0;
    private List<byte[]> bufferList;
    private Socket listener; 

    public int Port { get; init; }
    public string HostName { get; init; }
    public List<bool> ClientStatus;

    public Server(int port, string hostName)
    {
        this.Port = port;
        this.HostName = hostName ?? DEFAULT_HOST_NAME;
        ClearAllBuffers();
    }

    private void ClearBuffer(int clientIndex)
    {
        bufferList[clientIndex] = new byte[BUFFER_SIZE];
    }

    private void ClearAllBuffers()
    {
        for(int i = 0; i < MAX_CONNECTIONS; i++)
        {
            bufferList[i] = new byte[BUFFER_SIZE];
        }
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
        clientConnected[clientCount] = listener.Accept();
        clientCount++;
    }

    public bool TrySendMessage(string msg, int clientIndex)
    {
        if (!ClientStatus[clientIndex])
        {
            return false;
        }

        try
        {
            byte[] raw = Encoding.UTF8.GetBytes(msg);
            int sent = clientConnected[clientIndex].Send(raw);
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

    public void Close(int cliendIndex)
    {
        try
        {
            if (clientConnected[cliendIndex] is not null && ClientStatus[cliendIndex].Connected)
            {
                clientConnected[cliendIndex].Shutdown(SocketShutdown.Both);
                clientConnected[cliendIndex].Close();
            }
        }
        catch { }

        listener?.Close();
    }
}