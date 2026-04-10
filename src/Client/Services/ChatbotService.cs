using System.Net.Http.Json;

namespace Solodoc.Client.Services;

public class ChatbotService(ApiHttpClient api)
{
    public async Task<string?> SendMessageAsync(string message, List<ChatMsg>? history = null)
    {
        var request = new { message, history = history?.Select(h => new { h.Role, h.Content }).ToList() };
        var response = await api.PostAsJsonAsync("api/chatbot", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ChatReply>();
            return result?.Reply;
        }
        return null;
    }

    private record ChatReply(string Reply);
}

public record ChatMsg(string Role, string Content);
