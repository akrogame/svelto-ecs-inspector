using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AkroGame.ECS.Websocket
{
    public class Listener
    {
        public async Task Listen()
        {
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost/");
            httpListener.Start();

            while (true)
            {
                HttpListenerContext context = await httpListener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    HttpListenerWebSocketContext webSocketContext =
                        await context.AcceptWebSocketAsync(null);
                    WebSocket webSocket = webSocketContext.WebSocket;
                    var helloWorld = Encoding.UTF8.GetBytes("Hello World");
                    var ct = new CancellationToken();
                    while (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.SendAsync(helloWorld, WebSocketMessageType.Text, true, ct);
                        await Task.Delay(2000);
                    }
                }
            }
        }
    }
}
