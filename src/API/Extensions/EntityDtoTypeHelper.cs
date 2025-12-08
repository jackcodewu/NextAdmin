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
            // 1. Get entity assembly and DTO assembly
            var entityAssembly = typeof(AggregateRoot).Assembly;
            var dtoAssembly = typeof(BaseDto).Assembly;

            // 2. Get all entity types (inherit from AggregateRoot, exclude abstract classes)
            var entityTypes = entityAssembly.GetTypes()
                .Where(t => t.BaseType == typeof(AggregateRoot) && !t.IsAbstract && t.IsClass);

            foreach (var entityType in entityTypes)
            {
                var entityName = entityType.Name;

                // 3. DTO naming convention
                //   - XxxDto
                //   - CreateXxxDto
                //   - UpdateXxxDto
                //   - XxxQueryDto
                //   - XxxesDto or XxxsDto or XxxListDto (batch DTO, need to be compatible with plural)

                // 3.1 Find BaseDto
                var baseDto = FindDtoType(dtoAssembly, entityName + "Dto");
                // 3.2 Find CreateDto
                var createDto = FindDtoType(dtoAssembly, "Create" + entityName + "Dto");
                // 3.3 Find UpdateDto
                var updateDto = FindDtoType(dtoAssembly, "Update" + entityName + "Dto");
                // 3.4 Find QueryDto
                var queryDto = FindDtoType(dtoAssembly, entityName + "QueryDto")
                            ?? FindDtoType(dtoAssembly, entityName + "QueryDto", true); // Compatible with different directories
                                                                                        // 3.5 Find BasesDto (plural, compatible with es/s/List)
                var basesDto = FindDtoType(dtoAssembly, entityName + "esDto")
                            ?? FindDtoType(dtoAssembly, entityName + "sDto")
                            ?? FindDtoType(dtoAssembly, entityName + "ListDto");

                // 4. Only return type combinations where all are found
                if (baseDto != null && createDto != null && updateDto != null && queryDto != null && basesDto != null)
                {
                    yield return (entityType, baseDto, createDto, updateDto, queryDto, basesDto);
                }
            }
        }

        private static Type? FindDtoType(Assembly dtoAssembly, string typeName, bool ignoreNamespace = false)
        {
            // Exact search
            var type = dtoAssembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
            if (type != null) return type;

            // Optional: ignore namespace search
            if (ignoreNamespace)
            {
                type = dtoAssembly.GetTypes().FirstOrDefault(t => t.Name.EndsWith(typeName));
            }
            return type;
        }
    }
}
