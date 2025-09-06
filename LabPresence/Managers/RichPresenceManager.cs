using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

using DiscordRPC;
using DiscordRPC.Exceptions;
using DiscordRPC.Helper;

using LabPresence.Config;
using LabPresence.Helper;

using UnityEngine;

namespace LabPresence.Managers
{
    /// <summary>
    /// Class responsible for managing the Rich Presence
    /// </summary>
    public static class RichPresenceManager
    {
        private static Presence CurrentPresence { get; set; }

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
        /// <param name="timestamp">The <see cref="Managers.Timestamp"/> to set</param>
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

        private static float time = 0f;
        private static Presence old = null;

        internal static void OnUpdate()
        {
            if (CurrentPresence != null)
            {
                time += Time.deltaTime;
                if (CurrentPresence != old)
                {
                    time = 0f;
                    old = CurrentPresence;
                }
                var delay = CurrentPresence.GetMinimumDelay();
                if (time >= delay)
                {
                    TrySetRichPresence(CurrentPresence.Config, CurrentPresence.Type, CurrentPresence.Party, CurrentPresence.Secrets, CurrentPresence.LargeImage, CurrentPresence.SmallImage);
                    time = 0f;
                }
            }
            else
            {
                time = 0f;
            }
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
            => SetTimestamp(Timestamp.Now, autoUpdate);

        /// <summary>
        /// Set the timestamp to display the current time, like "16:50:00"
        /// </summary>
        /// <param name="autoUpdate">Should the timestamp be automatically updated</param>
        public static void SetTimestampToCurrentTime(bool autoUpdate = false)
            => SetTimestamp(Timestamp.CurrentTime, autoUpdate);

        /// <summary>
        /// Set the <see cref="OverrideTimestamp"/>
        /// </summary>
        /// <param name="timestamp">The <see cref="Managers.Timestamp"/> to set</param>
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
        /// Attempts to call <see cref="StringTools.GetNullOrString(string)"/> on the string and return the result, if its within a valid length.
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <param name="result">The formatted string result</param>
        /// <param name="useBytes">True if you need to validate the string by byte length</param>
        /// <param name="length">The maximum number of bytes/characters the string can take up</param>
        /// <param name="encoding">The encoding to count the bytes with, optional</param>
        /// <returns>True if the string fits within the number of bytes</returns>
        internal static bool ValidateString(string str, out string result, bool useBytes, int length, Encoding encoding = null)
        {
            result = str;
            if (str == null)
                return true;

            //Trim the string, for the best chance of fitting
            var s = str.Trim();

            //Make sure it fits
            if ((useBytes && !s.WithinLength(length, encoding)) || s.Length > length)
                return false;

            //Make sure its not empty
            result = s.GetNullOrString();
            return true;
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
            if (!ValidateString(Core.RemoveUnityRichText(details?.ApplyPlaceholders()), out string det, false, 128, Encoding.UTF8) ||
                !ValidateString(Core.RemoveUnityRichText(state?.ApplyPlaceholders()), out string stat, false, 128, Encoding.UTF8) ||
                !ValidateString(Core.RemoveUnityRichText(largeImage?.ToolTip?.ApplyPlaceholders()), out _, false, 128, Encoding.UTF8) ||
                !ValidateString(Core.RemoveUnityRichText(smallImage?.ToolTip?.ApplyPlaceholders()), out _, false, 128, Encoding.UTF8))
            {
                throw new ArgumentOutOfRangeException("State, Details and/or an asset tooltip is/are over 128 bytes which Rich Presence cannot handle, try to lower the amount of characters");
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

            bool res = TrySetRichPresence(config.Details, config.State, type, party, secrets, largeImage, smallImage);
            if (res)
            {
                CurrentConfig = config;
                CurrentPresence = new Presence(config, type, party, secrets, largeImage, smallImage);
            }
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
            CurrentPresence = new Presence(config, type, party, secrets, largeImage, smallImage);
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

    public class Presence(RPCConfig config, ActivityType type, Party party, Secrets secrets, Asset largeImage, Asset smallImage)
    {
        public RPCConfig Config { get; internal set; } = config;

        public ActivityType Type { get; internal set; } = type;

        public Party Party { get; internal set; } = party;

        public Secrets Secrets { get; internal set; } = secrets;

        public Asset LargeImage { get; internal set; } = largeImage;

        public Asset SmallImage { get; internal set; } = smallImage;
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