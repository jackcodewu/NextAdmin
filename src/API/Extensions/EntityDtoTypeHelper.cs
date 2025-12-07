namespace NextAdmin.API.Extensions
{
    using NextAdmin.Application.DTOs;
    using NextAdmin.Application.DTOs.Bases;
    using NextAdmin.Application.DTOs.Bases.QueryPages;
    using NextAdmin.Core.Domain.Entities;
    using System.Reflection;

    public static class EntityDtoTypeHelper
    {
        public static IEnumerable<(Type entity, Type baseDto, Type createDto, Type updateDto, Type queryDto, Type basesDto)> GetAllEntityDtoTypes()
        {
            // 1. 获取实体程序集和DTO程序集
            var entityAssembly = typeof(AggregateRoot).Assembly;
            var dtoAssembly = typeof(BaseDto).Assembly;

            // 2. 获取所有实体类型（继承自AggregateRoot，排除抽象类）
            var entityTypes = entityAssembly.GetTypes()
                .Where(t => t.BaseType == typeof(AggregateRoot) && !t.IsAbstract && t.IsClass);

            foreach (var entityType in entityTypes)
            {
                var entityName = entityType.Name;

                // 3. 约定DTO命名规则
                //   - XxxDto
                //   - CreateXxxDto
                //   - UpdateXxxDto
                //   - XxxQueryDto
                //   - XxxesDto 或 XxxsDto 或 XxxListDto（批量DTO，需兼容复数）

                // 3.1 查找BaseDto
                var baseDto = FindDtoType(dtoAssembly, entityName + "Dto");
                // 3.2 查找CreateDto
                var createDto = FindDtoType(dtoAssembly, "Create" + entityName + "Dto");
                // 3.3 查找UpdateDto
                var updateDto = FindDtoType(dtoAssembly, "Update" + entityName + "Dto");
                // 3.4 查找QueryDto
                var queryDto = FindDtoType(dtoAssembly, entityName + "QueryDto")
                            ?? FindDtoType(dtoAssembly, entityName + "QueryDto", true); // 兼容不同目录
                                                                                        // 3.5 查找BasesDto（复数，兼容es/s/List）
                var basesDto = FindDtoType(dtoAssembly, entityName + "esDto")
                            ?? FindDtoType(dtoAssembly, entityName + "sDto")
                            ?? FindDtoType(dtoAssembly, entityName + "ListDto");

                // 4. 只返回都找到的类型组合
                if (baseDto != null && createDto != null && updateDto != null && queryDto != null && basesDto != null)
                {
                    yield return (entityType, baseDto, createDto, updateDto, queryDto, basesDto);
                }
            }
        }

        private static Type? FindDtoType(Assembly dtoAssembly, string typeName, bool ignoreNamespace = false)
        {
            // 精确查找
            var type = dtoAssembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
            if (type != null) return type;

            // 可选：忽略命名空间查找
            if (ignoreNamespace)
            {
                type = dtoAssembly.GetTypes().FirstOrDefault(t => t.Name.EndsWith(typeName));
            }
            return type;
        }
    }
}
