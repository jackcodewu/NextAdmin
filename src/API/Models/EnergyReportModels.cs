using System;

namespace NextAdmin.API.Models
{
    /// <summary>
    /// 能耗查询请求
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
    /// 电费分析请求
    /// </summary>
    public class EnergyCostRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? PricePerKwh { get; set; }
    }

    /// <summary>
    /// 能耗对比请求（EnergyReportController专用）
    /// </summary>
    public class EnergyReportComparisonRequest
    {
        public DateTime? CurrentStartDate { get; set; }
        public DateTime? CurrentEndDate { get; set; }
        public DateTime? PreviousStartDate { get; set; }
        public DateTime? PreviousEndDate { get; set; }
    }
}
