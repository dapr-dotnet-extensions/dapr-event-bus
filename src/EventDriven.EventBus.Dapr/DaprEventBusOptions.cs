﻿namespace EventDriven.EventBus.Dapr
{
    /// <summary>
    /// DaprEventBus options.
    /// </summary>
    public class DaprEventBusOptions
    {
        /// <summary>
        /// Dapr PubSub component name.
        /// </summary>
        public string PubSubName { get; set; } = "pubsub";
        
        /// <summary>
        /// Disable retries.
        /// </summary>
        public bool DisableRetries { get; set; }
    }
}
