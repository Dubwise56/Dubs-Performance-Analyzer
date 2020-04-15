using RimWorld;

namespace DubsAnalyzer
{
    class AlertInfo
    {
        public bool dirty = true;
        public bool changed = true;
        public AlertReport report = new AlertReport();

        public bool Dirty()
        {
            if (dirty)
            {
                this.dirty = false;
                this.changed = true;
                return true;
            }
            return false;
        }
        public void Update(AlertReport report)
        {
            this.report = report;
            this.changed = false;
        }
    }
}