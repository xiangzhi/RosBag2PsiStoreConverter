using Microsoft.Psi;
using Microsoft.Psi.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace RosBagConverter.MessageSerializers
{
    /// <summary>
    /// Base class for different message serializers
    /// </summary>
    public abstract class BaseMsgsSerializer
    {
        protected bool useHeaderTime; // Whether to use the header time (if available) or message publish time
        public BaseMsgsSerializer(string prefix, bool useHeaderTime = false, TimeSpan? offset = null)
        {
            this.useHeaderTime = useHeaderTime;
            this.Prefix = prefix;
            this.Offset = offset ?? TimeSpan.Zero;
        }

        /// <summary>
        /// Attempt to convert and serialize the ROS message into Psi specific formats.
        /// </summary>
        /// <param name="pipeline">Current pipeline</param>
        /// <param name="store">Store to store the messages</param>
        /// <param name="streamName">The name of the stream to be saved</param>
        /// <param name="messages">IEnumerate of ROS Messages</param>
        /// <param name="messageType">A string describing the type of ROS Message</param>
        /// <returns>Whether the convert and serialization of the messages were successful</returns>
        public abstract bool SerializeMessage(Pipeline pipeline, Exporter store, string streamName, IEnumerable<RosMessage> messages, string messageType);

        /// <summary>
        /// Helper function to convert ROS Header to Psi Stream time. First check if use header time is set before converting
        /// </summary>
        /// <param name="m">Incoming Message</param>
        /// <returns>Datetime from header if set, else Datetime of message publish time</returns>
        protected DateTime ConvertMessageHeader(RosMessage m)
        {
            if (!m.HasField("header") && this.useHeaderTime)
            {
                throw new InvalidCastException("Attempting to extract header time in non-header ROS message.");
            }
            return this.useHeaderTime ? ((RosHeader) m.GetField("header")).Time.ToDateTime() + this.Offset : m.Time.ToDateTime() + this.Offset;
        }

        public string Prefix { get; private set; }

        public TimeSpan Offset { get; private set; }

    }
}
