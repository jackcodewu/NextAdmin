using System;

namespace NextAdmin.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToIso8601String(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        public static string ToLocalTimeString(this DateTime dateTime)
        {
            return dateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static bool IsWithinLastMinutes(this DateTime dateTime, int minutes)
        {
            return dateTime >= DateTime.UtcNow.AddMinutes(-minutes);
        }

        public static bool IsWithinLastHours(this DateTime dateTime, int hours)
        {
            return dateTime >= DateTime.UtcNow.AddHours(-hours);
        }

        public static bool IsWithinLastDays(this DateTime dateTime, int days)
        {
            return dateTime >= DateTime.UtcNow.AddDays(-days);
        }

        public static DateTime TruncateToSeconds(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
        }

        public static DateTime TruncateToMinutes(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);
        }

        public static DateTime TruncateToHours(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
                dateTime.Hour, 0, 0, dateTime.Kind);
        }

        public static DateTime TruncateToDays(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day,
                0, 0, 0, dateTime.Kind);
        }
    }
} 
