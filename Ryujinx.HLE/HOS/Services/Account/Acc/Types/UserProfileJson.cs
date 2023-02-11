using System.Text.Json.Serialization;

namespace Ryujinx.HLE.HOS.Services.Account.Acc.Types
{
    internal struct UserProfileJson
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("account_state")]
        public AccountState AccountState { get; set; }
        [JsonPropertyName("online_play_state")]
        public AccountState OnlinePlayState { get; set; }
        [JsonPropertyName("last_modified_timestamp")]
        public long LastModifiedTimestamp { get; set; }
        [JsonPropertyName("image")]
        public byte[] Image { get; set; }
    }
}