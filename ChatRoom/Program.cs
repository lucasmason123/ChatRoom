using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class WebSocketServer
{
    private const int BufferSize = 1024;
    private static readonly IPAddress ServerIpAddress = IPAddress.Parse("127.0.0.1");
    private const int ServerPort = 2050;

    private static ConcurrentDictionary<string, WebSocket> connectedClients = new ConcurrentDictionary<string, WebSocket>();

    static async Task Main()
    {
        try
        {
            using (HttpListener httpListener = new HttpListener())
            {
                httpListener.Prefixes.Add($"http://{ServerIpAddress}:{ServerPort}/");
                httpListener.Start();
                Console.WriteLine("Servidor en ejecución...");

                while (true)
                {
                    HttpListenerContext context = await httpListener.GetContextAsync();
                    if (context.Request.IsWebSocketRequest)
                    {
                        await ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Se produjo una excepción no controlada:");
            Console.WriteLine(ex.ToString());
        }
    }

    static async Task ProcessWebSocketRequest(HttpListenerContext context)
    {

        HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);

        WebSocket webSocket = webSocketContext.WebSocket;
        string clientId = Guid.NewGuid().ToString();

        connectedClients.TryAdd(clientId, webSocket);

        await SendInitialMessage(webSocket);

        byte[] buffer = new byte[BufferSize];

        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Mensaje recibido del cliente {clientId}: {message}");

                    await SendMessageToAllClients(message);

                    if (message.ToLower() == "chao")
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Se produjo una excepción durante la recepción/envío de mensajes:");
                Console.WriteLine(ex.ToString());
                break;
            }
        }

        try
        {
            webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cierre normal", CancellationToken.None);

            connectedClients.TryRemove(clientId, out _);
            Console.WriteLine($"Cliente {clientId} desconectado.");
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        try
        {
            // Esperar un nuevo cliente
            await WaitForNewClient(context);
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
       
    }

    static async Task WaitForNewClient(HttpListenerContext context)
    {
        while (true)
        {
            try
            {
                if (context.Request.IsWebSocketRequest)
                {
                    await ProcessWebSocketRequest(context);
                    break;
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Se produjo una excepción al esperar un nuevo cliente:");
                Console.WriteLine(ex.ToString());
                break;
            }
        }
    }

    static async Task SendInitialMessage(WebSocket webSocket)
    {
        string initialMessage = "Conexión establecida con el servidor WebSocket.";
        byte[] initialMessageBuffer = Encoding.UTF8.GetBytes(initialMessage + Environment.NewLine);
        await webSocket.SendAsync(new ArraySegment<byte>(initialMessageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    static async Task SendMessageToAllClients(string message)
    {
        byte[] messageBuffer = Encoding.UTF8.GetBytes(message);

        foreach (var clientWebSocket in connectedClients.Values)
        {
            if (clientWebSocket.State == WebSocketState.Open)
            {
                string messageToCliente = Console.ReadLine()?.Trim();
                byte[] buffer = Encoding.UTF8.GetBytes(messageToCliente);

                await clientWebSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}






