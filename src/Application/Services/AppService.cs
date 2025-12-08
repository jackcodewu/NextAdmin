using AutoMapper;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.Interfaces;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Interfaces.Repositories;
using NextAdmin.Log;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Bson;
using MongoDB.Driver;
using SharpCompress.Common;
using System.ComponentModel.Design;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NextAdmin.Application.Services
{
    /// <summary>
    /// Generic application service base class that encapsulates common MongoRepository operations with caching support
    /// </summary>
    public class AppService<TEntity, TBaseDto, TCreateDto, TUpdateDto, TQueryDto, TBasesDto>
        : IAppService<TEntity, TBaseDto, TCreateDto, TUpdateDto, TQueryDto, TBasesDto>
        where TEntity : AggregateRoot
        where TBaseDto : BaseDto, new()
        where TCreateDto : CreateDto, new()
        where TUpdateDto : UpdateDto, new()
        where TQueryDto : QueryDto<TEntity>, new()
        where TBasesDto : BasesDto, new()
    {
        protected readonly IBaseRepository<TEntity> repo;
        protected readonly IMapper _mapper;
        protected readonly IHttpContextAccessor _httpContextAccessor;

        protected readonly string key;
        protected readonly bool _isCache;
        private readonly bool _isCommanyId;

        protected bool IsSystem => string.Equals(
                _httpContextAccessor.HttpContext?.User?.FindFirst("IsSystem")?.Value,
                "true",
                StringComparison.OrdinalIgnoreCase
            );

        protected ObjectId CurrentTenantId => GetTenantId();

        protected string TenantName => GetTenantName();

        protected string UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "";

        protected readonly ObjectId UserId;

        public AppService(
            IBaseRepository<TEntity> repo,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            string? key = null,
            bool isCommanyId = false,
            bool isCache = true
        )
        {
            this.repo = repo;
            this.key = string.IsNullOrWhiteSpace(key) ? typeof(TEntity).Name : key;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;

            _isCommanyId = isCommanyId;
            _isCache = isCache;

            ObjectId.TryParse(_httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out UserId);

        }

        public ObjectId GetTenantId(string? inputTenantId = null)
        {
            if (_isCommanyId)
                return ObjectId.Empty;

            // Super admin: Allow specifying TenantId, otherwise return Empty
            if (
                !string.IsNullOrEmpty(inputTenantId)
                && ObjectId.TryParse(inputTenantId, out var cid)
            )
                return cid;

            // Regular user: Always use their own
            ObjectId.TryParse(
                _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value,
                out ObjectId TenantId
            );

            if (TenantId == ObjectId.Empty)
                throw new Exception("Tenant is empty");

            return TenantId;
        }

        public string GetTenantName(string? inputTenantId = null)
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("TenantName")?.Value ?? string.Empty;
        }

        // 2. Add
        /// <summary>
        /// Add entity with caching support
        /// </summary>
        public virtual async Task<TBaseDto> AddAsync(TEntity entity)
        {
            try
            {
                if (UserId != ObjectId.Empty)
                {
                    entity.SetCreatedById(UserId);
                    entity.SetUpdatedById(UserId);
                }
                entity.SetCreatedByName(UserName);
                entity.SetUpdatedByName(UserName);
                entity = await repo.AddAsync(entity);
                var dto = _mapper.Map<TBaseDto>(entity);

                if (_isCache)
                {
                    var cacheKey = $"{key}:{dto.Id}";
                    await repo.SetCacheObjectAsync(cacheKey, dto);
                }

                return dto;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, ex.Message);
                throw new Exception(ex.Message, ex);
            }
        }

        public virtual async Task<TBaseDto> AddAsync(
            TCreateDto createDto)
        {
            try
            {
                var entity = _mapper.Map<TEntity>(createDto);
                if (UserId != ObjectId.Empty)
                {
                    entity.SetCreatedById(UserId);
                    entity.SetUpdatedById(UserId);
                }
                entity.SetCreatedByName(UserName);
                entity.SetUpdatedByName(UserName);
                entity = await repo.AddAsync(entity);
                var dto = _mapper.Map<TBaseDto>(entity);

                // Cache single DTO after adding
                if (_isCache)
                {
                    var cacheKey = GenerateCacheKey(dto.Id);
                    await repo.SetCacheObjectAsync(cacheKey, dto);
                }

                return dto;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, ex.Message);
                throw new Exception(ex.Message, ex);
            }
        }

        /// <summary>
        /// Add multiple entities
        /// </summary>
        public virtual async Task<int> AddManyAsync(
            List<TCreateDto> createDtos)
        {
            try
            {
                if (createDtos == null || createDtos.Count == 0)
                    return 0;
                var entities = _mapper.Map<List<TEntity>>(createDtos);
                entities.ForEach((e) =>
               {
                   if (UserId != ObjectId.Empty)
                   {
                       e.SetCreatedById(UserId);
                       e.SetUpdatedById(UserId);
                   }
                   e.SetCreatedByName(UserName);
                   e.SetUpdatedByName(UserName);
               });
                await repo.AddManyAsync(entities);
                
                // Clear list cache after batch add (don't cache individually to avoid memory usage)
                if (_isCache)
                {
                    await DelCache($"{key}:list:");
                    await DelCache($"{key}:page:");
                    await DelCache($"{key}:options:");
                }
                
                return entities.Count;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, ex.Message);
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task<int> AddManyAsync(List<TEntity> entities)
        {
            try
            {
                entities.ForEach((e) =>
                {
                    if (UserId != ObjectId.Empty)
                    {
                        e.SetCreatedById(UserId);
                        e.SetUpdatedById(UserId);
                    }
                    e.SetCreatedByName(UserName);
                    e.SetUpdatedByName(UserName);
                });
                await repo.AddManyAsync(entities);

                // Clear list cache after batch add (don't cache individually to avoid memory usage)
                if (_isCache)
                {
                    await DelCache($"{key}:list:");
                    await DelCache($"{key}:page:");
                    await DelCache($"{key}:options:");
                }

                return entities.Count;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, ex.Message);
                throw new Exception(ex.Message, ex);
            }
        }


        // 3. Update
        /// <summary>
        /// Update entity with caching support
        /// </summary>
        public virtual async Task<ApiResponse> UpdateAsync(
            TUpdateDto updateDto)
        {
            try
            {
                var entity = _mapper.Map<TEntity>(updateDto);
                entity.SetUpdatedByName(UserName);
                entity.SetUpdatedById(UserId);
                entity.SetUpdateTime();

                var result = await repo.UpdateAsync(entity);

                if (result && _isCache)
                {
                    // Update single DTO cache
                    var cacheKey = GenerateCacheKey(updateDto.Id);
                    var dto = _mapper.Map<TBaseDto>(entity);
                    if (dto is not null)
                        await repo.SetCacheObjectAsync(cacheKey, dto);
                    
                    // Clear list-related cache (because it may affect list data)
                    await DelCache($"{key}:list:");
                    await DelCache($"{key}:page:");
                    await DelCache($"{key}:query:");
                }

                return result 
                    ? ApiResponse.SuccessResponse("Update successful") 
                    : ApiResponse.ErrorResponse("500", "Update failed");
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, ex.Message);
                return ApiResponse.ErrorResponse("500", $"Update failed: {ex.Message}");
            }
        }
        public async Task<bool> UpdateAsync(TEntity entity)
        {
            try
            {
                entity.SetUpdatedByName(UserName);
                entity.SetUpdatedById(UserId);
                entity.SetUpdateTime();

                var result = await repo.UpdateAsync(entity);

                if (result && _isCache)
                {
                    // Update single DTO cache
                    var cacheKey = GenerateCacheKey(entity.Id.ToString());
                    var dto = _mapper.Map<TBaseDto>(entity);
                    if (dto is not null)
                        await repo.SetCacheObjectAsync(cacheKey, dto);

                    // Clear list-related cache (because it may affect list data)
                    await DelCache($"{key}:list:");
                    await DelCache($"{key}:page:");
                    await DelCache($"{key}:query:");
                }

                return result;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, ex.Message);
                throw new Exception(ex.Message, ex);
            }
        }


        public async Task UpdateManyAsync(List<TEntity> entities)
        {
            try
            {
                entities.ForEach((e) =>
                {
                    e.SetUpdatedByName(UserName);
                    e.SetUpdatedById(UserId);
                    e.SetUpdateTime();
                });
                await repo.UpdateManyAsync(entities);
                await DelCache($"{key}:list:");
                await DelCache($"{key}:page:");
                await DelCache($"{key}:query:");
            }
            catch (Exception ex)
            {

                LogHelper.Error(ex, ex.Message);
                throw new Exception(ex.Message, ex);
            }
        }

        // 1. Get
        /// <summary>
        /// Get entity by Id with caching support
        /// </summary>
        public virtual async Task<TBaseDto> GetAsync(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
                throw new ArgumentException("Invalid ObjectId format", nameof(id));

            // Strategy: Cache DTO for single queries (high frequency access)
            if (_isCache)
            {
                var cacheKey = GenerateCacheKey(id);
                var cachedDto = await repo.GetCacheObjectAsync<TBaseDto>(cacheKey);
                if (cachedDto is not null)
                    return cachedDto;

                var entityFromDb = await repo.GetByIdAsync(objectId);
                if (entityFromDb is null)
                    return default!;

                var dto = _mapper.Map<TBaseDto>(entityFromDb);
                if (dto is not null)
                    await repo.SetCacheObjectAsync(cacheKey, dto);

                return dto ?? new TBaseDto();
            }

            // Caching not enabled, query database directly
            var entity = await repo.GetByIdAsync(objectId);
            return entity is null ? default! : _mapper.Map<TBaseDto>(entity);
        }

        // 1. Get
        /// <summary>
        /// Get entity by Id with caching support
        /// </summary>
        public virtual async Task<TEntity> GetAsync(ObjectId id)
        {
            // Strategy: Cache Entity for entity queries
            if (_isCache)
            {
                var cacheKey = GenerateCacheKey($"entity:{id}");
                var cache = await repo.GetCacheObjectAsync<TEntity>(cacheKey);
                if (cache is not null)
                    return cache;

                var entityFromDb = await repo.GetByIdAsync(id);
                if (entityFromDb is null)
                    return default!;

                await repo.SetCacheObjectAsync(cacheKey, entityFromDb);
                return entityFromDb;
            }

            // Caching not enabled, query database directly
            var entity = await repo.GetByIdAsync(id);
            return entity;
        }

        /// <summary>
        /// Get single entity DTO (with conditions)
        /// </summary>
        public virtual async Task<TBaseDto> GetOneAsync(TQueryDto TQueryDto)
        {
            // Strategy: Cache DTO for single queries (high frequency access)
            if (_isCache)
            {
                var cacheKey = GenerateQueryCacheKey(TQueryDto, "dto");
                
                // Try to get DTO from cache
                var cachedDto = await repo.GetCacheObjectAsync<TBaseDto>(cacheKey);
                if (cachedDto is not null)
                    return cachedDto;
                
                // Cache miss, query database
                var entity = await repo.GetOneAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId));
                if (entity is null)
                    return default!;
                
                var dto = _mapper.Map<TBaseDto>(entity);
                await repo.SetCacheObjectAsync(cacheKey, dto);
                return dto;
            }

            // Caching not enabled, query database directly
            var one = await repo.GetOneAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId));
            return one is null ? default! : _mapper.Map<TBaseDto>(one);
        }

        /// <summary>
        /// Get single entity (with conditions)
        /// </summary>
        public virtual async Task<TEntity> GetOneEntityAsync(TQueryDto TQueryDto)
        {
            // Strategy: Cache Entity for entity queries
            if (_isCache)
            {
                var cacheKey = GenerateQueryCacheKey(TQueryDto, "entity");
                
                // Try to get Entity from cache
                var cachedEntity = await repo.GetCacheObjectAsync<TEntity>(cacheKey);
                if (cachedEntity is not null)
                    return cachedEntity;
                
                // Cache miss, query database
                var entity = await repo.GetOneAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId));
                if (entity is not null)
                {
                    await repo.SetCacheObjectAsync(cacheKey, entity);
                }
                return entity!;
            }

            // Caching not enabled, query database directly
            return (await repo.GetOneAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId)))!;
        }

        /// <summary>
        /// Query entities with conditions
        /// </summary>
        public virtual async Task<List<TBasesDto>> GetsAsync(TQueryDto TQueryDto, string sortField = "CreateTime", bool isAsc = false)
        {
            // Strategy: Cache DTO for list queries (avoid repeated mapping)
            if (_isCache)
            {
                var cacheKey = GenerateQueryCacheKey(TQueryDto, $"list:dto:{sortField}:{isAsc}");
                
                // Try to get DTO from cache
                var cachedDtos = await repo.GetCacheObjectAsync<List<TBasesDto>>(cacheKey);
                if (cachedDtos?.Any() == true)
                    return cachedDtos;

                // Cache miss, query database
                // Try to project directly to TBasesDto at database layer (avoid fetching full entity)
                ProjectionDefinition<TEntity, TBasesDto>? projection = null;
                try
                {
                    projection = CreateProjectionFromDto(sortField);
                }
                catch
                {
                    projection = null;
                }

                List<TBasesDto> dtos;
                if (projection != null)
                {
                    var projected = await repo.GetAsync<TBasesDto>(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId), projection, sortField, isAsc);
                    dtos = projected ?? new List<TBasesDto>();
                }
                else
                {
                    var entities = await repo.GetAsync(
                        TQueryDto.ToExpression(), 
                        GetTenantId(TQueryDto.TenantId),
                        sortField, 
                        isAsc);

                    if (entities?.Any() != true)
                        return new List<TBasesDto>();

                    // Map to DTO
                    dtos = _mapper.Map<List<TBasesDto>>(entities);
                }

                // Cache DTO (instead of Entity)
                if (dtos?.Any() == true)
                    await repo.SetCacheObjectAsync(cacheKey, dtos);

                return dtos;
            }

            // Caching not enabled, query database directly
            var result = await repo.GetAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId), sortField, isAsc);
            return result?.Any() == true ? _mapper.Map<List<TBasesDto>>(result) : new List<TBasesDto>();
        }

        public virtual async Task<List<TEntity>> GetsEntityAsync(TQueryDto TQueryDto, string sortField = "CreateTime", bool isAsc = false)
        {
            var entities = await repo.GetAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId), sortField, isAsc);
            return entities ?? new List<TEntity>();
        }

        /// <summary>
        /// Get all entities
        /// </summary>
        public virtual async Task<List<TBasesDto>> GetAllAsync()
        {
            if (_isCache)
            {
                var cacheKey = $"{key}:all";
                var cachedDtos = await repo.GetCacheObjectAsync<List<TBasesDto>>(cacheKey);
                if (cachedDtos is not null)
                    return cachedDtos;

                var entities = await repo.GetAllAsync(CurrentTenantId);
                var dtos = entities?.Any() == true ? _mapper.Map<List<TBasesDto>>(entities) : new List<TBasesDto>();
                
                // Cache DTO list
                await repo.SetCacheObjectAsync(cacheKey, dtos);
                return dtos;
            }
            else
            {
                var entities = await repo.GetAllAsync(CurrentTenantId);
                return entities?.Any() == true ? _mapper.Map<List<TBasesDto>>(entities) : new List<TBasesDto>();
            }
        } 
        
        /// <summary>
        /// Get all entities
        /// </summary>
        public virtual async Task<List<TEntity>> GetAllEntityAsync()
        {
            return await repo.GetAllAsync(CurrentTenantId) ?? new List<TEntity>();
        }

        /// <summary>
        /// Get all option data (no parameters)
        /// </summary>
        public virtual async Task<List<OptionDto>> GetOptionsAsync()
        {
            if (_isCache)
            {
                var cacheKey = $"{key}:options";
                var cachedOptions = await repo.GetCacheObjectAsync<List<OptionDto>>(cacheKey);
                if (cachedOptions is not null)
                    return cachedOptions;

                // Use projection to get Id and Name directly (performance optimization)
                var projection = CreateOptionProjection();
                List<OptionDto> options;

                if (projection != null)
                {
                    var projected = await repo.GetAsync<OptionDto>(
                        Builders<TEntity>.Filter.Empty,
                        CurrentTenantId,
                        projection,
                        "CreateTime",
                        false);
                    options = projected ?? new List<OptionDto>();
                }
                else
                {
                    // Fallback: Use full entity mapping
                    var entities = await repo.GetAllAsync(CurrentTenantId);
                    if (entities?.Any() != true)
                        return new List<OptionDto>();

                    options = entities.Select(x => new OptionDto { value = x.Id.ToString(), label = x.Name }).ToList();
                }
                
                // Cache option data (commonly used for dropdown lists)
                await repo.SetCacheObjectAsync(cacheKey, options);
                return options ?? new List<OptionDto>();
            }
            else
            {
                // Use projection to get Id and Name directly (performance optimization)
                var projection = CreateOptionProjection();
                if (projection != null)
                {
                    var projected = await repo.GetAsync<OptionDto>(
                        Builders<TEntity>.Filter.Empty,
                        CurrentTenantId,
                        projection,
                        "CreateTime",
                        false);
                    return projected ?? new List<OptionDto>();
                }

                // Fallback: Use full entity mapping
                var entities = await repo.GetAllAsync(CurrentTenantId);
                if (entities?.Any() != true)
                    return new List<OptionDto>();

                var options = entities.Select(x => new OptionDto { value = x.Id.ToString(), label = x.Name }).ToList();
                return options ?? new List<OptionDto>();
            }
        }

        /// <summary>
        /// Get option data (with query conditions and sorting)
        /// </summary>
        /// <param name="queryDto">Query conditions</param>
        /// <param name="sortField">Sort field</param>
        /// <param name="isAsc">Ascending order</param>
        /// <returns>Option data list</returns>
        public virtual async Task<List<OptionDto>> GetOptionsAsync(TQueryDto queryDto, string sortField = "CreateTime", bool isAsc = false)
        {
            // Strategy: Cache OptionDto for option queries (commonly used for dropdown lists, high frequency access)
            if (_isCache)
            {
                var cacheKey = GenerateQueryCacheKey(queryDto, $"options:dto:{sortField}:{isAsc}");
                
                // Try to get option data from cache
                var cachedOptions = await repo.GetCacheObjectAsync<List<OptionDto>>(cacheKey);
                if (cachedOptions is not null)
                    return cachedOptions;

                // Cache miss, use projection to query database
                var projection = CreateOptionProjection();
                List<OptionDto> options;

                if (projection != null)
                {
                    var projected = await repo.GetAsync<OptionDto>(
                        queryDto.ToExpression(),
                        GetTenantId(queryDto.TenantId),
                        projection,
                        sortField,
                        isAsc);
                    options = projected ?? new List<OptionDto>();
                }
                else
                {
                    // Fallback: Use full entity mapping
                    List<TEntity> entities = await repo.GetAsync(queryDto.ToExpression(), GetTenantId(queryDto.TenantId), sortField, isAsc);
                    options = entities?.Any() == true 
                        ? entities.Select(x => new OptionDto { value = x.Id.ToString(), label = x.Name }).ToList()
                        : new List<OptionDto>();
                }

                // Cache option data
                await repo.SetCacheObjectAsync(cacheKey, options);
                
                return options;
            }

            // Caching not enabled, use projection to query database directly
            var projectionNonCached = CreateOptionProjection();
            if (projectionNonCached != null)
            {
                var projected = await repo.GetAsync<OptionDto>(
                    queryDto.ToExpression(),
                    GetTenantId(queryDto.TenantId),
                    projectionNonCached,
                    sortField,
                    isAsc);
                return projected ?? new List<OptionDto>();
            }

            // Fallback: Use full entity mapping
            List<TEntity> result = await repo.GetAsync(queryDto.ToExpression(), GetTenantId(queryDto.TenantId), sortField, isAsc);
            return result?.Any() == true 
                ? result.Select(x => new OptionDto { value = x.Id.ToString(), label = x.Name }).ToList()
                : new List<OptionDto>();
        }


        /// <summary>
        /// Pagination query - automatic projection optimization (simplest calling method)
        /// Automatically creates projection based on TBasesDto properties without manual definition
        /// Recommended for list pages, better performance than AutoMapper approach
        /// </summary>
        public virtual async Task<PagedResultDto<TBasesDto>> GetListPageAsync(
            TQueryDto queryDto,
            int pageNumber,
            int pageSize,
            string sortField = "CreateTime",
            bool isAsc = false
        )
        {
            return await GetListPageWithProjectionAsync(
                queryDto,
                pageNumber,
                pageSize,
                null,  // Pass null, automatically create projection
                sortField,
                isAsc
            );
        }

        // ===================== AppService<T...> class =====================

        /// <summary>
        /// Pagination query - using MongoDB projection (recommended for list pages, best performance)
        /// Excludes large fields directly at database layer, returns lightweight DTO
        /// </summary>
        protected virtual async Task<PagedResultDto<TBasesDto>> GetListPageWithProjectionAsync(
            TQueryDto queryDto,
            int pageNumber,
            int pageSize,
            ProjectionDefinition<TEntity, TBasesDto>? projection = null,
            string sortField = "CreateTime",
            bool isAsc = false
        )
        {
            // ✅ When projection not provided, create based on TBasesDto and ensure cursor-required fields are included
            if (projection == null)
            {
                projection = CreateProjectionFromDto(sortField);
            }

            if (_isCache)
            {
                var projectionHash = projection?.ToString()?.GetHashCode().ToString() ?? "default";
                var cacheKey = GenerateQueryCacheKey(queryDto, $"page:projection:{projectionHash}:{pageNumber}:{pageSize}:{sortField}:{isAsc}");

                var cachedResult = await repo.GetCacheObjectAsync<PagedResultDto<TBasesDto>>(cacheKey);
                if (cachedResult is not null)
                    return cachedResult;

                var (items, total) = await repo.GetListPageAsync(
                    pageNumber,
                    pageSize,
                    queryDto.ToExpression(),
                    GetTenantId(queryDto.TenantId),
                    projection,
                    sortField,
                    isAsc);

                var pagedResult = new PagedResultDto<TBasesDto>(total, items ?? new List<TBasesDto>());
                await repo.SetCacheObjectAsync(cacheKey, pagedResult);
                return pagedResult;
            }

            var result = await repo.GetListPageAsync(
                pageNumber,
                pageSize,
                queryDto.ToExpression(),
                GetTenantId(queryDto.TenantId),
                projection,
                sortField,
                isAsc);

            return new PagedResultDto<TBasesDto>(result.Total, result.Items ?? new List<TBasesDto>());
        }

        /// <summary>
        /// Automatically create MongoDB projection based on TBasesDto properties
        /// Only includes properties defined in DTO and existing in entity; forcefully includes _id and sort field
        /// </summary>
        private ProjectionDefinition<TEntity, TBasesDto> CreateProjectionFromDto(string sortField = "CreateTime")
        {
            // 1) Get public readable property names of TBasesDto
            var dtoProperties = typeof(TBasesDto)
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p => p.Name)
                .ToList();

            // 2) Get property names of TEntity (for validating intersection)
            var entityProperties = typeof(TEntity)
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 3) Only keep fields that exist in entity
            var validFields = dtoProperties.Where(name => entityProperties.Contains(name)).ToList();

            if (validFields.Count == 0)
                return null!; // Calling code will fall back to default behavior

            // 4) Build BsonDocument projection
            var projectionDoc = new MongoDB.Bson.BsonDocument();

            foreach (var field in validFields)
            {
                // Regular C# property name (like CreateTime/UpdateTime/TenantName/Id etc)
                projectionDoc[field] = 1;
            }

            // ✅ 5) Forcefully include _id (Note: Mongo's primary key field name is "_id" not "Id")
            if (!projectionDoc.Contains("_id"))
            {
                projectionDoc["_id"] = 1;
            }

            // ✅ 6) Forcefully include sort field (if "Id" then map to "_id")
            var sortFieldBson = string.Equals(sortField, "Id", StringComparison.OrdinalIgnoreCase) ? "_id" : sortField;
            if (!projectionDoc.Contains(sortFieldBson))
            {
                projectionDoc[sortFieldBson] = 1;
            }

            // 7) Return projection definition
            return new BsonDocumentProjectionDefinition<TEntity, TBasesDto>(projectionDoc);
        }

        /// <summary>
        /// Create MongoDB projection for OptionDto
        /// Only includes Id and Name fields, used for dropdown lists and similar scenarios
        /// Maps MongoDB's _id to value and Name to label
        /// </summary>
        protected virtual ProjectionDefinition<TEntity, OptionDto>? CreateOptionProjection()
        {
            try
            {
                // Check if entity has Name property
                var entityProperties = typeof(TEntity)
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Select(p => p.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (!entityProperties.Contains("Name"))
                    return null; // Entity doesn't have Name property, cannot create projection

                // Build projection: _id as value, Name as label
                var projectionDoc = new MongoDB.Bson.BsonDocument
                {
                    { "value", "$_id" },  // Project _id as value
                    { "label", "$Name" }   // Project Name as label
                };

                return new BsonDocumentProjectionDefinition<TEntity, OptionDto>(projectionDoc);
            }
            catch
            {
                return null; // Projection creation failed, calling code will fall back to full entity query
            }
        }


        // 4. Delete
        /// <summary>
        /// Delete entity by Id
        /// </summary>
        public virtual async Task<ApiResponse> DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
                return ApiResponse.ErrorResponse("400", "Invalid ID format");

            try
            {
                var result = await repo.DeleteAsync(objectId);
                
                if (result && _isCache)
                {
                    // Delete single cache
                    var cacheKey = GenerateCacheKey(id);
                    await repo.RemoveCacheByPrefixAsync(cacheKey);
                    
                    // Clear list-related cache
                    await DelCache($"{key}:list:");
                    await DelCache($"{key}:page:");
                    await DelCache($"{key}:query:");
                    await DelCache($"{key}:options:");
                    await DelCache($"{key}:all");
                }
                
                return result 
                    ? ApiResponse.SuccessResponse("Deletion successful") 
                    : ApiResponse.ErrorResponse("500", "Deletion failed");
            }
            catch (Exception err)
            {
                LogHelper.Error($"Deletion failed: {err.Message}", err);
                return ApiResponse.ErrorResponse("500", $"Deletion failed: {err.Message}");
            }
        }

        // 5. Statistics
        /// <summary>
        /// Get entity count
        /// </summary>
        public virtual async Task<long> CountAsync(TQueryDto TQueryDto)
        {
                return await repo.CountAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId));
        }

        #region Cache helper methods


        /// <summary>
        /// Generate cache key
        /// </summary>
        protected string GenerateCacheKey(string suffix)
        {
            return $"{key}:{suffix}";
        }

        /// <summary>
        /// Delete cache by prefix
        /// </summary>
        /// <param name="prefix">Cache prefix; if null, deletes all cache with key prefix</param>
        protected async Task DelCache(string? prefix = null)
        {
            if (_isCache && repo.SupportsCaching)
            {
                var cachePrefix = string.IsNullOrEmpty(prefix) ? key : prefix;
                await repo.RemoveCacheByPrefixAsync($"{cachePrefix}*");
            }
        }

        /// <summary>
        /// Generate query cache key
        /// </summary>
        protected string GenerateQueryCacheKey(TQueryDto queryDto, string? suffix = null)
        {
            var hash = GenerateQueryDtoMD5(queryDto);
            return string.IsNullOrEmpty(suffix) 
                ? $"{key}:query:{hash}" 
                : $"{key}:query:{hash}:{suffix}";
        }

        /// <summary>
        /// JSON serialization options (for cache key generation)
        /// </summary>
        private static readonly JsonSerializerOptions CacheKeyJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Generate MD5 hash of query DTO
        /// </summary>
        private static string GenerateQueryDtoMD5(TQueryDto queryDto)
        {
            byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(queryDto, CacheKeyJsonOptions);
            byte[] hash = MD5.HashData(utf8);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        #endregion


        #region Archived code

        ///// <summary>
        ///// Pagination query - using Seek cursor pagination (supports MongoDB projection optimization)
        ///// Uses AutoMapper to map full entity by default, subclasses can override to use MongoDB projection
        ///// </summary>
        //public virtual async Task<PagedResultDto<TBasesDto>> GetListPageAsyncbak(
        //    TQueryDto queryDto,
        //    int pageNumber,
        //    int pageSize,
        //    string sortField,
        //    bool isAsc
        //)
        //{
        //    // Strategy: Cache PagedResultDto for pagination queries (includes total count and data list)
        //    if (_isCache)
        //    {
        //        var cacheKey = GenerateQueryCacheKey(queryDto, $"page:dto:{pageNumber}:{pageSize}:{sortField}:{isAsc}");

        //        // Try to get pagination result from cache
        //        var cachedResult = await repo.GetCacheObjectAsync<PagedResultDto<TBasesDto>>(cacheKey);
        //        if (cachedResult is not null)
        //            return cachedResult;

        //        // Cache miss, query database (using Seek pagination)
        //        (List<TEntity> items, long total) queryResult = await repo.GetListPageAsync(
        //                pageNumber,
        //                pageSize,
        //                queryDto.ToExpression(),
        //                GetTenantId(queryDto.TenantId),
        //                sortField,
        //                isAsc);

        //        var dtosResult = queryResult.items?.Any() == true
        //            ? _mapper.Map<List<TBasesDto>>(queryResult.items)
        //            : new List<TBasesDto>();

        //        var pagedResult = new PagedResultDto<TBasesDto>(queryResult.total, dtosResult);

        //        // Cache pagination result (includes total count and data)
        //        await repo.SetCacheObjectAsync(cacheKey, pagedResult);

        //        return pagedResult;
        //    }

        //    // Caching not enabled, query database directly
        //    (List<TEntity> items, long total) result = await repo.GetListPageAsync(
        //            pageNumber,
        //            pageSize,
        //            queryDto.ToExpression(),
        //            GetTenantId(queryDto.TenantId),
        //            sortField,
        //            isAsc);

        //    var dtos = result.items?.Any() == true ? _mapper.Map<List<TBasesDto>>(result.items) : new List<TBasesDto>();
        //    return new PagedResultDto<TBasesDto>(result.total, dtos);
        //}

        #endregion

    }
}
