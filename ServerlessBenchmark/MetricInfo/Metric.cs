using System;

namespace ServerlessBenchmark.MetricInfo
{
    public sealed class Metric:IEquatable<Metric>
    {
        public string Name { get; private set; }
        public Metric(string metricName)
        {
            if (!string.IsNullOrEmpty(metricName))
            {
                Name = metricName;
            }
            else
            {
                throw new ArgumentNullException("metricName");
            }
        }

        public bool Equals(Metric other)
        {
            return Name.Equals(other.Name, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
