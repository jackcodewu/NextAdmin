using System.Collections.Generic;

namespace NextAdmin.API.Models
{
    /// <summary>
    /// Describes documentation information for legacy report/chart endpoints.
    /// </summary>
    public class LegacyReportEndpointInfo
    {
        /// <summary>
        /// PHP interface act name, e.g. energyPeakBar.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Type recorded in PHP documentation (function/controller etc.).
        /// </summary>
        public string? PhpType { get; init; }

        /// <summary>
        /// Feature description in PHP documentation.
        /// </summary>
        public string? PhpSummary { get; init; }

        /// <summary>
        /// Additional manually maintained description (from missing list).
        /// </summary>
        public string? AdditionalSummary { get; init; }

        /// <summary>
        /// Parameter signature in PHP documentation, e.g. "t, r".
        /// </summary>
        public string? ParameterSignature { get; init; }

        /// <summary>
        /// Request parameter keys list description in PHP documentation.
        /// </summary>
        public string? RequestParameters { get; init; }

        /// <summary>
        /// Source file in PHP documentation.
        /// </summary>
        public string? SourceFile { get; init; }

        /// <summary>
        /// Frontend Vue component/file path collection calling this act.
        /// </summary>
        public IReadOnlyCollection<string> FrontendEntries { get; init; } = new List<string>();

        /// <summary>
        /// Generated documentation anchor, e.g. ./php_api.md#energyanalysisoneMonth.
        /// </summary>
        public string? PhpDocAnchor { get; init; }
    }
}
