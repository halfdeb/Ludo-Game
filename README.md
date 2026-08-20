# The Ludo Times — a 4-player real-time Ludo game

Everything lives in **one folder** and runs as **one process**: the
ASP.NET Core backend serves the frontend itself (as a static file), so
there's no separate URL to configure and no CORS to fight with.

**This has actually been built and run in this environment** — I compiled
it, started it, and drove it through a full game (create room → join →
start → roll → move → capture → reach home) using a real SignalR client,
not just written and hoped. See "What I verified" below.

```
LudoGame/
  LudoGame.sln
  NuGet.config              <- no external packages needed; forces offline restore
  src/
    LudoGame.Core/           pure game rules, zero web dependencies, unit-testable on its own
      Models/                 Token, Player, GameRoom, enums
      Engine/                 BoardConstants (board geometry) + GameEngine (all the rules)
    LudoGame.Api/             the one thing you run
      wwwroot/index.html      <- the entire frontend, served by this same process
      Hubs/GameHub.cs         SignalR hub: CreateRoom, JoinRoom, StartGame, RollDice, MoveToken
      Services/RoomManager.cs in-memory room registry
      Dtos/                   what gets sent down to clients
      Program.cs
```

## Run it — one command

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
installed (on Ubuntu/Debian: `sudo apt-get install dotnet-sdk-8.0`; on
Mac: `brew install dotnet-sdk`; on Windows: the installer from the link
above).

```bash
cd LudoGame/src/LudoGame.Api
dotnet run
```

Then open **http://localhost:5080** in a browser. That's it — the same
process is serving the game board and handling the real-time connection.
Open it in 2–4 tabs (or send that URL to friends on your network / through
a tunnel, see below) to actually play with multiple people.

## About "exposed code in the frontend"

Every browser-based frontend's HTML/CSS/JS is visible to anyone who opens
dev tools — that's true of every website, not a flaw specific to this one,
and `wwwroot/index.html` doesn't contain any secret, key, or credential, so
there's nothing sensitive to expose. What *was* a real issue in the first
draft: the backend had a CORS policy that accepted requests from **any**
origin with credentials enabled — that's the kind of thing that matters for
security. Since the frontend is now served by the same process as the API,
they're same-origin, so that permissive CORS policy is gone entirely (see
`Program.cs`) rather than just hidden behind a config flag.

## Playing with people who aren't on your machine

Opening `http://localhost:5080` only reaches people on your machine. For
real friends on the internet:

**Quick and free — tunnel it:**
```bash
dotnet run                # starts on port 5080
ngrok http 5080            # in a second terminal
```
Send everyone the `https://xxxx.ngrok-free.app` URL ngrok prints. Because
frontend and backend are the same origin, that single link is all anyone
needs — no separate config, no editing the HTML.

**Properly hosted:** push this folder to Azure App Service, Render,
Railway, or Fly.io (any host that runs `dotnet publish` for you, pointed at
`src/LudoGame.Api`). You get one public URL that serves both the board and
the live connection.

## What I verified (not just claimed)

In this build session I actually:
1. Installed the .NET 8 SDK and ran `dotnet build` — compiles clean, 0 errors.
2. Started the server with plain `dotnet run` and confirmed `/` serves the
   board and `/gamehub` accepts SignalR connections.
3. Wrote a small Node script using the real `@microsoft/signalr` client
   (the same library the browser uses) that simulated two players end to
   end: create room → join → start game → roll dice → move tokens for 40
   turns. It exercised base-exit-on-6, extra turns, and a token reaching
   home (`steps: 57`) — all over a live connection, no mocking.

That's why clicking the buttons in a real browser will work the same way —
the exact same hub methods and message shapes were driven end to end.

## The rules that are implemented

- Standard 4-color, 4-token-each Ludo on the classic 52-square ring + 6-square
  home stretch per color.
- Roll a 6 to leave base; rolling a 6, capturing, or reaching home grants an
  extra roll.
- Three 6s in a row forfeits the turn.
- Landing on an opponent (outside the 8 safe/star squares) sends it back to base.
- A token needs an exact roll to enter/finish its home column — overshooting
  is not a legal move.
- First player to get all 4 tokens home wins; disconnected players are
  auto-skipped so the game doesn't stall.
- 2–4 players supported (you don't need all 4 to start).

## Extending it (it's modular on purpose)

- **New win conditions / rule variants**: only touch `GameEngine.cs`.
- **Persistence / horizontal scaling**: swap `InMemoryRoomManager` for a
  Redis-backed `IRoomManager`; `GameHub` and `GameEngine` don't need to know.
- **Reconnect-with-a-new-tab support**: currently a disconnect mid-game
  marks the seat "away" and auto-skips their turns; name+room-code based
  re-authentication in `GameHub.JoinRoom` is the natural next step.
- **Theme**: all pastel-Shrek colors are CSS variables at the top of
  `wwwroot/index.html` (`--ogre`, `--swamp`, `--fiona`, `--donkey`).
