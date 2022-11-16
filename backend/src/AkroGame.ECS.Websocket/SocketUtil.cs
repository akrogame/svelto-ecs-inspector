using System;
using System.Text;
using Newtonsoft.Json;

namespace AkroGame.ECS.Websocket
{
    public static class SocketUtil
    {
        public static ArraySegment<byte> Serialize<T>(T t) =>
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(t));
    }
}
