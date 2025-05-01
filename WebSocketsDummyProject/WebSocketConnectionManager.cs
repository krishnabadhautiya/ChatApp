using System.Net.WebSockets;
using System.Collections.Concurrent;

public class WebSocketConnectionManager
{
    private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

    public ICollection<WebSocket> ConnectedSockets => _sockets.Values;

    public string AddSocket(WebSocket socket)
    {
        string connId = Guid.NewGuid().ToString();
        _sockets.TryAdd(connId, socket);
        return connId;
    }

    public void RemoveSocket(WebSocket socket)
    {
        var pair = _sockets.FirstOrDefault(p => p.Value == socket);
        if (pair.Key != null)
        {
            _sockets.TryRemove(pair.Key, out _);
        }
    }
}