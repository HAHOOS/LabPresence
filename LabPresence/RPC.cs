using System;
using System.Collections.Generic;
using System.Net.Http;

using DiscordRPC;

using LabPresence.Config;
using LabPresence.Helper;

using UnityEngine;

namespace LabPresence
{
    /// <summary>
    /// Class responsible for managing the Rich Presence
    /// </summary>
    public static class RPC
    {
        /// <summary>
        /// The current Rich Presence configuration
        /// </summary>
        public static RPCConfig CurrentConfig { get; private set; }

        /// <summary>
        /// The timestamp for the Rich Presence
        /// </summary>
        public static Timestamp Timestamp { get; private set; }

        /// <summary>
        /// Set the timestamp for the Rich Presence
        /// </summary>
        /// <param name="timestamp">The <see cref="LabPresence.Timestamp"/> to set</param>
        /// <param name="autoUpdate">Should the timestamp be automatically updated</param>
        public static void SetTimestamp(Timestamp timestamp, bool autoUpdate = false)
        {
            Timestamp = timestamp;

            if (Core.Client.CurrentPresence != null && autoUpdate)
                Core.Client.Update(x => x.Timestamps = Timestamp.ToRPC());
        }

        /// <summary>
        /// Set the timestamp for the Rich Presence
        /// </summary>
        /// <param name="start">The start of the timestamp in unix milliseconds</param>
        /// <param name="end">The end of the timestamp in unix milliseconds</param>
        /// <param name="autoUpdate">Should the timestamp be automatically updated</param>
        public static void SetTimestamp(ulong? start, ulong? end, bool autoUpdate = false)
            => SetTimestamp(new(start, end), autoUpdate);

        /// <summary>
        /// Set the timestamp to start from now
        /// </summary>
        /// <param name="autoUpdate">Should the timestamp be automatically updated</param>
        public static void SetTimestampStartToNow(bool autoUpdate = false)
            => SetTimestamp(LabPresence.Timestamp.Now, autoUpdate);

        /// <summary>
        /// Set the timestamp to display the current time, like "16:50:00"
        /// </summary>
        /// <param name="autoUpdate">Should the timestamp be automatically updated</param>
        public static void SetTimestampToCurrentTime(bool autoUpdate = false)
            => SetTimestamp(LabPresence.Timestamp.CurrentTime, autoUpdate);

        /// <summary>
        /// Set the current Rich Presence
        /// </summary>
        /// <param name="details"><inheritdoc cref="RPCConfig.Details"/></param>
        /// <param name="state"><inheritdoc cref="RPCConfig.State"/></param>
        /// <param name="type"><inheritdoc cref="ActivityType"/></param>
        /// <param name="party"><inheritdoc cref="Party"/></param>
        /// <param name="secrets"><inheritdoc cref="Secrets"/></param>
        /// <param name="smallImageKey"><inheritdoc cref="Assets.SmallImageKey"/></param>
        /// <param name="smallImageTitle"><inheritdoc cref="Assets.SmallImageText"/></param>
        private static void SetRPC(string details, string state, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, string smallImageKey = null, string smallImageTitle = null)
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

        /// <summary>
        /// Set the current Rich Presence from a provided <see cref="RPCConfig"/>
        /// </summary>
        /// <param name="config">The <see cref="RPCConfig"/> to get the state and details from</param>
        /// <param name="type"><inheritdoc cref="ActivityType"/></param>
        /// <param name="party"><inheritdoc cref="Party"/></param>
        /// <param name="secrets"><inheritdoc cref="Secrets"/></param>
        /// <param name="smallImageKey"><inheritdoc cref="Assets.SmallImageKey"/></param>
        /// <param name="smallImageTitle"><inheritdoc cref="Assets.SmallImageText"/></param>
        public static void SetRPC(RPCConfig config, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, string smallImageKey = null, string smallImageTitle = null)
        {
            if (!config.Use)
                return;

            CurrentConfig = config;
            SetRPC(config.Details, config.State, type, party, secrets, smallImageKey, smallImageTitle);
        }

        private static readonly Dictionary<ulong, Texture2D> _avatarCache = [];

        /// <summary>
        /// Get a <see cref="Texture2D"/> of a provided <see cref="User"/>'s avatar
        /// </summary>
        /// <param name="user">The <see cref="User"/> to get the avatar of</param>
        /// <param name="cache">Should the avatar be written be cached and on the next request use the cache</param>
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

    /// <summary>
    /// Timestamp for the <see cref="RPC"/>
    /// </summary>
    /// <param name="start"><inheritdoc cref="Start"/></param>
    /// <param name="end"><inheritdoc cref="End"/></param>
    public class Timestamp(ulong? start, ulong? end)
    {
        /// <summary>
        /// Start of the timestamp in unix milliseconds
        /// </summary>
        public ulong? Start { get; private set; } = start;

        /// <summary>
        /// End of the timestamp in unix milliseconds
        /// </summary>
        public ulong? End { get; private set; } = end;

        /// <summary>
        /// Initialize a <see cref="Timestamp"/> with the current time as start and the offset as the end
        /// </summary>
        /// <param name="days">The days offset from the current time</param>
        /// <param name="hours">The hours offset from the current time</param>
        /// <param name="minutes">The minutes offset from the current time</param>
        /// <param name="seconds">The seconds offset from the current time</param>
        /// <param name="milliseconds">The milliseconds offset from the current time</param>
        public static Timestamp FromNow(int days = 0, int hours = 0, int minutes = 0, int seconds = 0, int milliseconds = 0)
        {
            var now = DateTimeOffset.Now;
            var end = now.Add(new TimeSpan(days, hours, minutes, seconds, milliseconds));
            return new((ulong)now.ToUnixTimeMilliseconds(), (ulong)end.ToUnixTimeMilliseconds());
        }

        /// <summary>
        /// Initialize a <see cref="Timestamp"/> with the current time as start and the offset as the end
        /// </summary>
        /// <param name="span">The offset from the current time</param>
        public static Timestamp FromNow(TimeSpan span)
        {
            var now = DateTimeOffset.Now;
            var end = now.Add(span);
            return new((ulong)now.ToUnixTimeMilliseconds(), (ulong)end.ToUnixTimeMilliseconds());
        }

        /// <summary>
        /// <see cref="Timestamp"/> with the start as the current time
        /// </summary>
        public static Timestamp Now
        {
            get => new((ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(), null);
        }

        /// <summary>
        /// <see cref="Timestamp"/> that displays the current time
        /// </summary>
        public static Timestamp CurrentTime
        {
            get
            {
                var now = DateTimeOffset.Now;
                var timeSpan = new TimeSpan(0, now.Hour, now.Minute, now.Second, now.Millisecond);
                var substracted = now.Subtract(timeSpan);
                return new((ulong)substracted.ToUnixTimeMilliseconds(), null);
            }
        }

        /// <summary>
        /// Converts the <see cref="Timestamp"/> to <see cref="Timestamps"/>
        /// </summary>
        /// <returns>A <see cref="Timestamps"/> with values of the <see cref="Timestamp"/></returns>
        public Timestamps ToRPC()
            => new() { EndUnixMilliseconds = End, StartUnixMilliseconds = Start };
    }
}