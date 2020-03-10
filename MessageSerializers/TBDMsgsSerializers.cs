using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;

namespace RosBagConverter.MessageSerializers
{
    class TBDMsgsSerializers : BaseMsgsSerializer
    {
        public TBDMsgsSerializers(bool useHeaderTime = false, TimeSpan? offset = null)
        : base("tbd_", useHeaderTime, offset: offset)
        {
        }

        public override bool SerializeMessage(Pipeline pipeline, Exporter store, string streamName, IEnumerable<RosMessage> messages, string messageType)
        {
            switch (messageType)
            {
                case "tbd_audio_msgs/Utterance":
                    DynamicSerializers.WriteStronglyTyped<string>(pipeline, streamName, messages.Select(m => { return (m.GetField("text"), this.ConvertMessageHeader(m)); }), store);
                    return true;
                case "tbd_audio_msgs/AudioDataStamped":
                    DynamicSerializers.WriteStronglyTyped<AudioBuffer>(pipeline, streamName, messages.Select(m => { return ( new AudioBuffer(m.GetField("data"), WaveFormat.Create16kHz1Channel16BitPcm()), this.ConvertMessageHeader(m)); }), store);
                    return true;
                case "tbd_audio_msgs/VADStamped":
                    DynamicSerializers.WriteStronglyTyped<bool>(pipeline, streamName, messages.Select(m => { return (m.GetField("is_speech"), this.ConvertMessageHeader(m)); }), store);
                    return true;
            }
            return false;
        }
    }
}
