namespace Analyzer
{
    public readonly struct ProfilerHistory
    {
        public readonly double[] times;
        public readonly int[] hits;

        public ProfilerHistory(int maxEntries)
        {
            times = new double[2000];
            hits = new int[2000];
        }

        public void AddMeasurement(double measurement, int HitCounter)
        {
            for (int i = times.Length - 1; i >= 0; i--)
            {
                if (i == 0)
                {
                    times[0] = measurement;
                    hits[0] = HitCounter;
                }
                else
                {
                    times[i] = times[i - 1];
                    hits[i] = hits[i - 1];
                }
            }
        }

        public double GetAverageTime(double count)
        {
            double sum = 0;

            for (int i = 0; i < count; i++)
            {
                sum += times[i];
            }

            return sum / count;
        }
    }
}