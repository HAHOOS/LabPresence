using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

using DiscordRPC;
using DiscordRPC.Exceptions;

using LabPresence.Config;
using LabPresence.Helper;

using UnityEngine;

namespace LabPresence
{
    /// <summary>
    /// Class responsible for managing the Rich Presence
    /// </summary>
    public static class RichPresenceManager
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
        /// The timestamp that will be used to override <see cref="Timestamp"/>, if <see langword="null"/> <see cref="Timestamp"/> will be used in the presence
        /// </summary>
        public static TimestampOverride OverrideTimestamp { get; private set; }

        /// <summary>
        /// Set the timestamp for the Rich Presence
        /// </summary>
        /// <param name="timestamp">The <see cref="LabPresence.Timestamp"/> to set</param>
        /// <param name="autoUpdate">Should the timestamp be automatically updated</param>
        public static void SetTimestamp(Timestamp timestamp, bool autoUpdate = false)
        {
            Timestamp = timestamp;

            if (autoUpdate)
                UpdateTimestamp();
        }

        /// <summary>
        /// Updates the timestamp on the Rich Presence to be up to date with current config
        /// </summary>
        public static void UpdateTimestamp()
        {
            if (Core.Client.CurrentPresence != null)
                Core.Client.Update(x => x.Timestamps = OverrideTimestamp != null ? OverrideTimestamp?.Timestamp?.ToRPC() : Timestamp.ToRPC());
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
        /// Set the <see cref="OverrideTimestamp"/>
        /// </summary>
        /// <param name="timestamp">The <see cref="LabPresence.Timestamp"/> to set</param>
        /// <param name="autoUpdate">Should the timestamp be automatically updated</param>
        public static void SetOverrideTimestamp(TimestampOverride timestamp, bool autoUpdate = false)
        {
            OverrideTimestamp = timestamp;
            if (autoUpdate) UpdateTimestamp();
        }

        /// <summary>
        /// Reset the <see cref="OverrideTimestamp"/>
        /// </summary>
        /// <param name="autoUpdate">Should the timestamp be automatically updated</param>
        public static void ResetOverrideTimestamp(bool autoUpdate = false)
        {
            OverrideTimestamp = null;
            if (autoUpdate) UpdateTimestamp();
        }

        /// <summary>
        /// Set the current Rich Presence
        /// </summary>
        /// <param name="details"><inheritdoc cref="RPCConfig.Details"/></param>
        /// <param name="state"><inheritdoc cref="RPCConfig.State"/></param>
        /// <param name="type"><inheritdoc cref="ActivityType"/></param>
        /// <param name="party"><inheritdoc cref="Party"/></param>
        /// <param name="secrets"><inheritdoc cref="Secrets"/></param>
        /// <param name="largeImage">The large image that appears as the main image in the Rich Presence. If <see langword="null"/>, will use BONELAB logo</param>
        /// <param name="smallImage">The small image that appears as the image in the down right corner in the Rich Presence. If <see langword="null"/>, it won't be displayed</param>
        private static void SetRichPresence(string details, string state, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, Asset largeImage = null, Asset smallImage = null)
        {
            if (Core.Client?.IsInitialized != true)
                throw new InvalidOperationException("The RPC client is not initialized!");

            largeImage ??= new("icon", "BONELAB");
            if (!RichPresence.ValidateString(Core.RemoveUnityRichText(details?.ApplyPlaceholders()), out string det, 128, Encoding.UTF8) ||
                !RichPresence.ValidateString(Core.RemoveUnityRichText(state?.ApplyPlaceholders()), out string stat, 128, Encoding.UTF8) ||
                !RichPresence.ValidateString(largeImage.ToolTip, out _, 128, Encoding.UTF8) ||
                !RichPresence.ValidateString(smallImage?.ToolTip, out _, 128, Encoding.UTF8))
            {
                throw new StringOutOfRangeException("State, Details and/or an asset tooltip is/are over 128 bytes which Rich Presence cannot handle, try to lower the amount of characters", 0, 128);
            }
            Core.Client.SetPresence(new DiscordRPC.RichPresence()
            {
                Details = det,
                State = stat,
                Timestamps = OverrideTimestamp?.Timestamp?.ToRPC() ?? Timestamp?.ToRPC(),
                Type = type,
                Assets = CreateAssets(largeImage, smallImage),
                Party = party,
                Secrets = secrets,
            });
        }

        private static Assets CreateAssets(Asset largeImage, Asset smallImage)
        {
            return new Assets()
            {
                LargeImageKey = largeImage?.Key,
                LargeImageText = largeImage?.ToolTip,
                SmallImageKey = smallImage?.Key,
                SmallImageText = smallImage?.ToolTip,
            };
        }

        /// <summary>
        /// Set the current Rich Presence without throwing exceptions
        /// </summary>
        /// <param name="details"><inheritdoc cref="RPCConfig.Details"/></param>
        /// <param name="state"><inheritdoc cref="RPCConfig.State"/></param>
        /// <param name="type"><inheritdoc cref="ActivityType"/></param>
        /// <param name="party"><inheritdoc cref="Party"/></param>
        /// <param name="secrets"><inheritdoc cref="Secrets"/></param>
        /// <param name="largeImage">The large image that appears as the main image in the Rich Presence. If <see langword="null"/>, will use BONELAB logo</param>
        /// <param name="smallImage">The small image that appears as the image in the down right corner in the Rich Presence. If <see langword="null"/>, it won't be displayed</param>
        /// <returns><see langword="true"/> if set rich presence successfully, otherwise <see langword="false"/></returns>
        private static bool TrySetRichPresence(string details, string state, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, Asset largeImage = null, Asset smallImage = null)
        {
            try
            {
                SetRichPresence(details, state, type, party, secrets, largeImage, smallImage);
                return true;
            }
            catch (StringOutOfRangeException)
            {
                Core.Logger.Error("State, Details and/or an asset tooltip is/are over 128 bytes which Rich Presence cannot handle, try to lower the amount of characters");
            }
            catch (InvalidOperationException)
            {
                Core.Logger.Error("The RPC client is not initialized!");
            }
            catch (Exception ex)
            {
                Core.Logger.Error($"An unexpected error has occurred while setting the rich presence, exception:\n{ex}");
            }
            return false;
        }

        /// <summary>
        /// Set the current Rich Presence from a provided <see cref="RPCConfig"/> without throwing exceptions
        /// </summary>
        /// <param name="config">The <see cref="RPCConfig"/> to get the state and details from</param>
        /// <param name="type"><inheritdoc cref="ActivityType"/></param>
        /// <param name="party"><inheritdoc cref="Party"/></param>
        /// <param name="secrets"><inheritdoc cref="Secrets"/></param>
        /// <param name="largeImage">The large image that appears as the main image in the Rich Presence. If <see langword="null"/>, will use BONELAB logo</param>
        /// <param name="smallImage">The small image that appears as the image in the down right corner in the Rich Presence. If <see langword="null"/>, it won't be displayed</param>
        /// <returns><see langword="true"/> if set rich presence successfully, otherwise <see langword="false"/>. Note that if config is set to not be used, <see langword="true"/> will be returned</returns>
        public static bool TrySetRichPresence(RPCConfig config, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, Asset largeImage = null, Asset smallImage = null)
        {
            if (!config.Use)
                return true;

            Core.Logger.Msg($"Update requested: {config.Details} / {config.State}");
            bool res = TrySetRichPresence(config.Details, config.State, type, party, secrets, largeImage, smallImage);
            if (res)
                CurrentConfig = config;
            return res;
        }

        /// <summary>
        /// Set the current Rich Presence from a provided <see cref="RPCConfig"/>
        /// </summary>
        /// <param name="config">The <see cref="RPCConfig"/> to get the state and details from</param>
        /// <param name="type"><inheritdoc cref="ActivityType"/></param>
        /// <param name="party"><inheritdoc cref="Party"/></param>
        /// <param name="secrets"><inheritdoc cref="Secrets"/></param>
        /// <param name="largeImage">The large image that appears as the main image in the Rich Presence. If <see langword="null"/>, will use BONELAB logo</param>
        /// <param name="smallImage">The small image that appears as the image in the down right corner in the Rich Presence. If <see langword="null"/>, it won't be displayed</param>
        public static void SetRichPresence(RPCConfig config, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, Asset largeImage = null, Asset smallImage = null)
        {
            if (!config.Use)
                return;

            SetRichPresence(config.Details, config.State, type, party, secrets, largeImage, smallImage);
            CurrentConfig = config;
        }

        private static readonly Dictionary<ulong, Texture2D> _avatarCache = [];

        /// <summary>
        /// Get a <see cref="Texture2D"/> of a provided <see cref="User"/>'s avatar
        /// </summary>
        /// <param name="user">The <see cref="User"/> to get the avatar of</param>
        /// <param name="size">The size of the image</param>
        /// <param name="cache">Should the avatar be written be cached and on the next request use the cache</param>
        public static Texture2D GetAvatar(User user, User.AvatarSize size = User.AvatarSize.x512, bool cache = false)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));

            if (cache && _avatarCache.ContainsKey(user.ID) && _avatarCache[user.ID] != null)
                return _avatarCache[user.ID];

            Texture2D texture = null;
            var avatar = user.GetAvatarURL(User.AvatarFormat.PNG, size);
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

        /// <summary>
        /// Encrypt a message by converting it to base64. This isn't like really good encryption but good enough for discord secrets
        /// </summary>
        /// <param name="secret">The secret to encrypt</param>
        public static string Encrypt(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                return string.Empty;
            // Not the strongest but provides a bit of protection
            // I mean Discord says to encrypt it so I mean yeah, I'm doing that
            // Whatever
            var bytes = Encoding.UTF8.GetBytes(secret);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decrypt a message by converting it from base64 to UTF8. This encryption is only to satisfy the requirements that Discord puts for secrets
        /// </summary>
        /// <param name="secret">The secret to decrypt</param>
        public static string Decrypt(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                return string.Empty;

            var bytes = Convert.FromBase64String(secret);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    /// <summary>
    /// Timestamp for the <see cref="RichPresenceManager"/>
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

    /// <summary>
    /// Holds information about the timestamp override
    /// </summary>
    /// <param name="timestamp"><inheritdoc cref="Timestamp"/></param>
    /// <param name="origin"><inheritdoc cref="Origin"/></param>
    public class TimestampOverride(Timestamp timestamp, string origin)
    {
        /// <summary>
        /// Timestamp for the <see cref="RichPresenceManager"/>
        /// </summary>
        public Timestamp Timestamp { get; set; } = timestamp;

        /// <summary>
        /// String enabling to recognize where the override came from
        /// </summary>
        public string Origin { get; set; } = origin;
    }

    /// <summary>
    /// An asset to be used in <see cref="Assets"/> for <see cref="RichPresence"/>
    /// </summary>
    public class Asset
    {
        /// <summary>
        /// The key of the asset. This can either be a name of an image uploaded in Art Assets under Rich Presence on the application.
        /// <para>
        /// You can also use links that point to an image.
        /// </para>
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The text that will display when you hover over the asset (128 bytes max)
        /// </summary>
        public string ToolTip { get; set; }

        /// <summary>
        /// Initialize a new instance of <see cref="Asset"/>
        /// </summary>
        /// <param name="key"><inheritdoc cref="Key"/></param>
        /// <param name="tooltip"><inheritdoc cref="ToolTip"/></param>
        public Asset(string key, string tooltip)
        {
            Key = key;
            ToolTip = tooltip;
        }

        /// <summary>
        /// Initialize a new instance of <see cref="Asset"/>
        /// </summary>
        /// <param name="uri">The URI of the file that contains the image</param>
        /// <param name="tooltip"><inheritdoc cref="ToolTip"/></param>
        public Asset(Uri uri, string tooltip)
        {
            Key = uri.ToString();
            ToolTip = tooltip;
        }
    }
}