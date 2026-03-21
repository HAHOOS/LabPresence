using System;
using System.Collections.Generic;
using System.Linq;

using Scriban;
using Scriban.Runtime;

namespace LabPresence.Managers
{
    /// <summary>
    /// Class responsible for handling placeholders
    /// </summary>
    public static class PlaceholderManager
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

        /// <summary>
        /// Register a new <see cref="Placeholder"/>
        /// </summary>
        /// <param name="id"><see cref="Placeholder.ID"/></param>
        /// <param name="values"><see cref="Placeholder.Values"/></param>
        public static void RegisterPlaceholder(string id, Func<ScriptObject> values)
            => new Placeholder(id, values).RegisterPlaceholder();

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

            var index = _Placeholders.FindIndex(x => x.ID == name);
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
            => UnregisterPlaceholder(placeholder?.ID);

        /// <summary>
        /// Checks if a placeholder is registered
        /// </summary>
        /// <param name="id">ID of the <see cref="Placeholder"/></param>
        public static bool IsPlaceholderRegistered(string id)
            => _Placeholders.Any(x => x.ID == id);

        /// <summary>
        /// Checks if a placeholder is registered
        /// </summary>
        /// <param name="placeholder">The <see cref="Placeholder"/> to check</param>
        public static bool IsPlaceholderRegistered(this Placeholder placeholder)
            => IsPlaceholderRegistered(placeholder?.ID);

        /// <summary>
        /// Get the amount of registered placeholders
        /// </summary>
        public static int GetPlaceholderCount()
            => _Placeholders.Count;

        /// <summary>
        /// Apply placeholders in a text
        /// </summary>
        /// <param name="text">The text to apply the placeholders to</param>
        /// <returns>Text with applied placeholders</returns>
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

            var defaultObject = new ScriptObject();
            defaultObject.Import(typeof(ScribanHelper));
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

    /// <summary>
    /// A placeholder to be used in <see cref="PlaceholderManager"/>
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of a <see cref="Placeholder"/>
    /// </remarks>
    /// <param name="id"><inheritdoc cref="ID"/></param>
    /// <param name="values"><inheritdoc cref="Values"/></param>
    public class Placeholder(string id, Func<ScriptObject> values)
    {
        /// <summary>
        /// ID of the placeholder
        /// </summary>
        public string ID { get; set; } = id;

        /// <summary>
        /// Function which returns a <seealso cref="ScriptObject"/>, containing all of the placeholders
        /// </summary>
        public Func<ScriptObject> Values { get; set; } = values;
    }
}