using System.Net.WebSockets;
using System.Collections.Generic;
using WebSocketsDummyProject;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register WebSocketConnectionManager as a singleton
builder.Services.AddSingleton<WebSocketConnectionManager>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseWebSockets(); // Make sure this is before app.UseStaticFiles() and app.UseRouting()

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var connectionManager = app.Services.GetRequiredService<WebSocketConnectionManager>();
            connectionManager.AddSocket(webSocket);
            await ChatHandler(context, webSocket, connectionManager);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static async Task ChatHandler(HttpContext context, WebSocket webSocket, WebSocketConnectionManager connectionManager)
{
    var buffer = new byte[1024 * 4];
    WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    while (!result.CloseStatus.HasValue)
    {
        var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
        foreach (var socket in connectionManager.ConnectedSockets)
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message), 0, message.Length),
                    result.MessageType, result.EndOfMessage, CancellationToken.None);
            }
        }
        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    }
    connectionManager.RemoveSocket(webSocket);
    await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
}