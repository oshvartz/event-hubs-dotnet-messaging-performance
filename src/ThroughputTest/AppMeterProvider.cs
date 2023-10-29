using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThroughputTest
{
    public static class AppMeterProvider
    {
        public const string MeterName = "EventHub.ThroughputTest";

        private static readonly Meter InternalMeter = new(MeterName, "1.0");

        public static Meter AppMeter => InternalMeter;

    }
}
