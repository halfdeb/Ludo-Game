using LudoGame.Api.Hubs;
using LudoGame.Api.Services;
using LudoGame.Core.Engine;

var builder = WebApplication.CreateBuilder(args);

// ---- Services (modular: swap any of these independently) ----
builder.Services.AddSignalR();
builder.Services.AddSingleton<IGameEngine, GameEngine>();
builder.Services.AddSingleton<IRoomManager, InMemoryRoomManager>();

// The frontend is served from this same process's wwwroot folder (see below),
// so it's same-origin with the hub and needs no CORS policy at all. If you
// ever split the frontend out to its own host again, that's when a CORS
// policy needs to come back - it's deliberately not here otherwise, since
// a permissive CORS policy is attack surface you don't need for a same-origin app.

var app = builder.Build();

// Serves wwwroot/index.html at "/" and everything else in wwwroot as static files.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

app.MapHub<GameHub>("/gamehub");

app.Run();
