# Svelto ECS Inspector

A Web based inspector for Svelto ECS library to visualize groups, entities and engines.

## Getting started

### Before you start

Please make sure you have `yarn` and `node v17` installed on your machine.

Run `yarn install` in the `/inspector` directory

### `yarn start`

Runs the app in the development mode.\
Open [http://localhost:3000] to view it in the browser.

The page will reload if you make edits.\
You will also see any lint errors in the console.

### Backend

Packages:

```
<PackageReference Include="AkroGame.ECS.Analyzer" Version="1.0.0" />
<PackageReference Include="AkroGame.ECS.Inspector" Version="1.0.0" />

<!-- You'll Probably need these too -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="3.9.0" PrivateAssets="all" />
<PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.0.0" PrivateAssets="all" />
```

`AkroGame.ECS.Inspector.EngineNames.QueryInvocations` probably won't exist in VS / VSCode, you should restart VS or restart OmniSharp after the first load for analyzers to kick in.

#### TODO: Unity analyzer setup??

```
using AkroGame.ECS.Inspector;

var inspectorService = new InspectorService(
    enginesRoot,
    AkroGame.ECS.Inspector.EngineNames.QueryInvocations
        .Select(x => new AkroGame.ECS.Inspector.QueryInvocation(x.ClassName, x.Components))
        .ToList()
)

// your log provider implementation, I use log4net, so mines looks like
ILoggerProvider logProvider = null;
// ILoggerProvider logProvider = new Log4NetProvider(log4NetConfigPath);
var inspectorApi = new InspectorApi(
    args, // empty array of string, this is console's args
    new InspectorConfig(3001, "localhost"),
    inspectorService,
    logProvider
)

// Start anywhere, async api ( fire and forget )
inspectorApi.Start();

// Somewhere IN YOUR MAIN THREAD, around entity submission:
inspectorService.UpdateFromMainThread();
```
