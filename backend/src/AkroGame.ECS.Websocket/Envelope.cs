namespace AkroGame.ECS.Websocket
{
    public struct Envelope<K, T>
    {
        public Envelope(K id, T payload)
        {
            Id = id;
            Payload = payload;
        }

        public K Id { get; }
        public T Payload { get; }
    }
}
