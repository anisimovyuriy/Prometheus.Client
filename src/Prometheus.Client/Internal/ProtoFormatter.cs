﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Prometheus.Client.Contracts;
using ProtoBuf;

namespace Prometheus.Client.Internal
{
    internal static class ProtoFormatter
    {
        public static void Format(Stream destination, IEnumerable<CMetricFamily> metrics)
        {
            var metricFamilys = metrics.ToArray();
            foreach (var metricFamily in metricFamilys)
            {
                Serializer.SerializeWithLengthPrefix(destination, metricFamily, PrefixStyle.Base128, 0);
            }
        }
    }
}