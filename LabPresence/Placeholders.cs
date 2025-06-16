using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using LabPresence.Config;

namespace LabPresence
{
    /// <summary>
    /// Class responsible for handling placeholders
    /// </summary>
    public static class Placeholders
    {
        private static readonly List<Placeholder> _Placeholders = [];

        /// <summary>
        /// Register a new placeholder
        /// </summary>
        /// <param name="placeholder"><see cref="Placeholder"/> to register</param>
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

        /// <summary>
        /// Register a new <see cref="Placeholder"/>
        /// </summary>
        /// <param name="name"><see cref="Placeholder.Name"/></param>
        /// <param name="function"><see cref="Placeholder.Value"/></param>
        public static void RegisterPlaceholder(string name, Func<string[], string> function)
            => RegisterPlaceholder(new Placeholder(name, function));

        /// <summary>
        /// Register a new <see cref="Placeholder"/>
        /// </summary>
        /// <param name="name"><see cref="Placeholder.Name"/></param>
        /// <param name="function"><see cref="Placeholder.Value"/></param>
        /// <param name="aliases"><see cref="Placeholder.Aliases"/></param>
        public static void RegisterPlaceholder(string name, Func<string[], string> function, params string[] aliases)
            => RegisterPlaceholder(new Placeholder(name, function, aliases));

        /// <summary>
        /// Register a new <see cref="Placeholder"/>
        /// </summary>
        /// <param name="name"><see cref="Placeholder.Name"/></param>
        /// <param name="function"><see cref="Placeholder.Value"/></param>
        /// <param name="minimumDelay"><see cref="Placeholder.MinimalDelay"/></param>
        public static void RegisterPlaceholder(string name, Func<string[], string> function, float minimumDelay)
            => RegisterPlaceholder(new Placeholder(name, function, minimumDelay));

        /// <summary>
        /// Register a new <see cref="Placeholder"/>
        /// </summary>
        /// <param name="name"><see cref="Placeholder.Name"/></param>
        /// <param name="function"><see cref="Placeholder.Value"/></param>
        /// <param name="minimumDelay"><see cref="Placeholder.MinimalDelay"/></param>
        /// <param name="aliases"><see cref="Placeholder.Aliases"/></param>
        public static void RegisterPlaceholder(string name, Func<string[], string> function, float minimumDelay, params string[] aliases)
            => RegisterPlaceholder(new Placeholder(name, function, minimumDelay, aliases));

        /// <summary>
        /// Unregister a <see cref="Placeholder"/>
        /// </summary>
        /// <param name="name">The name of the <see cref="Placeholder"/></param>
        /// <returns>Was the placeholder removed successfully</returns>
        /// <exception cref="ArgumentNullException">The name cannot be empty or null!</exception>
        public static bool UnregisterPlaceholder(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), "The name cannot be empty or null!");

            if (_Placeholders.Count == 0)
                return false;

            var index = _Placeholders.FindIndex(x => x.Name == name);
            if (index == -1)
                return false;

            _Placeholders.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Unregister a <see cref="Placeholder"/>
        /// </summary>
        /// <param name="placeholder">The <see cref="Placeholder"/> to unregister</param>
        /// <returns>Was the placeholder removed successfully</returns>
        /// <exception cref="ArgumentNullException">The name cannot be empty or null!</exception>
        public static bool UnregisterPlaceholder(this Placeholder placeholder)
            => UnregisterPlaceholder(placeholder?.Name);

        /// <summary>
        /// Checks if a placeholder is registered
        /// </summary>
        /// <param name="name">Name of the <see cref="Placeholder"/></param>
        public static bool IsPlaceholderRegistered(string name)
            => _Placeholders.Any(x => x.Name == name);

        /// <summary>
        /// Checks if a placeholder is registered
        /// </summary>
        /// <param name="placeholder">The <see cref="Placeholder"/> to check</param>
        public static bool IsPlaceholderRegistered(this Placeholder placeholder)
            => IsPlaceholderRegistered(placeholder?.Name);

        /// <summary>
        /// Get the amount of registered placeholders
        /// </summary>
        public static int GetPlaceholderCount()
            => _Placeholders.Count;

        /// <summary>
        /// Get the names of all registered placeholders
        /// </summary>
        public static string[] GetPlaceholderNames()
        {
            List<string> names = [];
            _Placeholders.ForEach(x => names.Add(x.Name));
            return [.. names];
        }

        // Now when I look at this, I don't really know why I bothered to overcomplicate it so much. Probably, because I had too much free time.
        private const string PlaceholderRegex = @"\\%(?'escaped'{0})\\%|%(?'found'{0})(?:(?:\|)(?'arg'.*?))*(?<!\\)%";

        /// <summary>
        /// Get all the placeholders present in a string
        /// </summary>
        /// <param name="text">The text to check</param>
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

        /// <summary>
        /// Apply placeholders in a text
        /// </summary>
        /// <param name="text">The text to apply the placeholders to</param>
        /// <returns>Text with applied placeholders</returns>
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

        /// <summary>
        /// Get the minimum delay for a config
        /// </summary>
        /// <param name="config">The config to get the delay of</param>
        /// <exception cref="ArgumentNullException">The config is null</exception>
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

    /// <summary>
    /// A placeholder to be used in <see cref="Placeholders"/>
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of a <see cref="Placeholder"/>
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        /// <param name="value"><inheritdoc cref="Value"/></param>
        public Placeholder(string name, Func<string[], string> value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="Placeholder"/>
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        /// <param name="value"><inheritdoc cref="Value"/></param>
        /// <param name="minimalDelay"><inheritdoc cref="MinimalDelay"/></param>
        public Placeholder(string name, Func<string[], string> value, float minimalDelay)
        {
            this.Name = name;
            this.Value = value;
            this.MinimalDelay = minimalDelay;
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="Placeholder"/>
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        /// <param name="value"><inheritdoc cref="Value"/></param>
        /// <param name="aliases"><inheritdoc cref="Aliases"/></param>
        public Placeholder(string name, Func<string[], string> value, params string[] aliases)
        {
            this.Name = name;
            this.Value = value;
            this.Aliases = aliases ?? [];
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="Placeholder"/>
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name"/></param>
        /// <param name="value"><inheritdoc cref="Value"/></param>
        /// <param name="minimalDelay"><inheritdoc cref="MinimalDelay"/></param>
        /// <param name="aliases"><inheritdoc cref="Aliases"/></param>
        public Placeholder(string name, Func<string[], string> value, float minimalDelay, params string[] aliases)
        {
            this.Name = name;
            this.Value = value;
            this.MinimalDelay = minimalDelay;
            this.Aliases = aliases ?? [];
        }
    }
}