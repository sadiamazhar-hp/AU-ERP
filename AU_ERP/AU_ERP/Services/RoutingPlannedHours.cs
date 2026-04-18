using AU_ERP.Models;

namespace AU_ERP.Services
{
    /// <summary>Convert routing operation machine + labor times (per work-centre time UOM) into a single hours value.</summary>
    public static class RoutingPlannedHours
    {
        /// <exception cref="InvalidOperationException">Time UOM is present but not Min/Hr/Day.</exception>
        public static decimal SumLineToHours(decimal? machineTime, decimal? laborTime, string? timeUom)
        {
            var m = machineTime ?? 0m;
            var l = laborTime ?? 0m;
            var sum = m + l;

            if (string.IsNullOrWhiteSpace(timeUom))
                return sum;

            var normalized = WorkCenterTimeUom.FromMeasurementCode(timeUom);
            if (normalized == null)
                throw new InvalidOperationException($"Routing line has unsupported time UOM '{timeUom}'. Use Min, Hr, or Day on the work centre.");

            if (normalized == WorkCenterTimeUom.Min)
                return sum / 60m;
            if (normalized == WorkCenterTimeUom.Day)
                return sum * 24m;
            return sum;
        }

        public static decimal SumHeaderPlannedHours(RoutingOperationHeaderSample header)
        {
            var ops = header.RoutingOperationsSamples ?? Array.Empty<RoutingOperationsSample>();
            decimal total = 0;
            foreach (var op in ops)
                total += SumLineToHours(op.MachineTime, op.LaborTime, op.TimeUom);
            return total;
        }
    }
}
