using System;
using System.Collections.Generic;
using System.Net.Http;

using DiscordRPC;

using LabPresence.Config;
using LabPresence.Helper;

using UnityEngine;

namespace LabPresence
{
    public static class RPC
    {
        public static RPCConfig CurrentConfig { get; private set; }

        public static Timestamps Timestamps { get; private set; }

        public static void SetTimestamp(ulong? start, ulong? end)
        {
            Timestamps ??= new Timestamps();
            Timestamps.StartUnixMilliseconds = start;
            Timestamps.EndUnixMilliseconds = end;

            if (Core.Client.CurrentPresence != null)
            {
                Core.Client.CurrentPresence.Timestamps = Timestamps;
                Core.Client.SetPresence(Core.Client.CurrentPresence);
            }
        }

        public static void SetStartToNow()
            => RPC.SetTimestamp((ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(), null);

        public static void SetRPC(string details, string state, Party party = null, Secrets secrets = null, string smallImageKey = null, string smallImageTitle = null)
        {
            if (Core.Client?.IsInitialized != true)
                return;

            Core.Client.SetPresence(new DiscordRPC.RichPresence()
            {
                Details = Core.RemoveUnityRichText(details?.ApplyPlaceholders()),
                State = Core.RemoveUnityRichText(state?.ApplyPlaceholders()),
                Timestamps = Timestamps,
                Type = DiscordRPC.ActivityType.Playing,
                Assets = new DiscordRPC.Assets()
                {
                    LargeImageKey = "icon",
                    LargeImageText = "BONELAB",
                    SmallImageKey = smallImageKey,
                    SmallImageText = smallImageTitle
                },
                Party = party,
                Secrets = secrets,
            });
        }

        public static void SetRPC(RPCConfig config, Party party = null, Secrets secrets = null, string smallImageKey = null, string smallImageTitle = null)
        {
            if (!config.Use)
                return;

            CurrentConfig = config;
            SetRPC(config.Details, config.State, party, secrets, smallImageKey, smallImageTitle);
        }

        private static readonly Dictionary<ulong, Texture2D> _avatarCache = [];

        public static Texture2D GetAvatar(User user, bool cache = false)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            if (cache && _avatarCache.ContainsKey(user.ID) && _avatarCache[user.ID] != null)
                return _avatarCache[user.ID];

            Texture2D texture = null;
            var avatar = user.GetAvatarURL(DiscordRPC.User.AvatarFormat.PNG, User.AvatarSize.x512);
            var client = new HttpClient();
            var task = client.GetAsync(avatar);
            task.Wait();
            if (task.IsCompletedSuccessfully)
            {
                var bytesTask = task.Result.Content.ReadAsByteArrayAsync();
                if (bytesTask.IsCompletedSuccessfully)
                {
                    texture = new Texture2D(2, 2);
                    texture.LoadImage(bytesTask.Result, false);
                    texture.name = $"{user.DisplayName} ({user.Username})";
                    texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
                }
            }

            if (cache && texture != null)
                _avatarCache[user.ID] = texture;

            return texture;
        }
    }
}