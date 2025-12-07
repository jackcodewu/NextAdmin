using System.Collections.Generic;

namespace NextAdmin.API.Models
{
    /// <summary>
    /// 描述遗留报表/图表接口的文档信息。
    /// </summary>
    public class LegacyReportEndpointInfo
    {
        /// <summary>
        /// PHP 接口 act 名称，例如 energyPeakBar。
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// 在 PHP 文档中记录的类型（函数/控制器等）。
        /// </summary>
        public string? PhpType { get; init; }

        /// <summary>
        /// PHP 文档中的功能描述。
        /// </summary>
        public string? PhpSummary { get; init; }

        /// <summary>
        /// 额外的手工维护描述（来自缺失清单）。
        /// </summary>
        public string? AdditionalSummary { get; init; }

        /// <summary>
        /// PHP 文档中的参数签名，例如 "t, r"。
        /// </summary>
        public string? ParameterSignature { get; init; }

        /// <summary>
        /// PHP 文档中的请求参数键列表描述。
        /// </summary>
        public string? RequestParameters { get; init; }

        /// <summary>
        /// PHP 文档中的来源文件。
        /// </summary>
        public string? SourceFile { get; init; }

        /// <summary>
        /// 前端调用该 act 的 Vue 组件/文件路径集合。
        /// </summary>
        public IReadOnlyCollection<string> FrontendEntries { get; init; } = new List<string>();

        /// <summary>
        /// 生成的文档锚点，例如 ./php_api.md#energyanalysisoneMonth。
        /// </summary>
        public string? PhpDocAnchor { get; init; }
    }
}
