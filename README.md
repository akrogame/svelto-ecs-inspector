# Svelto ECS Inspector

A Web based inspector for Svelto ECS library to visualize groups, entities and engines.

## Getting started

Packages:

You must include this in all of your assemblies where you have Engines defined:
https://www.nuget.org/packages/AkroGame.ECS.Analyzer
And this contains the inspector serving code itself:
https://www.nuget.org/packages/AkroGame.ECS.Websocket

You're going to need a websocket SERVER implementation for Unity.
Recommended: https://github.com/James-Frowen/SimpleWebTransport
With the following wrapper to tie it to the Inspector:

```cs
using AkroGame.ECS.Websocket;
using System;
using JamesFrowen.SimpleWeb;

public class WebSocketWrapper: IWebSocket
{
    private readonly SimpleWebServer server;
    public WebSocketWrapper()
    {
        var tcpConfig = new TcpConfig(true, 5000, 5000);
        server = new SimpleWebServer(5000, tcpConfig, 32000, 5000, default);
        // listen for events
        server.onDisconnect += (id) => { OnClose?.Invoke(id); };
        server.onData += (id, data) => { OnData?.Invoke(new Envelope<int, ArraySegment<byte>>(id, data)); };

        // start server listening on port 9300
        server.Start(9300);
    }

    public event Action<Envelope<int, ArraySegment<byte>>> OnData;
    public event Action<int> OnClose;

    public void Send(int connectionId, ArraySegment<byte> source)
    {
        server.SendOne(connectionId, source);
    }

    /// <summary>
    /// Call this from Unity Main Thread!
    /// </summary>
    public void Update()
    {
        server.ProcessMessageQueue();
    }
}
```

If you are using the above WebSocket implementation you must call the Update from Unity Main Thread.

Next create the inspector service

```cs
IWebSocket ws = new WebSocketWrapper();
InspectorService inspector = InspectorService(ws, enginesRoot);
```

You **MUST** call `inspector.Update(deltaTime);` note: deltaTime is a TimeSpan! from your main loop (so Unity main thread / any step engine / whatever you use to tick your engines with)

Open the UI and enjoy: https://akrogame.github.io/svelto-ecs-inspector/
note: the UI uses port 9300 so if you changed the port you must edit the port in the top left.

### Developing UI:

Please make sure you have `yarn` and `node v17` installed on your machine.

Run `yarn install` in the `/inspector` directory

### `yarn start`

Runs the app in the development mode.\
Open [http://localhost:3000/svelto-ecs-inspector] to view it in the browser.

The page will reload if you make edits.\
You will also see any lint errors in the console.
