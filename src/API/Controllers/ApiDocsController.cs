using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Xml.Linq;
using NextAdmin.Application.DTOs;

namespace NextAdmin.API.Controllers
{
    [Route("api/docs")]
    [ApiController]
    public class ApiDocsController : ControllerBase
    {
        private readonly XDocument? _xmlDoc;

        public ApiDocsController()
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, "NextAdmin.API.xml");
            if (System.IO.File.Exists(xmlPath))
                _xmlDoc = XDocument.Load(xmlPath);
        }

        /// <summary>
        /// Get API documentation browser interface
        /// </summary>
        [HttpGet("ui")]
        public ContentResult GetUI()
        {
            var html = System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "wwwroot", "api-docs.html"));
            return new ContentResult
            {
                ContentType = "text/html",
                Content = html
            };
        }

        [HttpGet("all")]
        public IActionResult GetAll()
        {
            var asm = Assembly.GetExecutingAssembly();
            var controllers = asm.GetTypes()
                .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
                .Select(ctrl => new
                {
                    Controller = ctrl.Name,
                    Summary = GetTypeSummary(ctrl),
                    Actions = ctrl.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Where(m => m.IsPublic && !m.IsSpecialName && m.GetCustomAttribute<NonActionAttribute>() == null)
                        .Select(act => new
                        {
                            Action = act.Name,
                            Summary = GetMethodSummary(act),
                            Route = GetRoute(ctrl, act),
                            HttpMethod = GetHttpMethod(act),
                            Parameters = act.GetParameters().Select(p => new
                            {
                                Name = p.Name,
                                Type = p.ParameterType.Name,
                                Summary = GetParameterSummary(act, p)
                            })
                        })
                }).ToList();

            return Ok(ApiResponse<object>.SuccessResponse(controllers, "API documentation retrieved successfully"));
        }

        private string GetTypeSummary(Type type)
        {
            if (_xmlDoc == null) return "";
            var memberName = $"T:{type.FullName}";
            var node = _xmlDoc.Descendants("member").FirstOrDefault(x => x.Attribute("name")?.Value == memberName);
            return node?.Element("summary")?.Value.Trim() ?? "";
        }

        private string GetMethodSummary(MethodInfo method)
        {
            if (_xmlDoc == null || method.DeclaringType == null) return "";
            var paramTypes = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
            var memberName = $"M:{method.DeclaringType.FullName}.{method.Name}";
            if (method.GetParameters().Any())
                memberName += $"({paramTypes})";
            var node = _xmlDoc.Descendants("member").FirstOrDefault(x => x.Attribute("name")?.Value.StartsWith(memberName) == true);
            return node?.Element("summary")?.Value.Trim() ?? "";
        }

        private string GetParameterSummary(MethodInfo method, ParameterInfo param)
        {
            if (_xmlDoc == null || method.DeclaringType == null) return "";
            var paramTypes = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
            var memberName = $"M:{method.DeclaringType.FullName}.{method.Name}";
            if (method.GetParameters().Any())
                memberName += $"({paramTypes})";
            var node = _xmlDoc.Descendants("member").FirstOrDefault(x => x.Attribute("name")?.Value.StartsWith(memberName) == true);
            return node?.Elements("param").FirstOrDefault(e => e.Attribute("name")?.Value == param.Name)?.Value.Trim() ?? "";
        }

        private string GetRoute(Type ctrl, MethodInfo act)
        {
            // Get Route attributes from controller and method
            var ctrlRoute = ctrl.GetCustomAttribute<RouteAttribute>()?.Template ?? $"api/{ctrl.Name.Replace("Controller", "")}";
            var actRoute = act.GetCustomAttribute<RouteAttribute>()?.Template;
            if (!string.IsNullOrEmpty(actRoute))
                return ctrlRoute.TrimEnd('/') + "/" + actRoute.TrimStart('/');
            return ctrlRoute;
        }

        private string GetHttpMethod(MethodInfo act)
        {
            if (act.GetCustomAttribute<HttpGetAttribute>() != null) return "GET";
            if (act.GetCustomAttribute<HttpPostAttribute>() != null) return "POST";
            if (act.GetCustomAttribute<HttpPutAttribute>() != null) return "PUT";
            if (act.GetCustomAttribute<HttpDeleteAttribute>() != null) return "DELETE";
            return "GET";
        }
    }
}
