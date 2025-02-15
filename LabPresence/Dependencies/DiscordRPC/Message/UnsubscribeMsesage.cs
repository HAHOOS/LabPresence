using DiscordRPC.RPC.Payload;

namespace DiscordRPC.Message
{
    /// <summary>
    /// Called as validation of a subscribe
    /// </summary>
    public class UnsubscribeMessage : IMessage
    {
        /// <summary>
        /// The type of message received from discord
        /// </summary>
        public override MessageType Type { get { return MessageType.Unsubscribe; } }

        /// <summary>
        /// The event that was subscribed too.
        /// </summary>
        public EventType Event { get; internal set; }

        internal UnsubscribeMessage(ServerEvent evt)
        {
            Event = evt switch
            {
                ServerEvent.ActivityJoinRequest => EventType.JoinRequest,
                ServerEvent.ActivitySpectate => EventType.Spectate,
                _ => EventType.Join,
            };
        }
    }
}