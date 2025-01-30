using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class ImageProcessWebSocketHandler
{
    public async Task Handle(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (message.StartsWith("process-image"))
                {
                    string taskId = message.Split(':')[1];
                    bool success = await PollFlaskTaskStatus(webSocket, taskId);
                    string statusMessage = success
                        ? $"{{\"taskId\": \"{taskId}\", \"status\": \"done\"}}"
                        : $"{{\"taskId\": \"{taskId}\", \"status\": \"failed\"}}";

                    var statusBytes = Encoding.UTF8.GetBytes(statusMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(statusBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnected", CancellationToken.None);
            }
        }
    }

    private async Task<bool> PollFlaskTaskStatus(WebSocket webSocket, string taskId)
    {
        using (HttpClient client = new HttpClient())
        {
            string flaskUrl = $"http://127.0.0.1:8000/status/{taskId}";

            for (int i = 0; i < 1200; i++)
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(flaskUrl);
                    if (!response.IsSuccessStatusCode) return false;

                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                    {
                        string status = doc.RootElement.GetProperty("status").GetString() ?? string.Empty;
                        if (status == "done") return true;
                        if (status == "failed") return false;
                        if (status == "canceled") return false;
                    }
                }
                catch
                {
                    return false;
                }

                await Task.Delay(1000);
            }

            return false;
        }
    }
}