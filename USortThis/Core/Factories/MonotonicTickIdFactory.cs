using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;

namespace USortThis.Core.Factories
{
    public class MonotonicTickIdFactory : IIdFactory
    {
        // last composed id (timestamp portion shifted + counter)
        private long _lastId;
        // 16-bit counter for IDs generated within the same tick
        private uint _counter;

        /// <summary>Creates a monotonically increasing identifier from the current UTC tick and a counter.</summary>
        /// <returns>A numeric identifier that preserves generation order.</returns>
        public long CreateId()
        {
            long timestamp = DateTime.UtcNow.Ticks;
            long lastId = Interlocked.Read(ref _lastId);

            // Extract previous timestamp from lastId
            long lastTimestamp = lastId >> 16;

            if (timestamp < lastTimestamp)
            {
                // System clock moved backwards → enforce monotonicity
                timestamp = lastTimestamp + 1;
                Interlocked.Exchange(ref _counter, 0);
            }
            else if (timestamp == lastTimestamp)
            {
                // Same timestamp → increment 16-bit counter
                uint count = Interlocked.Increment(ref _counter);

                // If overflow (unlikely), advance timestamp by 1 tick
                if (count > 0xFFFF)
                {
                    timestamp = lastTimestamp + 1;
                    Interlocked.Exchange(ref _counter, 0);
                }
            }
            else
            {
                // Clock moved forward normally → reset counter
                Interlocked.Exchange(ref _counter, 0);
            }

            // Build ID: [ 48-bit timestamp | 16-bit counter ]
            long id = timestamp << 16 | _counter & 0xFFFF;

            // Save new id atomically
            Interlocked.Exchange(ref _lastId, id);

            return id;
        }

        /// <summary>Creates a monotonically increasing identifier and formats it as a string.</summary>
        /// <returns>The generated identifier in decimal form.</returns>
        public string CreateIdString() => CreateId().ToString();
    }
}
