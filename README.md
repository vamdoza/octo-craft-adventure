# Calafia Rush

```text
   ______      __        _____          ____             __
  / ____/___ _/ /___ _  / __(_)___ _   / __ \__  _______/ /_
 / /   / __ `/ / __ `/ / /_/ / __ `/  / /_/ / / / / ___/ __ \
/ /___/ /_/ / / /_/ / / __/ / /_/ /  / _, _/ /_/ (__  ) / / /
\____/\__,_/_/\__,_/ /_/ /_/\__,_/  /_/ |_|\__,_/____/_/ /_/

      PICK UP RIDERS  *  BEAT THE CLOCK  *  RUN THE ROUTE
```

**Calafia Rush** is a fast, arcade-style driving game inspired by Tijuana's
colorful short transit buses. Switch lanes, manage your speed, collect waiting
passengers, avoid traffic, watch the lights, and keep enough cash on hand to
get through police checkpoints before the route timer expires.

The current version is a playable prototype built for the Unity Editor and
WebGL. Its world and gameplay objects are assembled at runtime from lightweight
3D primitives, making the project quick to build and easy to iterate on.

## Gameplay

Each run starts with 75 seconds on the clock and three lanes of traffic ahead.
The objective is to complete as many laps and carry as many passengers as
possible before time runs out.

- Slow down near waiting passengers to pick them up.
- Carry up to 12 passengers at a time.
- Earn score and fare money from pickups and roadside bonuses.
- Avoid traffic collisions, which cost score and time.
- Stop for red lights or receive a time penalty.
- Pay `$10` at police checkpoints to continue immediately, or wait for release.
- Complete a lap to earn bonus score and 12 additional seconds.

## Controls

| Action | Keyboard | On-screen control |
| --- | --- | --- |
| Move left | `A` or Left Arrow | **LEFT** |
| Move right | `D` or Right Arrow | **RIGHT** |
| Accelerate | `W` or Up Arrow | **GAS** |
| Brake | `S` or Down Arrow | Not currently exposed |
| Pay at checkpoint | `B` | **PAY $10** |
| Start the route | `Space` or `Enter` | **START THE ROUTE** |

## Technology

- **Unity 2022.3.62f3 LTS** for the game engine and editor.
- **C#** for gameplay, world generation, input, scoring, and UI.
- **Unity WebGL / WebAssembly** for browser builds.
- **Unity Addressables 1.22.3** for the original asset-loading experiment and
  future downloadable content support.
- **Unity UI / IMGUI** for the prototype HUD and touch-friendly controls.
- **Python 3 HTTP server** for serving local WebGL builds.
- **Shell and PowerShell scripts** for local hosting and deployment automation.

The primary prototype implementation is in
`Assets/Scripts/CalafiaRushGame.cs`. It bootstraps after scene loading, removes
the original demo objects, and creates the road, bus, traffic, passengers,
signals, checkpoints, camera, lighting, HUD, and game loop.

## Requirements

- Unity Hub
- Unity Editor `2022.3.62f3`
- WebGL Build Support module, when building for browsers
- Python 3, when serving a WebGL build locally
- Git, when cloning the repository

Install the exact Unity version through Unity Hub to avoid automatic project
upgrades and unnecessary serialized-file changes.

## Get The Project

```bash
git clone git@github.com:vamdoza/octo-craft-adventure.git
cd octo-craft-adventure
```

## Run In Unity

1. Open Unity Hub.
2. Select **Add project from disk** and choose this repository.
3. Open it using Unity `2022.3.62f3`.
4. Open `Assets/Scenes/SampleScene.unity`.
5. Select the **Game** tab and press **Play**.

The runtime bootstrap replaces the original Addressables sample scene with
Calafia Rush when Play mode begins.

## Build For WebGL

1. Install **WebGL Build Support** for Unity `2022.3.62f3` in Unity Hub.
2. Open the project and wait for package import and script compilation.
3. Open **File > Build Settings**.
4. Select **WebGL** and click **Switch Platform** if necessary.
5. Confirm `Assets/Scenes/SampleScene.unity` is enabled in **Scenes In Build**.
6. Click **Build** and select `<repository>/Build` as the output directory.

The `Build/` directory is intentionally ignored by Git because it contains
generated browser artifacts.

## Run A WebGL Build Locally

WebGL builds must be served over HTTP. Opening `Build/index.html` directly can
fail because browsers restrict local WebAssembly and asset requests.

On Linux, macOS, WSL, or Git Bash:

```bash
bash ./.scripts/run-local-build.sh
```

Use a custom port by passing it as the first argument:

```bash
bash ./.scripts/run-local-build.sh 8080
```

On Windows PowerShell:

```powershell
.\.scripts\run-local-build.ps1
```

Then open `http://localhost:8989/`, or the custom port you selected.

## Testing

The prototype has been validated with the following workflow:

- Unity batch-mode C# compilation using Unity `2022.3.62f3`.
- A complete WebGL build with the WebGL Build Support module.
- Local HTTP smoke tests confirming successful responses for:
  - `index.html`
  - the Unity WebGL loader
  - the WebAssembly binary
  - the Unity data file
- Manual browser playtesting of startup, controls, pickups, obstacles, HUD,
  scoring, checkpoints, laps, and game-over flow.

There are currently no automated gameplay tests. Future work should add Unity
Test Framework coverage for scoring, lane bounds, pickup rules, time penalties,
and checkpoint payments.

## Project Structure

```text
Assets/
  AddressableAssetsData/  Addressables groups and build configuration
  Editor/                 Experimental Addressables import tooling
  Materials/              Original sample materials
  Prefabs/                Original addressable cube sample
  Resources/              Calafia Rush title artwork
  Scenes/                 Unity scene used to launch the game
  Scripts/                Runtime gameplay and legacy sample scripts
  Textures/               Original sample textures
Packages/                 Unity Package Manager dependencies
ProjectSettings/          Unity editor and player configuration
.scripts/                 Local WebGL server helpers
PostBuildScripts/         GitHub Pages deployment helpers
```

## Prototype Scope

This is an early gameplay prototype, not a finished simulation. Traffic and
road events are procedurally spawned, the city is assembled from primitive
geometry, and vehicle movement uses arcade rules rather than full physics.

Useful next milestones include audio, route maps, passenger drop-off stops,
multiple Calafia vehicles, controller support, improved mobile input, saved
high scores, difficulty progression, and dedicated 3D environment art.

## Deployment

The repository includes post-build shell and PowerShell scripts for copying a
Unity Cloud Build WebGL output into a separate GitHub Pages repository. Those
scripts expect deployment credentials and build metadata through environment
variables; review them before using them in CI.

## License

No license file is currently included. Treat the source code and generated
artwork as all rights reserved until the project owner adds an explicit license.
