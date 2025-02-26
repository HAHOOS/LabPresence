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

        public static Timestamp Timestamp { get; private set; }

        public static void SetTimestamp(Timestamp timestamp, bool autoUpdate = false)
        {
            Timestamp = timestamp;

            if (Core.Client.CurrentPresence != null && autoUpdate)
                Core.Client.Update(x => x.Timestamps = Timestamp.ToRPC());
        }

        public static void SetTimestamp(ulong? start, ulong? end, bool autoUpdate = false)
            => SetTimestamp(new(start, end), autoUpdate);

        public static void SetTimestampStartToNow(bool autoUpdate = false)
            => SetTimestamp(LabPresence.Timestamp.Now(), autoUpdate);

        public static void SetTimestampToCurrentTime(bool autoUpdate = false)
            => SetTimestamp(LabPresence.Timestamp.CurrentTime(), autoUpdate);

        public static void SetRPC(string details, string state, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, string smallImageKey = null, string smallImageTitle = null)
        {
            if (Core.Client?.IsInitialized != true)
                return;

            Core.Client.SetPresence(new DiscordRPC.RichPresence()
            {
                Details = Core.RemoveUnityRichText(details?.ApplyPlaceholders()),
                State = Core.RemoveUnityRichText(state?.ApplyPlaceholders()),
                Timestamps = Fusion.GetGamemodeOverrideTime()?.ToRPC() ?? Timestamp?.ToRPC(),
                Type = type,
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

        public static void SetRPC(RPCConfig config, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, string smallImageKey = null, string smallImageTitle = null)
        {
            if (!config.Use)
                return;

            CurrentConfig = config;
            SetRPC(config.Details, config.State, type, party, secrets, smallImageKey, smallImageTitle);
        }

        private static readonly Dictionary<ulong, Texture2D> _avatarCache = [];

        public static Texture2D GetAvatar(User user, bool cache = false)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            if (cache && _avatarCache.ContainsKey(user.ID) && _avatarCache[user.ID] != null)
                return _avatarCache[user.ID];

            Texture2D texture = null;
            var avatar = user.GetAvatarURL(User.AvatarFormat.PNG, User.AvatarSize.x512);
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

    public class Timestamp(ulong? start, ulong? end)
    {
        public ulong? Start { get; private set; } = start;

        public ulong? End { get; private set; } = end;

        public static Timestamp FromNow(int days = 0, int hours = 0, int minutes = 0, int seconds = 0, int milliseconds = 0)
        {
            var now = DateTimeOffset.Now;
            var end = now.Add(new TimeSpan(days, hours, minutes, seconds, milliseconds));
            return new((ulong)now.ToUnixTimeMilliseconds(), (ulong)end.ToUnixTimeMilliseconds());
        }

        public static Timestamp FromNow(TimeSpan span)
        {
            var now = DateTimeOffset.Now;
            var end = now.Add(span);
            return new((ulong)now.ToUnixTimeMilliseconds(), (ulong)end.ToUnixTimeMilliseconds());
        }

        public static Timestamp Now()
            => new((ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(), null);

        public static Timestamp CurrentTime()
        {
            var now = DateTimeOffset.Now;
            var timeSpan = new TimeSpan(0, now.Hour, now.Minute, now.Second, now.Millisecond);
            var substracted = now.Subtract(timeSpan);
            return new((ulong)substracted.ToUnixTimeMilliseconds(), null);
        }

        public Timestamps ToRPC()
            => new() { EndUnixMilliseconds = End, StartUnixMilliseconds = Start };
    }
}