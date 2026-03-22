using System;
using System.Collections.Generic;
using System.Linq;

using LabPresence.Utilities;

using Scriban;
using Scriban.Runtime;

namespace LabPresence.Managers
{
    public static class PlaceholderManager
    {
        private static readonly List<Placeholder> _Placeholders = [];

        public static void RegisterPlaceholder(this Placeholder placeholder)
        {
            if (placeholder == null)
                throw new ArgumentNullException(nameof(placeholder));

            if (string.IsNullOrWhiteSpace(placeholder.ID))
                throw new ArgumentNullException(nameof(placeholder), "The ID cannot be null or empty!");

            if (placeholder.Values == null)
                throw new ArgumentNullException(nameof(placeholder), "The values callback cannot be null!");

            if (_Placeholders.Contains(placeholder))
                throw new ArgumentException("The placeholder is already registered");

            if (_Placeholders.Any(x => x.ID == placeholder.ID))
                throw new ArgumentException("A placeholder with the same ID already exists!");

            _Placeholders.Add(placeholder);
        }

        public static void RegisterPlaceholder(string id, Func<ScriptObject> values)
            => new Placeholder(id, values).RegisterPlaceholder();

        public static bool UnregisterPlaceholder(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), "The name cannot be empty or null!");

            if (_Placeholders.Count == 0)
                return false;

            var index = _Placeholders.FindIndex(x => x.ID == name);
            if (index == -1)
                return false;

            _Placeholders.RemoveAt(index);
            return true;
        }

        public static bool UnregisterPlaceholder(this Placeholder placeholder)
            => UnregisterPlaceholder(placeholder?.ID);

        public static bool IsPlaceholderRegistered(string id)
            => _Placeholders.Any(x => x.ID == id);

        public static bool IsPlaceholderRegistered(this Placeholder placeholder)
            => IsPlaceholderRegistered(placeholder?.ID);

        public static int GetPlaceholderCount()
            => _Placeholders.Count;

        public static string ApplyPlaceholders(this string text)
        {
            var template = Template.Parse(text);

            if (template.HasErrors)
            {
                Core.Logger.Error($"An error occurred while parsing the text! '{text}'");
                foreach (var error in template.Messages)
                {
                    Core.Logger.Error($"{(error.Type == Scriban.Parsing.ParserMessageType.Error ? "[ERR]" : "[WARN]")} {error.Message}");
                }
                return text;
            }

            var content = new TemplateContext();

            var defaultObject = new ScriptObject(StringComparer.OrdinalIgnoreCase)
            {
                { "utils", new ScribanUtils() }
            };
            content.PushGlobal(defaultObject);

            foreach (var placeholder in _Placeholders)
            {
                try
                {
                    content.PushGlobal(placeholder.Values?.Invoke());
                }
                catch (Exception ex)
                {
                    Core.Logger.Error($"An error occurred while retrieving placeholders with ID '{placeholder.ID}'!", ex);
                }
            }

            return template.Render(content);
        }
    }

    public class Placeholder(string id, Func<ScriptObject> values)
    {
        public string ID { get; set; } = id;

        public Func<ScriptObject> Values { get; set; } = values;
    }
}