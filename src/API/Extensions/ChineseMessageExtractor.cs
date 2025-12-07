using System.Text.RegularExpressions;

namespace NextAdmin.API.Extensions
{
    public static class ChineseMessageExtractor
    {
        private static readonly Regex ChineseBlockRegex =
            new(@"[\u4e00-\u9fa5][\u4e00-\u9fa5，。；：、“”‘’？！（）【】《》〈〉—…·]*",
                RegexOptions.Compiled);

        public static string Extract(Exception ex)
        {
            if (ex == null) return "系统内部错误";

            // 组合多层异常消息
            var raw = FlattenExceptionMessage(ex);

            var segments = ChineseBlockRegex.Matches(raw)
                .Cast<Match>()
                .Select(m => m.Value)
                .Where(s => !string.IsNullOrWhiteSpace(s));

            var text = string.Join("", segments);
            return string.IsNullOrWhiteSpace(text) ? "操作失败,请检测提交的数据是否符合规范或名称等是否重复" : text;
        }

        private static string FlattenExceptionMessage(Exception ex)
        {
            var sb = new System.Text.StringBuilder();
            while (ex != null)
            {
                sb.Append(ex.Message).Append(' ');
                ex = ex.InnerException;
            }
            return sb.ToString();
        }
    }
}
