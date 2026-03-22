using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

using DiscordRPC;
using DiscordRPC.Exceptions;
using DiscordRPC.Helper;

using LabPresence.Config;
using LabPresence.Utilities;

using UnityEngine;

namespace LabPresence.Managers
{
    public static class RichPresenceManager
    {
        private static Presence CurrentPresence { get; set; }

        public static RpcConfig CurrentConfig { get; private set; }

        public static Timestamp Timestamp { get; private set; }

        public static TimestampOverride OverrideTimestamp { get; private set; }

        public static bool AutoUpdate { get; set; } = true;

        public static void UpdateTimestamp()
        {
            if (Core.Client.CurrentPresence != null)
                Core.Client.Update(x => x.Timestamps = (OverrideTimestamp?.Timestamp != null) ? OverrideTimestamp?.Timestamp?.ToRPC() : Timestamp.ToRPC());
        }

        private static float time = 0f;
        private static Presence old = null;

        internal static void OnUpdate()
        {
            if (CurrentPresence != null && AutoUpdate)
            {
                time += Time.deltaTime;
                if (CurrentPresence != old)
                {
                    time = 0f;
                    old = CurrentPresence;
                }
                const float delay = 5f;
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

        public static void SetTimestamp(Timestamp timestamp, bool autoUpdate = false)
        {
            Timestamp = timestamp;

            if (autoUpdate)
                UpdateTimestamp();
        }

        public static void SetTimestamp(ulong? start, ulong? end, bool autoUpdate = false)
            => SetTimestamp(new(start, end), autoUpdate);

        public static void SetTimestampStartToNow(bool autoUpdate = false)
            => SetTimestamp(Timestamp.Now, autoUpdate);

        public static void SetTimestampToCurrentTime(bool autoUpdate = false)
            => SetTimestamp(Timestamp.CurrentTime, autoUpdate);

        public static void SetOverrideTimestamp(TimestampOverride timestamp, bool autoUpdate = false)
        {
            OverrideTimestamp = timestamp;
            if (autoUpdate) UpdateTimestamp();
        }

        public static void ResetOverrideTimestamp(bool autoUpdate = false)
        {
            OverrideTimestamp = null;
            if (autoUpdate) UpdateTimestamp();
        }

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

        private static void SetRichPresence(string details, string state, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, Asset largeImage = null, Asset smallImage = null)
        {
            if (Core.Client?.IsInitialized != true)
                throw new InvalidOperationException("The RPC client is not initialized!");

            if (Core.Config.UseAnimatedLogo)
                largeImage ??= new("https://raw.githubusercontent.com/HAHOOS/LabPresence/refs/heads/master/Data/animated.gif", "BONELAB");
            else
                largeImage ??= new("icon", "BONELAB");

            if (!ValidateString(Core.RemoveUnityRichText(details?.ApplyPlaceholders()), out string det, false, 128, Encoding.UTF8) ||
                !ValidateString(Core.RemoveUnityRichText(state?.ApplyPlaceholders()), out string stat, false, 128, Encoding.UTF8) ||
                !ValidateString(Core.RemoveUnityRichText(largeImage?.ToolTip?.ApplyPlaceholders()), out string large, false, 128, Encoding.UTF8) ||
                !ValidateString(Core.RemoveUnityRichText(smallImage?.ToolTip?.ApplyPlaceholders()), out string small, false, 128, Encoding.UTF8))
            {
                throw new ArgumentException(
                    message: "State, Details and/or an asset tooltip is/are over 128 bytes which Rich Presence cannot handle, try to lower the amount of characters"
                );
            }

            largeImage?.ToolTip = large;
            smallImage?.ToolTip = small;

            Core.Client.SetPresence(new DiscordRPC.RichPresence()
            {
                Details = det,
                State = stat,
                Timestamps = (OverrideTimestamp?.Timestamp != null) ? OverrideTimestamp?.Timestamp?.ToRPC() : Timestamp?.ToRPC(),
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

        public static bool TrySetRichPresence(RpcConfig config, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, Asset largeImage = null, Asset smallImage = null)
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

        public static void SetRichPresence(RpcConfig config, ActivityType type = ActivityType.Playing, Party party = null, Secrets secrets = null, Asset largeImage = null, Asset smallImage = null)
        {
            if (!config.Use)
                return;

            SetRichPresence(config.Details, config.State, type, party, secrets, largeImage, smallImage);
            CurrentPresence = new Presence(config, type, party, secrets, largeImage, smallImage);
            CurrentConfig = config;
        }

        private static readonly Dictionary<ulong, Texture2D> _avatarCache = [];

        public static Texture2D GetAvatar(User user, User.AvatarSize size = User.AvatarSize.x512, bool cache = false)
        {
            ArgumentNullException.ThrowIfNull(user);

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
                    texture = ImageConversion.LoadTexture($"{user.DisplayName} (@{user.Username})", bytesTask.Result);
            }

            if (cache && texture != null)
                _avatarCache[user.ID] = texture;

            return texture;
        }

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

        public static string Decrypt(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                return string.Empty;

            var bytes = Convert.FromBase64String(secret);
            return Encoding.UTF8.GetString(bytes);
        }
    }

    public class Presence(RpcConfig config, ActivityType type, Party party, Secrets secrets, Asset largeImage, Asset smallImage)
    {
        public RpcConfig Config { get; internal set; } = config;

        public ActivityType Type { get; internal set; } = type;

        public Party Party { get; internal set; } = party;

        public Secrets Secrets { get; internal set; } = secrets;

        public Asset LargeImage { get; internal set; } = largeImage;

        public Asset SmallImage { get; internal set; } = smallImage;
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

        public static Timestamp Now
        {
            get => new((ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds(), null);
        }

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

        public Timestamps ToRPC()
            => new() { EndUnixMilliseconds = End, StartUnixMilliseconds = Start };
    }

    public class TimestampOverride(Timestamp timestamp, string origin)
    {
        public Timestamp Timestamp { get; set; } = timestamp;

        public string Origin { get; set; } = origin;
    }

    public class Asset
    {
        public string Key { get; set; }

        public string ToolTip { get; set; }

        public Asset(string key, string tooltip)
        {
            Key = key;
            ToolTip = tooltip;
        }

        public Asset(Uri uri, string tooltip)
        {
            Key = uri.ToString();
            ToolTip = tooltip;
        }
    }
}