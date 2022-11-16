using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkroGame.ECS.Websocket
{
    public interface IInspectorDataStream
    {
        void UnSubscribe(int inspectorId);
        void PushAll(TimeSpan deltaTime, IWebSocket ws);
    }

    public abstract class InspectorDataStream<TContext> : IInspectorDataStream
    {
        protected readonly ConcurrentDictionary<int, TContext> inspectors;
        private readonly string key;
        private readonly TimeSpan sendInterval;
        private TimeSpan nextSendIn;

        protected InspectorDataStream(string key, TimeSpan sendInterval)
        {
            inspectors = new ConcurrentDictionary<int, TContext>();
            this.key = key;
            this.sendInterval = sendInterval;
            this.nextSendIn = sendInterval;
        }

        public void Subscribe(int inspectorId, TContext context)
        {
            inspectors.AddOrUpdate(inspectorId, context, (id, existing) => context);
        }

        public void UnSubscribe(int inspectorId)
        {
            inspectors.Remove(inspectorId, out var _);
        }

        protected Envelope<string, T> MakeEnvelope<T>(T payload) =>
            new Envelope<string, T>(key, payload);

        protected abstract ArraySegment<byte> FetchData(TContext context);

        public void PushAll(TimeSpan deltaTime, IWebSocket ws)
        {
            if (!inspectors.Any())
                return;

            nextSendIn -= deltaTime;
            if (nextSendIn > TimeSpan.Zero)
                return;

            nextSendIn += sendInterval;
            foreach (var inspector in inspectors)
            {
                var data = FetchData(inspector.Value);
                ws.Send(inspector.Key, data);
            }
        }
    }
}
