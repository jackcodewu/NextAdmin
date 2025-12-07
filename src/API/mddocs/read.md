using EasyEnglish.API.Controllers; // 1. 引入了我们定义的 BaseController，这是生成API的模板。
using EasyEnglish.Application.Interfaces; // 2. 引入了 IAppService 接口，这是我们要侦测的目标。
using Microsoft.AspNetCore.Mvc.ApplicationParts; // 3. 引入ASP.NET Core的“应用部件”概念，我们的代码就是其中一部分。
using Microsoft.AspNetCore.Mvc.Controllers; // 4. 引入 ControllerFeature，这是ASP.NET Core用来存放所有已发现控制器的列表。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; // 5. 引入反射，这是动态检查和创建类型的核心工具。

namespace EasyEnglish.API.Extensions
{
    // 6. 定义一个类，它实现了 IApplicationFeatureProvider<ControllerFeature> 接口。
    //    这告诉ASP.NET Core：“我是一个特性提供程序，专门负责为你的‘控制器列表(ControllerFeature)’提供额外的内容”。
    public class DynamicControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        // 7. 实现接口所要求的方法 PopulateFeature。
        //    ASP.NET Core在启动并扫描控制器时，会自动调用这个方法。
        //    参数 parts: 包含了应用程序的所有组成部分（比如项目本身、引用的库等）。
        //    参数 feature: 就是那个需要我们来填充的“控制器列表”。
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            // 8. 核心查询开始：在应用程序的所有部件(parts)中进行查找。
            var appServiceTypes = parts
                // 9. OfType<IApplicationPartTypeProvider>(): 筛选出那些能提供类型信息(代码)的部件。
                .OfType<IApplicationPartTypeProvider>()
                // 10. SelectMany(p => p.Types): 从每个部件中，获取它包含的所有类型（类、接口等），并将它们全部平铺到一个大列表里。
                .SelectMany(p => p.Types)
                // 11. Where(...): 对所有找到的类型进行筛选，留下符合我们条件的。
                .Where(t => 
                    t.IsClass && !t.IsAbstract && // 12. 条件一：这个类型必须是一个具体的类（不是接口或抽象类）。比如 AppVersionService 就是一个具体的类。
                    t.GetInterfaces().Any(i => // 13. 条件二：这个类实现的接口中，至少有一个满足下面的子条件。
                        i.IsGenericType && // 14. 子条件a：该接口必须是泛型接口，比如 IAppService<T, ...>。
                        i.GetGenericTypeDefinition() == typeof(IAppService<,,,,,>))) // 15. 子条件b：该泛型接口的“开放”定义必须是 IAppService<,,,,,>。
                                                                                      // 这能精确地匹配 IAppService<AppVersion, AppVersionDto, ...> 这样的接口，而排除了其他泛型接口。
                .ToList(); // 16. ToList(): 将所有找到的符合条件的服务类型（如 AppVersionService 类型本身）收集到一个列表中。

            // 17. 遍历我们刚刚找到的所有服务类型。
            foreach (var appServiceType in appServiceTypes)
            {
                // 18. 从当前服务类型（如 AppVersionService）所实现的所有接口中，找到那个我们关心的 IAppService<...> 接口。
                var interfaceType = appServiceType.GetInterfaces().First(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IAppService<,,,,,>));

                // 19. GetGenericArguments(): 从找到的具体接口 IAppService<AppVersion, AppVersionDto, ...> 中，提取出它的泛型参数。
                var genericArgs = interfaceType.GetGenericArguments();
                // 20. 结果会是: [AppVersion, AppVersionDto, CreateAppVersionDto, UpdateAppVersionDto, AppVersionQueryDto]
                
                var entityType = genericArgs[0];    // 21. 第一个参数是实体类型 (AppVersion)
                var baseDtoType = genericArgs[1];   // 22. 第二个是基础DTO类型 (AppVersionDto)
                var createDtoType = genericArgs[2]; // 23. ...以此类推
                var updateDtoType = genericArgs[3];
                var queryDtoType = genericArgs[4];

