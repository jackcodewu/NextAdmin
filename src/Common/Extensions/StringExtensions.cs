namespace NextAdmin.Common.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a string to camelCase.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The camelCase version of the string.</returns>
        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || !char.IsUpper(str[0]))
            {
                return str;
            }

            char[] chars = str.ToCharArray();
            chars[0] = char.ToLowerInvariant(chars[0]);
            return new string(chars);
        }
    }
} 
