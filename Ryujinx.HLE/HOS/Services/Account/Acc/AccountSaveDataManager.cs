using Ryujinx.Common.Configuration;
using Ryujinx.Common.Utilities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Ryujinx.HLE.HOS.Services.Account.Acc
{
    class AccountSaveDataManager
    {
        private readonly string _accountsJsonPath = Path.Join(AppDataManager.BaseDirPath, "system", "Accounts.json");

        private struct AccountsJson
        {
            [JsonPropertyName("accounts")]
            public List<UserProfileJson> Profiles { get; set; }
            [JsonPropertyName("last_opened")]
            public string LastOpened { get; set; }
        }

        private struct UserProfileJson
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

        public UserId LastOpened { get; set; }

        public AccountSaveDataManager(ConcurrentDictionary<string, UserProfile> profiles)
        {
            // TODO: Uses 0x8000000000000010 system savedata instead of a JSON file if needed.

            if (File.Exists(_accountsJsonPath))
            {
                AccountsJson accountJson = JsonHelper.DeserializeFromFile<AccountsJson>(_accountsJsonPath);

                foreach (var profile in accountJson.Profiles)
                {
                    UserProfile addedProfile = new UserProfile(new UserId(profile.UserId), profile.Name, profile.Image, profile.LastModifiedTimestamp);

                    profiles.AddOrUpdate(profile.UserId, addedProfile, (key, old) => addedProfile);
                }

                LastOpened = new UserId(accountJson.LastOpened);
            }
            else
            {
                LastOpened = AccountManager.DefaultUserId;
            }
        }

        public void Save(ConcurrentDictionary<string, UserProfile> profiles)
        {
            AccountsJson accountsJson = new AccountsJson()
            {
                Profiles   = new List<UserProfileJson>(),
                LastOpened = LastOpened.ToString()
            };

            foreach (var profile in profiles)
            {
                accountsJson.Profiles.Add(new UserProfileJson()
                {
                    UserId                = profile.Value.UserId.ToString(),
                    Name                  = profile.Value.Name,
                    AccountState          = profile.Value.AccountState,
                    OnlinePlayState       = profile.Value.OnlinePlayState,
                    LastModifiedTimestamp = profile.Value.LastModifiedTimestamp,
                    Image                 = profile.Value.Image,
                });
            }

            File.WriteAllText(_accountsJsonPath, JsonHelper.Serialize(accountsJson, true));
        }
    }
}