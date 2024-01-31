using System.Diagnostics;
using System.Text;
using by.illusion21.Services.Common.Types;
using by.illusion21.Utilities.Common;
using Newtonsoft.Json;

namespace by.illusion21.Communication;

public class KookMessage {
    private static readonly HttpClient HttpClient = new();

    public async Task<MessageStatus> SendMessageAsync(string message) {
        Debug.Assert(PalWorldServerMg.Config != null, "PalWorldServerMg.Config != null");
        if (!PalWorldServerMg.Config.ValueOf<bool>("Kook", "KookEnable")) {
            Log.WriteLine("Kook pushing feature is disabled by configuration");
            return MessageStatus.Undefined;
        }

        var requestUrl = PalWorldServerMg.Config.ValueOf<string>("Kook", "BaseUrl");
        var requestPath = PalWorldServerMg.Config.ValueOf<string>("Kook", "RequestPath");
        var authorization = PalWorldServerMg.Config.ValueOf<string>("Kook", "Authorization");
        var type = PalWorldServerMg.Config.ValueOf<string>("Kook", "PostType");
        var targetId = PalWorldServerMg.Config.ValueOf<string>("Kook", "TargetID");
        var customContent = PalWorldServerMg.Config.ValueOf<string>("Kook", "CustomContent");
        var useSsl = PalWorldServerMg.Config.ValueOf<bool>("Kook", "UseSSL");
        var requestBody = new {
            type,
            target_id = targetId,
            content = message + customContent
        };
        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        var requestUri = new Uri(useSsl ? $"https://{requestUrl}/{requestPath}" : $"http://{requestUrl}/{requestPath}");
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) {
            Headers = { { "Authorization", authorization } },
            Content = content
        };
        var response = await HttpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonConvert.DeserializeObject<KookResponseData>(responseContent);

        return responseData != null && response.IsSuccessStatusCode && responseData.Code == 0 ? MessageStatus.Successful : MessageStatus.Failed;
    }
}

public class KookResponseData {
    [JsonProperty("code")] public int Code { get; set; }

    [JsonProperty("message")] public string? Message { get; set; }

    [JsonProperty("data")] public KookData? Data { get; set; }
}

public class KookData {
    [JsonProperty("msg_id")] public string? MsgId { get; set; }

    [JsonProperty("msg_timestamp")] public long MsgTimestamp { get; set; }

    [JsonProperty("nonce")] public string? Nonce { get; set; }

    [JsonProperty("not_permissions_mention")]
    public string[]? NotPermissionsMention { get; set; }
}