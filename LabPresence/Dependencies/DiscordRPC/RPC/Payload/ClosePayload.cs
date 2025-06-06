﻿using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordRPC.RPC.Payload
{
    [method: JsonConstructor]
    internal class ClosePayload() : IPayload()
    {
        /// <summary>
        /// The close code the discord gave us
        /// </summary>
        [JsonProperty("code")]
        public int Code { get; set; } = -1;

        /// <summary>
        /// The close reason discord gave us
        /// </summary>
        [JsonProperty("message")]
        public string Reason { get; set; } = "";
    }
}