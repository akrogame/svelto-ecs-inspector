using System;

namespace AkroGame.ECS.Websocket
{
    public interface IWebSocket
    {
        /// <summary>
        /// Callback for when data is received on the websocket for a certain connection
        /// 
        /// Users should assume that the payload in the envelope is only safe to use during the callback
        ///  and storing it could lead to undesirable effects
        /// </summary>
        event Action<Envelope<int, ArraySegment<byte>>> OnData;
        event Action<int> OnClose;

        public void Send(int connectionId, ArraySegment<byte> source);
    }
}
