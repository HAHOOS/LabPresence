using Tomlet.Attributes;

namespace LabPresence.Config
{
    /// <summary>
    /// Config for the Rich Presence, to be used in the other configs
    /// </summary>
    public class RPCConfig
    {
        /// <summary>
        /// Should the config be used
        /// </summary>
        [TomlProperty("Use")]
        public bool Use { get; set; } = true;

        /// <summary>
        /// What the player is currently doing
        /// </summary>
        [TomlProperty("Details")]
        public string Details { get; set; }

        /// <summary>
        /// User's current party status, or text used for a custom status
        /// </summary>
        [TomlProperty("State")]
        public string State { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="RPCConfig"/>
        /// </summary>
        public RPCConfig()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RPCConfig"/>
        /// </summary>
        /// <param name="use"><inheritdoc cref="Use"/></param>
        public RPCConfig(bool use)
        {
            Use = use;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RPCConfig"/>
        /// </summary>
        /// <param name="details"><inheritdoc cref="Details"/></param>
        public RPCConfig(string details)
        {
            Details = details;
            Use = true;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="RPCConfig"/>
        /// </summary>
        /// <param name="details"><inheritdoc cref="Details"/></param>
        /// <param name="state"><inheritdoc cref="State"/></param>
        public RPCConfig(string details, string state)
        {
            Details = details;
            State = state;
            Use = true;
        }
    }
}