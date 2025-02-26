using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using LabPresence.Config;

namespace LabPresence
{
    public static class Placeholders
    {
        private static readonly List<Placeholder> _Placeholders = [];

        public static void RegisterPlaceholder(this Placeholder placeholder)
        {
            if (placeholder == null)
                throw new ArgumentNullException(nameof(placeholder));

            if (string.IsNullOrWhiteSpace(placeholder.Name))
                throw new ArgumentNullException(nameof(placeholder), "The name cannot be null or empty!");

            if (placeholder.Value == null)
                throw new ArgumentNullException(nameof(placeholder), "The value callback cannot be null!");

            if (_Placeholders.Contains(placeholder))
                throw new ArgumentException("The placeholder is already registered");

            if (_Placeholders.Any(x => x.Name == placeholder.Name))
                throw new ArgumentException("A placeholder with the same name already exists!");

            _Placeholders.Add(placeholder);
        }

        public static void RegisterPlaceholder(string name, Func<string[], string> function)
            => RegisterPlaceholder(new Placeholder(name, function));

        public static void RegisterPlaceholder(string name, Func<string[], string> function, params string[] aliases)
            => RegisterPlaceholder(new Placeholder(name, function, aliases));

        public static void RegisterPlaceholder(string name, Func<string[], string> function, float minimumDelay)
            => RegisterPlaceholder(new Placeholder(name, function, minimumDelay));

        public static void RegisterPlaceholder(string name, Func<string[], string> function, float minimumDelay, params string[] aliases)
            => RegisterPlaceholder(new Placeholder(name, function, minimumDelay, aliases));

        public static bool UnregisterPlaceholder(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), "The name cannot be empty or null!");

            if (_Placeholders.Count == 0)
                return true;

            var index = _Placeholders.FindIndex(0, x => x.Name == name);
            if (index == -1)
                return false;

            _Placeholders.RemoveAt(index);
            return true;
        }

        public static bool UnregisterPlaceholder(this Placeholder placeholder)
            => UnregisterPlaceholder(placeholder?.Name);

        public static bool IsPlaceholderRegistered(string name)
            => _Placeholders.Any(x => x.Name == name);

        public static bool IsPlaceholderRegistered(this Placeholder placeholder)
            => IsPlaceholderRegistered(placeholder?.Name);

        public static int GetPlaceholderCount()
            => _Placeholders.Count;

        public static string[] GetPlaceholderNames()
        {
            List<string> names = [];
            _Placeholders.ForEach(x => names.Add(x.Name));
            return [.. names];
        }

        private const string PlaceholderRegex = @"\\%(?'escaped'{0})\\%|%(?'found'{0})(?:(?:\|)(?'arg'.*?))*(?<!\\)%";

        public static Placeholder[] GetPlaceholdersInString(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            if (_Placeholders.Count == 0)
                return [];

            List<Placeholder> found = [];
            foreach (var placeholder in _Placeholders)
            {
                List<string> names = [placeholder.Name];
                if (placeholder.Aliases != null) names.AddRange(placeholder.Aliases);
                foreach (var name in names)
                {
                    Regex regex = new(string.Format(PlaceholderRegex, Regex.Escape(name)));
                    Match match = regex.Match(text);
                    if (match.Success && match?.Groups?.ContainsKey("found") == true)
                    {
                        found.Add(placeholder);
                        break;
                    }
                }
            }
            return [.. found];
        }

        public static string ApplyPlaceholders(this string text)
        {
            foreach (var placeholder in _Placeholders)
            {
                List<string> names = [placeholder.Name];
                if (placeholder.Aliases != null) names.AddRange(placeholder.Aliases);
                foreach (var name in names)
                {
                    Regex regex = new(string.Format(PlaceholderRegex, Regex.Escape(name)));
                    string match(Match match)
                    {
                        if (match.Success)
                        {
                            if (match.Groups.ContainsKey("escaped") &&
                                match.Groups["escaped"].Success &&
                                !string.IsNullOrWhiteSpace(match.Groups["escaped"].Value))
                            {
                                return $"%{name}%";
                            }
                            else if (match.Groups.ContainsKey("found") &&
                                match.Groups["found"].Success &&
                                !string.IsNullOrWhiteSpace(match.Groups["found"].Value))
                            {
                                try
                                {
                                    if (match.Groups.ContainsKey("arg"))
                                    {
                                        var group = match.Groups["arg"];
                                        string[] vals = new string[group.Captures.Count];
                                        for (int i = 0; i < group.Captures.Count; i++)
                                            vals[i] = group.Captures[i].Value;
                                        return placeholder.Value.Invoke(vals);
                                    }
                                    else
                                    {
                                        return placeholder.Value.Invoke([]);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Core.Logger.Error($"Placeholder '{name}' threw an exception:\n{ex}");
                                }
                            }
                        }
                        return string.Empty;
                    }
                    text = regex.Replace(text, match);
                }
            }
            return text;
        }

        public static float GetMinimumDelay(this RPCConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var state = config.State?.GetPlaceholdersInString();
            var details = config.Details?.GetPlaceholdersInString();

            List<Placeholder> placeholders = [];
            if (state != null) placeholders.AddRange(state);
            if (details != null) placeholders.AddRange(details);

            float max = 0.1f;
            placeholders?.ForEach(x => max = (float)Math.Clamp(max, x.MinimalDelay, double.MaxValue));
            return max;
        }
    }

    public class Placeholder
    {
        /// <summary>
        /// Name of the placeholder which will be used in the placeholder
        /// <para>Example: %test%</para>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Function which returns the value of the placeholder
        /// </summary>
        public Func<string[], string> Value { get; set; }

        /// <summary>
        /// Aliases which can be used instead of the Name
        /// </summary>
        public string[] Aliases { get; set; } = [];

        /// <summary>
        /// Discord has a limit of 5 requests / 20 seconds to the RPC.
        /// <para>If the value constantly changes, it is recommended to set the minimum delay to at least 4 seconds</para>
        /// </summary>
        public float MinimalDelay { get; set; } = -1f;

        public Placeholder(string name, Func<string[], string> value)
        {
            this.Name = name;
            this.Value = value;
        }

        public Placeholder(string name, Func<string[], string> value, float minimalDelay)
        {
            this.Name = name;
            this.Value = value;
            this.MinimalDelay = minimalDelay;
        }

        public Placeholder(string name, Func<string[], string> value, params string[] aliases)
        {
            this.Name = name;
            this.Value = value;
            this.Aliases = aliases ?? [];
        }

        public Placeholder(string name, Func<string[], string> value, float minimalDelay, params string[] aliases)
        {
            this.Name = name;
            this.Value = value;
            this.MinimalDelay = minimalDelay;
            this.Aliases = aliases ?? [];
        }
    }
}