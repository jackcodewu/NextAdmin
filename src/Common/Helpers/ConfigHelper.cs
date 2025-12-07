using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Common.Helpers
{
    public class ConfigHelper
    {
        public static void UpdateTenantId(string TenantId)
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
#if DEBUG
            appPath = appPath.Replace(@"bin\Debug\net9.0\", "");
#endif
            // 写入到 appsettings.json
            var appSettingsPath = Path.Combine(
                appPath,
                "appsettings.json"
            );

            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                var jObj = JObject.Parse(json);

                jObj["TenantId"] = TenantId;
                File.WriteAllText(
                    appSettingsPath,
                    jObj.ToString(Newtonsoft.Json.Formatting.Indented)
                );

                File.WriteAllText(
                     Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"),
                    jObj.ToString(Newtonsoft.Json.Formatting.Indented)
                );
            }
        }

        public static string? GetTenantId()
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
#if DEBUG
            appPath = appPath.Replace(@"bin\Debug\net9.0\", "");
#endif
            // 写入到 appsettings.json
            var appSettingsPath = Path.Combine(
                appPath,
                "appsettings.json"
            );

            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);
                var TenantId = jObj["TenantId"]?.ToString();
                return TenantId;
            }

            return null;
        }

        public static void UpdateSeedData(bool isSeed)
        {
            // 写回 appsettings.json
            var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                var jObj = JObject.Parse(json);
                jObj["SeedData"] = isSeed;
                File.WriteAllText(appSettingsPath, jObj.ToString(Newtonsoft.Json.Formatting.Indented));
            }
        }
    }
}
