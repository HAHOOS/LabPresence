using Tomlet.Attributes;

namespace LabPresence.Config
{
    public class RpcConfig
    {
        [TomlProperty("Use")]
        public bool Use { get; set; } = true;

        [TomlProperty("Details")]
        public string Details { get; set; }

        [TomlProperty("State")]
        public string State { get; set; }

        public RpcConfig()
        {
        }

        public RpcConfig(bool use)
        {
            Use = use;
        }

        public RpcConfig(string details)
        {
            Details = details;
            Use = true;
        }

        public RpcConfig(string details, string state)
        {
            Details = details;
            State = state;
            Use = true;
        }
    }
}