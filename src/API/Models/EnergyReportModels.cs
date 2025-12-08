using System;

namespace NextAdmin.API.Models
{
    /// <summary>
    /// Energy query request
    /// </summary>
    public class EnergyQueryRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? TargetDate { get; set; }
        public string? DeviceId { get; set; }
        public string? ProjectId { get; set; }
        public int? TopN { get; set; }
    }

    /// <summary>
    /// Energy cost analysis request
    /// </summary>
    public class EnergyCostRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? PricePerKwh { get; set; }
    }

    /// <summary>
    /// Energy comparison request (EnergyReportController specific)
    /// </summary>
    public class EnergyReportComparisonRequest
    {
        public DateTime? CurrentStartDate { get; set; }
        public DateTime? CurrentEndDate { get; set; }
        public DateTime? PreviousStartDate { get; set; }
        public DateTime? PreviousEndDate { get; set; }
    }
}
