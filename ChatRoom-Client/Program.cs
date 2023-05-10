using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class WebSocketClient
{
    static async Task Main()
    {
        Console.Write("Ingrese la IP del servidor: ");
        string serverIp = Console.ReadLine();

        IPAddress ipAddress;
        if (!IPAddress.TryParse(serverIp, out ipAddress))
        {
            Console.WriteLine("La IP ingresada no es válida.");
            return;
        }

        Console.Write("Ingresa el puerto del servidor: ");
        int serverPort;
        while (!int.TryParse(Console.ReadLine(), out serverPort))
        {
            Console.WriteLine("El puerto ingresado no es válido. Inténtalo nuevamente.");
            Console.Write("Ingresa el puerto del servidor: ");
        }

        string serverUrl = $"ws://{serverIp}:{serverPort}/";
        using (ClientWebSocket clientWebSocket = new ClientWebSocket())
        {
            try
            {
                await clientWebSocket.ConnectAsync(new Uri(serverUrl), CancellationToken.None);
                Console.WriteLine("Conexión establecida con el servidor WebSocket.");

                await Task.WhenAll(ReceiveMessages(clientWebSocket), SendMessages(clientWebSocket));

                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cierre normal", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Se produjo una excepción durante la conexión:");
                Console.WriteLine(ex.ToString());
            }
        }
    }

    static async Task ReceiveMessages(ClientWebSocket clientWebSocket)
    {
        byte[] buffer = new byte[1024];

        while (clientWebSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Mensaje recibido del servidor: {message}");
            }
        }
    }

    static async Task SendMessages(ClientWebSocket clientWebSocket)
    {
        while (clientWebSocket.State == WebSocketState.Open)
        {
            string message = Console.ReadLine();
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await clientWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);           
        }
    }
}




