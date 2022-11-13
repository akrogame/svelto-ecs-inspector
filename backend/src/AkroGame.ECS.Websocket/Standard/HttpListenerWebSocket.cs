using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace AkroGame.ECS.Websocket.Standard
{
    public class HttpListenerWebSocket : IWebSocket
    {
        private readonly CancellationTokenSource tokenSource;
        private readonly CancellationToken cancellationToken;
        private readonly HttpListener httpListener;
        private readonly ConcurrentDictionary<int, WebSocket> connections;

        private int clientId = 1;

        public event Action<Envelope<int, ArraySegment<byte>>>? OnData;
        public event Action<int>? OnClose;

        public HttpListenerWebSocket(ushort port, string bindAddr = "localhost")
        {
            this.connections = new ConcurrentDictionary<int, WebSocket>();
            this.tokenSource = new CancellationTokenSource();
            this.cancellationToken = tokenSource.Token;

            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://{bindAddr}:{port}/");
            httpListener.Start();

            Task t = new Task(() => ConnectionWorker().ConfigureAwait(false), cancellationToken);
            t.Start();
        }

        public void Stop()
        {
            if (httpListener.IsListening)
            {
                tokenSource.Cancel();
                httpListener.Stop();
                httpListener.Close();
                tokenSource.Dispose();
            }
        }

        private async Task ConnectionWorker()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await httpListener
                    .GetContextAsync()
                    .ConfigureAwait(false);
                if (context.Request.IsWebSocketRequest)
                {
                    HttpListenerWebSocketContext webSocketContext =
                        await context.AcceptWebSocketAsync(null);

                    Interlocked.Increment(ref clientId);
                    connections.TryAdd(clientId, webSocketContext.WebSocket);

                    var t = new Task(
                        () => WebSocketWorker(webSocketContext, clientId).ConfigureAwait(false)
                    );
                    t.Start();
                }
            }
        }

        private async Task WebSocketWorker(HttpListenerWebSocketContext ctx, int id)
        {
            var socket = ctx.WebSocket;
            try
            {
                byte[] buffer = new byte[4096];
                while (
                    socket.State == WebSocketState.Open
                    && !cancellationToken.IsCancellationRequested
                )
                {
                    WebSocketReceiveResult receiveResult = await socket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken
                    );
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "",
                            cancellationToken
                        );
                    }
                    else
                    {
                        if (receiveResult.MessageType != WebSocketMessageType.Binary)
                            continue;
                        if (!receiveResult.EndOfMessage)
                            break;
                        OnData?.Invoke(
                            new Envelope<int, ArraySegment<byte>>(
                                id,
                                new ArraySegment<byte>(buffer, 0, receiveResult.Count)
                            )
                        );
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
            finally
            {
                OnClose?.Invoke(id);
                connections.Remove(clientId, out var _);
                socket?.Dispose();
            }
        }

        public void Send(int connectionId, ArraySegment<byte> source)
        {
            if (!connections.TryGetValue(connectionId, out var ws))
                return;

            if (ws.State != WebSocketState.Open)
                return;

            ws.SendAsync(source, WebSocketMessageType.Binary, true, cancellationToken)
                .Wait(1000, cancellationToken);
        }
    }
}
