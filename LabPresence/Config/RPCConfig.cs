using Tomlet.Attributes;

namespace LabPresence.Config
{
    public class RPCConfig
    {
        [TomlProperty("Use")]
        public bool Use { get; set; } = true;

        [TomlProperty("Details")]
        public string Details { get; set; }

        [TomlProperty("State")]
        public string State { get; set; }

        public RPCConfig()
        {
        }

        public RPCConfig(bool use)
        {
            Use = use;
        }

        public RPCConfig(string details)
        {
            Details = details;
            Use = true;
        }

        public RPCConfig(string details, string state)
        {
            Details = details;
            State = state;
            Use = true;
        }
    }
}