                // 25. 动态构建控制器的关键步骤！
                //    获取我们的模板控制器 BaseController<,,,,,> 的“开放”泛型定义。
                var controllerType = typeof(BaseController<,,,,,>)
                    // 26. .MakeGenericType(...): 用我们刚刚提取出的具体类型 (AppVersion, AppVersionDto, ...) 填充到模板中。
                    .MakeGenericType(entityType, baseDtoType, createDtoType, updateDtoType, queryDtoType)
                    // 27. 这一步之后，我们就得到了一个全新的、具体的控制器类型：BaseController<AppVersion, AppVersionDto, ...>。
                    .GetTypeInfo(); // 28. .GetTypeInfo(): 获取这个新类型的类型信息。

                // 29. 检查一下ASP.NET Core默认发现的控制器列表里，是否已经包含了我们刚创建的这个类型。
                //     这可以防止重复添加。
                if (!feature.Controllers.Any(c => c.AsType() == controllerType))
                {
                    // 30. 如果不存在，就把它添加到控制器列表(feature)中。
                    feature.Controllers.Add(controllerType);
                }
            }
        }
    }
}


using Microsoft.AspNetCore.Mvc; // 1. 引入了 [Route] 和 [ApiController] 这些核心特性。
using Microsoft.AspNetCore.Mvc.ApplicationModels; // 2. 引入了ASP.NET Core的“应用模型”，它允许我们在程序启动时检查和修改控制器的行为。
using System;
using System.Linq;

namespace EasyEnglish.API.Extensions
{
    // 3. 定义一个类，它实现了 IControllerModelConvention 接口。
    //    这告诉ASP.NET Core：“我是一个控制器约定，请在发现每个控制器后，都用我来对它进行一番配置”。
    public class DynamicControllerRouteConvention : IControllerModelConvention
    {
        // 4. 实现接口所要求的方法 Apply。
        //    ASP.NET Core在启动并发现每一个控制器（无论是动态的还是手写的）后，都会调用一次这个方法。
        //    参数 controller: 代表当前正被处理的那个控制器模型（比如 MenuController 的模型）。
        public void Apply(ControllerModel controller)
        {
            var controllerType = controller.ControllerType; // 5. 获取当前控制器模型的具体类型，例如 typeof(MenuController)。

            // 6. 核心筛选逻辑：我们只希望这个约定对我们特定的控制器起作用。
            //    这个 if 判断语句检查当前控制器是否是我们继承自 BaseController<...> 的控制器。
            if (!controllerType.IsGenericType || 
                controllerType.GetGenericTypeDefinition() != typeof(Controllers.BaseController<,,,,,>))
            {
                // 7. 如果一个控制器（比如您手写的 AuthController）不满足这个条件，
                //    就直接 return，不对它做任何修改，保持其原有的 [Route] 和行为。
                return;
            }

            // --- 如果程序能执行到这里，说明当前的控制器是我们关心的动态控制器或继承自BaseController的手写控制器 ---

            // 8. 从控制器的泛型参数列表中，获取第一个参数，也就是我们的实体类型。
            //    例如，对于 BaseController<Menu, MenuDto, ...>，这里会得到 typeof(Menu)。
            var entityType = controllerType.GetGenericArguments().First(); 
            var entityName = entityType.Name; // 9. 获取实体类型的名称，例如 "Menu"。

            // 10. 设置控制器的名称。这主要用于路由生成和API文档。
            //     例如，将 MenuController 的 ControllerName 设置为 "Menu"。
            controller.ControllerName = entityName;

            // 11. 构造路由模板。
            //     例如，对于 "Menu" 实体，这里会生成字符串 "api/menu"。
            var routeTemplate = $"api/{entityName.ToLower()}"; 
            
            // 12. 为控制器添加一个新的“选择器”(Selector)。
            //     你可以把选择器理解为控制器的一套路由规则。
            controller.Selectors.Add(new SelectorModel
            {
                // 13. 为这个选择器设置“特性路由模型”(AttributeRouteModel)。
                //     这等同于我们在代码里手动给控制器加上 [Route("api/menu")] 特性。
                AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(routeTemplate))
            });

            // 14. 为控制器添加一个过滤器(Filter)。
            //     在这里，我们添加的是 ApiControllerAttribute 的一个实例。
            //     这等同于我们手动给控制器加上 [ApiController] 特性，
            //     它会自动启用模型验证、自动返回400错误等一系列便利的API行为。
            controller.Filters.Add(new ApiControllerAttribute());
        }
    }
}