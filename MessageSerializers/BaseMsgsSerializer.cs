using System;
using System.Collections.Generic;
using System.Text;

namespace RosBagConverter.MessageSerializers
{
    /// <summary>
    /// Base class for different message serializers
    /// </summary>
    public class BaseMsgsSerializer
    {
        protected bool useHeaderTime; // Whether to use the header time (if available) or message publish time
        public BaseMsgsSerializer(bool useHeaderTime = false)
        {
            this.useHeaderTime = useHeaderTime;
        }
    }
}
