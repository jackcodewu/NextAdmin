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
    /// 通用应用服务基类，封装MongoRepository的常用操作，支持缓存
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

            // 超级管理员：允许指定TenantId，否则返回Empty
            if (
                !string.IsNullOrEmpty(inputTenantId)
                && ObjectId.TryParse(inputTenantId, out var cid)
            )
                return cid;

            // 普通用户：始终用自己
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

        // 2. 添加
        /// <summary>
        /// 添加实体，支持缓存
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

                // 添加后缓存单条 DTO
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
        /// 批量添加实体
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
                
                // 批量添加后清除列表缓存（不单独缓存，避免内存占用）
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

                // 批量添加后清除列表缓存（不单独缓存，避免内存占用）
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


        // 3. 更新
        /// <summary>
        /// 更新实体，支持缓存
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
                    // 更新单条 DTO 缓存
                    var cacheKey = GenerateCacheKey(updateDto.Id);
                    var dto = _mapper.Map<TBaseDto>(entity);
                    if (dto is not null)
                        await repo.SetCacheObjectAsync(cacheKey, dto);
                    
                    // 清除列表相关缓存（因为可能影响列表数据）
                    await DelCache($"{key}:list:");
                    await DelCache($"{key}:page:");
                    await DelCache($"{key}:query:");
                }

                return result 
                    ? ApiResponse.SuccessResponse("更新成功") 
                    : ApiResponse.ErrorResponse("500", "更新失败");
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, ex.Message);
                return ApiResponse.ErrorResponse("500", $"更新失败: {ex.Message}");
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
                    // 更新单条 DTO 缓存
                    var cacheKey = GenerateCacheKey(entity.Id.ToString());
                    var dto = _mapper.Map<TBaseDto>(entity);
                    if (dto is not null)
                        await repo.SetCacheObjectAsync(cacheKey, dto);

                    // 清除列表相关缓存（因为可能影响列表数据）
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

        // 1. 获取
        /// <summary>
        /// 根据Id获取实体，支持缓存
        /// </summary>
        public virtual async Task<TBaseDto> GetAsync(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
                throw new ArgumentException("Invalid ObjectId format", nameof(id));

            // 策略：单条查询缓存 DTO（高频访问）
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

            // 未启用缓存，直接查询数据库
            var entity = await repo.GetByIdAsync(objectId);
            return entity is null ? default! : _mapper.Map<TBaseDto>(entity);
        }

        // 1. 获取
        /// <summary>
        /// 根据Id获取实体，支持缓存
        /// </summary>
        public virtual async Task<TEntity> GetAsync(ObjectId id)
        {
            // 策略：实体查询缓存 Entity
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

            // 未启用缓存，直接查询数据库
            var entity = await repo.GetByIdAsync(id);
            return entity;
        }

        /// <summary>
        /// 获取单个实体Dto（条件）
        /// </summary>
        public virtual async Task<TBaseDto> GetOneAsync(TQueryDto TQueryDto)
        {
            // 策略：单个查询缓存 DTO（高频访问）
            if (_isCache)
            {
                var cacheKey = GenerateQueryCacheKey(TQueryDto, "dto");
                
                // 尝试从缓存获取 DTO
                var cachedDto = await repo.GetCacheObjectAsync<TBaseDto>(cacheKey);
                if (cachedDto is not null)
                    return cachedDto;
                
                // 缓存未命中，查询数据库
                var entity = await repo.GetOneAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId));
                if (entity is null)
                    return default!;
                
                var dto = _mapper.Map<TBaseDto>(entity);
                await repo.SetCacheObjectAsync(cacheKey, dto);
                return dto;
            }

            // 未启用缓存，直接查询数据库
            var one = await repo.GetOneAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId));
            return one is null ? default! : _mapper.Map<TBaseDto>(one);
        }

        /// <summary>
        /// 获取单个实体（条件）
        /// </summary>
        public virtual async Task<TEntity> GetOneEntityAsync(TQueryDto TQueryDto)
        {
            // 策略：实体查询缓存 Entity
            if (_isCache)
            {
                var cacheKey = GenerateQueryCacheKey(TQueryDto, "entity");
                
                // 尝试从缓存获取 Entity
                var cachedEntity = await repo.GetCacheObjectAsync<TEntity>(cacheKey);
                if (cachedEntity is not null)
                    return cachedEntity;
                
                // 缓存未命中，查询数据库
                var entity = await repo.GetOneAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId));
                if (entity is not null)
                {
                    await repo.SetCacheObjectAsync(cacheKey, entity);
                }
                return entity!;
            }

            // 未启用缓存，直接查询数据库
            return (await repo.GetOneAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId)))!;
        }

        /// <summary>
        /// 条件查询实体
        /// </summary>
        public virtual async Task<List<TBasesDto>> GetsAsync(TQueryDto TQueryDto, string sortField = "CreateTime", bool isAsc = false)
        {
            // 策略：列表查询缓存 DTO（避免重复映射）
            if (_isCache)
            {
                var cacheKey = GenerateQueryCacheKey(TQueryDto, $"list:dto:{sortField}:{isAsc}");
                
                // 尝试从缓存获取 DTO
                var cachedDtos = await repo.GetCacheObjectAsync<List<TBasesDto>>(cacheKey);
                if (cachedDtos?.Any() == true)
                    return cachedDtos;

                // 缓存未命中，查询数据库
                // 尝试直接在数据库层投影为 TBasesDto（避免拉取完整实体）
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

                    // 映射为 DTO
                    dtos = _mapper.Map<List<TBasesDto>>(entities);
                }

                // 缓存 DTO（而非 Entity）
                if (dtos?.Any() == true)
                    await repo.SetCacheObjectAsync(cacheKey, dtos);

                return dtos;
            }

            // 未启用缓存，直接查询数据库
            var result = await repo.GetAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId), sortField, isAsc);
            return result?.Any() == true ? _mapper.Map<List<TBasesDto>>(result) : new List<TBasesDto>();
        }

        public virtual async Task<List<TEntity>> GetsEntityAsync(TQueryDto TQueryDto, string sortField = "CreateTime", bool isAsc = false)
        {
            var entities = await repo.GetAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId), sortField, isAsc);
            return entities ?? new List<TEntity>();
        }

        /// <summary>
        /// 获取所有实体
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
                
                // 缓存 DTO 列表
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
        /// 获取所有实体
        /// </summary>
        public virtual async Task<List<TEntity>> GetAllEntityAsync()
        {
            return await repo.GetAllAsync(CurrentTenantId) ?? new List<TEntity>();
        }

        /// <summary>
        /// 获取所有选项数据（无参数）
        /// </summary>
        public virtual async Task<List<OptionDto>> GetOptionsAsync()
        {
            if (_isCache)
            {
                var cacheKey = $"{key}:options";
                var cachedOptions = await repo.GetCacheObjectAsync<List<OptionDto>>(cacheKey);
                if (cachedOptions is not null)
                    return cachedOptions;

                // 使用投影直接获取 Id 和 Name（性能优化）
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
                    // 降级：使用完整实体映射
                    var entities = await repo.GetAllAsync(CurrentTenantId);
                    if (entities?.Any() != true)
                        return new List<OptionDto>();

                    options = entities.Select(x => new OptionDto { value = x.Id.ToString(), label = x.Name }).ToList();
                }
                
                // 缓存选项数据（常用于下拉列表）
                await repo.SetCacheObjectAsync(cacheKey, options);
                return options ?? new List<OptionDto>();
            }
            else
            {
                // 使用投影直接获取 Id 和 Name（性能优化）
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

                // 降级：使用完整实体映射
                var entities = await repo.GetAllAsync(CurrentTenantId);
                if (entities?.Any() != true)
                    return new List<OptionDto>();

                var options = entities.Select(x => new OptionDto { value = x.Id.ToString(), label = x.Name }).ToList();
                return options ?? new List<OptionDto>();
            }
        }

        /// <summary>
        /// 获取选项数据（带查询条件和排序）
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <param name="sortField">排序字段</param>
        /// <param name="isAsc">是否升序</param>
        /// <returns>选项数据列表</returns>
        public virtual async Task<List<OptionDto>> GetOptionsAsync(TQueryDto queryDto, string sortField = "CreateTime", bool isAsc = false)
        {
            // 策略：选项查询缓存 OptionDto（常用于下拉列表，高频访问）
            if (_isCache)
            {
                var cacheKey = GenerateQueryCacheKey(queryDto, $"options:dto:{sortField}:{isAsc}");
                
                // 尝试从缓存获取选项数据
                var cachedOptions = await repo.GetCacheObjectAsync<List<OptionDto>>(cacheKey);
                if (cachedOptions is not null)
                    return cachedOptions;

                // 缓存未命中，使用投影查询数据库
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
                    // 降级：使用完整实体映射
                    List<TEntity> entities = await repo.GetAsync(queryDto.ToExpression(), GetTenantId(queryDto.TenantId), sortField, isAsc);
                    options = entities?.Any() == true 
                        ? entities.Select(x => new OptionDto { value = x.Id.ToString(), label = x.Name }).ToList()
                        : new List<OptionDto>();
                }

                // 缓存选项数据
                await repo.SetCacheObjectAsync(cacheKey, options);
                
                return options;
            }

            // 未启用缓存，使用投影直接查询数据库
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

            // 降级：使用完整实体映射
            List<TEntity> result = await repo.GetAsync(queryDto.ToExpression(), GetTenantId(queryDto.TenantId), sortField, isAsc);
            return result?.Any() == true 
                ? result.Select(x => new OptionDto { value = x.Id.ToString(), label = x.Name }).ToList()
                : new List<OptionDto>();
        }


        /// <summary>
        /// 分页查询 - 自动投影优化（最简单的调用方式）
        /// 自动基于 TBasesDto 属性创建投影，无需手动定义
        /// 推荐用于列表页，性能优于 AutoMapper 方式
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
                null,  // 传 null，自动创建投影
                sortField,
                isAsc
            );
        }

        // ===================== AppService<T...> 类内 =====================

        /// <summary>
        /// 分页查询 - 使用 MongoDB 投影（推荐用于列表页，性能最优）
        /// 直接在数据库层面排除大字段，返回轻量 DTO
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
            // ✅ 未提供投影时，基于 TBasesDto 创建，并确保包含游标所需字段
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
        /// 基于 TBasesDto 的属性自动创建 MongoDB 投影
        /// 只包含 DTO 中定义且实体存在的属性；并强制包含 _id 与排序字段
        /// </summary>
        private ProjectionDefinition<TEntity, TBasesDto> CreateProjectionFromDto(string sortField = "CreateTime")
        {
            // 1) 获取 TBasesDto 的公共可读属性名
            var dtoProperties = typeof(TBasesDto)
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p => p.Name)
                .ToList();

            // 2) 获取 TEntity 的属性名（用于验证交集）
            var entityProperties = typeof(TEntity)
                .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 3) 只保留实体中存在的字段
            var validFields = dtoProperties.Where(name => entityProperties.Contains(name)).ToList();

            if (validFields.Count == 0)
                return null!; // 调用方会退回默认行为

            // 4) 构建 BsonDocument 投影
            var projectionDoc = new MongoDB.Bson.BsonDocument();

            foreach (var field in validFields)
            {
                // regular C# 属性名（如 CreateTime/UpdateTime/TenantName/Id 等）
                projectionDoc[field] = 1;
            }

            // ✅ 5) 强制包含 _id（注意：Mongo 的主键字段名是 "_id" 而不是 "Id"）
            if (!projectionDoc.Contains("_id"))
            {
                projectionDoc["_id"] = 1;
            }

            // ✅ 6) 强制包含排序字段（如果是 "Id" 则映射为 "_id"）
            var sortFieldBson = string.Equals(sortField, "Id", StringComparison.OrdinalIgnoreCase) ? "_id" : sortField;
            if (!projectionDoc.Contains(sortFieldBson))
            {
                projectionDoc[sortFieldBson] = 1;
            }

            // 7) 返回投影定义
            return new BsonDocumentProjectionDefinition<TEntity, TBasesDto>(projectionDoc);
        }

        /// <summary>
        /// 创建 OptionDto 的 MongoDB 投影
        /// 只包含 Id 和 Name 字段，用于下拉列表等场景
        /// 将 MongoDB 的 _id 映射为 value，Name 映射为 label
        /// </summary>
        protected virtual ProjectionDefinition<TEntity, OptionDto>? CreateOptionProjection()
        {
            try
            {
                // 检查实体是否有 Name 属性
                var entityProperties = typeof(TEntity)
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Select(p => p.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                if (!entityProperties.Contains("Name"))
                    return null; // 实体没有 Name 属性，无法创建投影

                // 构建投影：_id 作为 value, Name 作为 label
                var projectionDoc = new MongoDB.Bson.BsonDocument
                {
                    { "value", "$_id" },  // 将 _id 投影为 value
                    { "label", "$Name" }   // 将 Name 投影为 label
                };

                return new BsonDocumentProjectionDefinition<TEntity, OptionDto>(projectionDoc);
            }
            catch
            {
                return null; // 投影创建失败，调用方会降级到完整实体查询
            }
        }


        // 4. 删除
        /// <summary>
        /// 根据Id删除实体
        /// </summary>
        public virtual async Task<ApiResponse> DeleteAsync(string id)
        {
            if (!ObjectId.TryParse(id, out ObjectId objectId))
                return ApiResponse.ErrorResponse("400", "无效的ID格式");

            try
            {
                var result = await repo.DeleteAsync(objectId);
                
                if (result && _isCache)
                {
                    // 删除单条缓存
                    var cacheKey = GenerateCacheKey(id);
                    await repo.RemoveCacheByPrefixAsync(cacheKey);
                    
                    // 清除列表相关缓存
                    await DelCache($"{key}:list:");
                    await DelCache($"{key}:page:");
                    await DelCache($"{key}:query:");
                    await DelCache($"{key}:options:");
                    await DelCache($"{key}:all");
                }
                
                return result 
                    ? ApiResponse.SuccessResponse("删除成功") 
                    : ApiResponse.ErrorResponse("500", "删除失败");
            }
            catch (Exception err)
            {
                LogHelper.Error($"删除失败: {err.Message}", err);
                return ApiResponse.ErrorResponse("500", $"删除失败: {err.Message}");
            }
        }

        // 5. 统计
        /// <summary>
        /// 获取实体数量
        /// </summary>
        public virtual async Task<long> CountAsync(TQueryDto TQueryDto)
        {
                return await repo.CountAsync(TQueryDto.ToExpression(), GetTenantId(TQueryDto.TenantId));
        }

        #region 缓存辅助方法


        /// <summary>
        /// 生成缓存键
        /// </summary>
        protected string GenerateCacheKey(string suffix)
        {
            return $"{key}:{suffix}";
        }

        /// <summary>
        /// 删除指定前缀的缓存
        /// </summary>
        /// <param name="prefix">缓存前缀，如果为 null 则删除整个 key 前缀的所有缓存</param>
        protected async Task DelCache(string? prefix = null)
        {
            if (_isCache && repo.SupportsCaching)
            {
                var cachePrefix = string.IsNullOrEmpty(prefix) ? key : prefix;
                await repo.RemoveCacheByPrefixAsync($"{cachePrefix}*");
            }
        }

        /// <summary>
        /// 生成查询缓存键
        /// </summary>
        protected string GenerateQueryCacheKey(TQueryDto queryDto, string? suffix = null)
        {
            var hash = GenerateQueryDtoMD5(queryDto);
            return string.IsNullOrEmpty(suffix) 
                ? $"{key}:query:{hash}" 
                : $"{key}:query:{hash}:{suffix}";
        }

        /// <summary>
        /// JSON 序列化选项（用于缓存键生成）
        /// </summary>
        private static readonly JsonSerializerOptions CacheKeyJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// 生成查询DTO的MD5哈希
        /// </summary>
        private static string GenerateQueryDtoMD5(TQueryDto queryDto)
        {
            byte[] utf8 = JsonSerializer.SerializeToUtf8Bytes(queryDto, CacheKeyJsonOptions);
            byte[] hash = MD5.HashData(utf8);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        #endregion


        #region 存档代码

        ///// <summary>
        ///// 分页查询 - 使用 Seek 游标分页（支持 MongoDB 投影优化）
        ///// 默认使用 AutoMapper 映射完整实体，子类可重写使用 MongoDB 投影
        ///// </summary>
        //public virtual async Task<PagedResultDto<TBasesDto>> GetListPageAsyncbak(
        //    TQueryDto queryDto,
        //    int pageNumber,
        //    int pageSize,
        //    string sortField,
        //    bool isAsc
        //)
        //{
        //    // 策略：分页查询缓存 PagedResultDto（包含总数和数据列表）
        //    if (_isCache)
        //    {
        //        var cacheKey = GenerateQueryCacheKey(queryDto, $"page:dto:{pageNumber}:{pageSize}:{sortField}:{isAsc}");

        //        // 尝试从缓存获取分页结果
        //        var cachedResult = await repo.GetCacheObjectAsync<PagedResultDto<TBasesDto>>(cacheKey);
        //        if (cachedResult is not null)
        //            return cachedResult;

        //        // 缓存未命中，查询数据库（使用 Seek 分页）
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

        //        // 缓存分页结果（包含总数和数据）
        //        await repo.SetCacheObjectAsync(cacheKey, pagedResult);

        //        return pagedResult;
        //    }

        //    // 未启用缓存，直接查询数据库
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
