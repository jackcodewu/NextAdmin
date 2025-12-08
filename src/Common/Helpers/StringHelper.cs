using System;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;

namespace NextAdmin.Common.Helpers
{
    /// <summary>
    /// String helper class
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Check if string is null or whitespace
        /// </summary>
        public static bool IsNullOrWhiteSpace(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Check if string is null or empty
        /// </summary>
        public static bool IsNullOrEmpty(string value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Truncate string
        /// </summary>
        public static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Remove whitespace characters from string
        /// </summary>
        public static string RemoveWhitespace(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return Regex.Replace(value, @"\s+", "");
        }

        /// <summary>
        /// Convert string to camelCase
        /// </summary>
        public static string ToCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var words = value.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder(words[0].ToLower());

            for (int i = 1; i < words.Length; i++)
            {
                result.Append(char.ToUpper(words[i][0]));
                if (words[i].Length > 1)
                {
                    result.Append(words[i].Substring(1).ToLower());
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert string to PascalCase
        /// </summary>
        public static string ToPascalCase(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var words = value.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();

            foreach (var word in words)
            {
                result.Append(char.ToUpper(word[0]));
                if (word.Length > 1)
                {
                    result.Append(word.Substring(1).ToLower());
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert string to snake_case
        /// </summary>
        public static string ToSnakeCase(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var result = new StringBuilder();
            result.Append(char.ToLower(value[0]));

            for (int i = 1; i < value.Length; i++)
            {
                if (char.IsUpper(value[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(value[i]));
                }
                else
                {
                    result.Append(value[i]);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert string to kebab-case
        /// </summary>
        public static string ToKebabCase(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var result = new StringBuilder();
            result.Append(char.ToLower(value[0]));

            for (int i = 1; i < value.Length; i++)
            {
                if (char.IsUpper(value[i]))
                {
                    result.Append('-');
                    result.Append(char.ToLower(value[i]));
                }
                else
                {
                    result.Append(value[i]);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Check if string contains Chinese characters
        /// </summary>
        public static bool ContainsChinese(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return Regex.IsMatch(value, @"[\u4e00-\u9fa5]");
        }

        /// <summary>
        /// Check if string is a valid email address
        /// </summary>
        public static bool IsValidEmail(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        /// <summary>
        /// Check if string is a valid phone number
        /// </summary>
        public static bool IsValidPhoneNumber(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return Regex.IsMatch(value, @"^1[3-9]\d{9}$");
        }

        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            return result.ToString();
        }

        public static string GenerateRandomNumberString(int length)
        {
            const string chars = "0123456789";
            var random = new Random();
            var result = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            return result.ToString();
        }

        public static string GenerateRandomAlphabetString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var random = new Random();
            var result = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            return result.ToString();
        }

        public static string GenerateRandomHexString(int length)
        {
            const string chars = "0123456789ABCDEF";
            var random = new Random();
            var result = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            return result.ToString();
        }

        public static string GenerateObjectId()
        {
            return ObjectId.GenerateNewId().ToString();
        }

        public static string GenerateShortObjectId()
        {
            return Convert.ToBase64String(ObjectId.GenerateNewId().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")
                .Substring(0, 22);
        }

        public static string MaskString(string input, int visibleChars)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (input.Length <= visibleChars) return input;
            return input.Substring(0, visibleChars) + new string('*', input.Length - visibleChars);
        }

        public static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return email;
            var parts = email.Split('@');
            if (parts.Length != 2) return email;
            return MaskString(parts[0], 2) + "@" + parts[1];
        }

        public static string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber)) return phoneNumber;
            return Regex.Replace(phoneNumber, @"(\d{3})\d{4}(\d{4})", "$1****$2");
        }

        public static ObjectId MaskIdCard(ObjectId idCard)
        {
            if (idCard==null) return idCard;
            return idCard;
        }

        public static string MaskBankCard(string bankCard)
        {
            if (string.IsNullOrEmpty(bankCard)) return bankCard;
            return Regex.Replace(bankCard, @"(\d{4})\d{8}(\d{4})", "$1********$2");
        }

        public static string RemoveHtmlTags(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return Regex.Replace(input, "<.*?>", string.Empty);
        }

        public static string RemoveSpecialCharacters(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return Regex.Replace(input, "[^0-9a-zA-Z]+", "");
        }

        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static bool IsValidIpAddress(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress)) return false;
            return Regex.IsMatch(ipAddress, @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
        }

        public static bool IsValidMacAddress(string macAddress)
        {
            if (string.IsNullOrEmpty(macAddress)) return false;
            return Regex.IsMatch(macAddress, @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$");
        }

        public static bool IsValidHexColor(string hexColor)
        {
            if (string.IsNullOrEmpty(hexColor)) return false;
            return Regex.IsMatch(hexColor, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
        }

        public static bool IsValidBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return false;
            try
            {
                Convert.FromBase64String(base64);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                System.Text.Json.JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidXml(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;
            try
            {
                System.Xml.Linq.XDocument.Parse(xml);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 
