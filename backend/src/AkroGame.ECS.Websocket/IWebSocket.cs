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

        /// <summary>
        /// Should be called when the remote connection is closed
        /// </summary>
        event Action<int> OnClose;

        /// <summary>
        /// Sends the byte array segment to the client specified by the id
        /// </summary>
        public void Send(int connectionId, ArraySegment<byte> source);
    }
}
