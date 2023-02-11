using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ryujinx.HLE.HOS.Services.Account.Acc.Types
{
    internal struct ProfilesJson
    {
        [JsonPropertyName("profiles")]
        public List<UserProfileJson> Profiles { get; set; }
        [JsonPropertyName("last_opened")]
        public string LastOpened { get; set; }
    }
}