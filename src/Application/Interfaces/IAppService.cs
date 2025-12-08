using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Core.Domain.Entities;
using MongoDB.Bson;

namespace NextAdmin.Application.Interfaces
{
    /// <summary>
    /// Generic application service interface defining common CRUD, batch, pagination, conditional, delete, and statistics methods
    /// </summary>
    public interface IAppService<TEntity, TBaseDto, TCreateDto, TUpdateDto, TQueryDto, TBasesDto>
        where TEntity : AggregateRoot
        where TBaseDto : BaseDto, new()
        where TCreateDto : CreateDto, new()
        where TUpdateDto : UpdateDto, new()
        where TQueryDto : QueryDto<TEntity>, new()
        where TBasesDto : BasesDto, new()
    {
        Task<TBaseDto> AddAsync(TCreateDto createDto);
        Task<int> AddManyAsync(List<TCreateDto> createDtos);
        Task<int> AddManyAsync(List<TEntity> entities);
        Task<ApiResponse> UpdateAsync(TUpdateDto updateDto);
        Task<bool> UpdateAsync(TEntity entity);
        Task UpdateManyAsync(List<TEntity> entities);
        // 1. Query
        Task<TBaseDto> GetAsync(string id);
        Task<TBaseDto> GetOneAsync(TQueryDto TQueryDto);
        Task<TEntity> GetOneEntityAsync(TQueryDto TQueryDto);
        Task<List<TBasesDto>> GetsAsync(TQueryDto TQueryDto, string sortField = "CreateTime", bool isAsc = false);
        Task<List<TBasesDto>> GetAllAsync();
        Task<PagedResultDto<TBasesDto>> GetListPageAsync(TQueryDto queryDto, int pageNumber, int pageSize,string sortField = "CreateTime", bool isAsc = false);
        Task<ApiResponse> DeleteAsync(string id);
        // 5. Statistics
        Task<long> CountAsync(TQueryDto TQueryDto);
        Task<List<OptionDto>> GetOptionsAsync();
        /// <summary>
        /// Get option data (with query conditions and sorting)
        /// </summary>
        /// <param name="queryDto">Query conditions</param>
        /// <param name="sortField">Sort field</param>
        /// <param name="isAsc">Ascending order</param>
        /// <returns>Option data list</returns>
        Task<List<OptionDto>> GetOptionsAsync(TQueryDto queryDto, string sortField = "CreateTime", bool isAsc = false);
        Task<TBaseDto> AddAsync(TEntity entity);
        Task<List<TEntity>> GetAllEntityAsync();
        Task<List<TEntity>> GetsEntityAsync(TQueryDto TQueryDto, string sortField = "CreateTime", bool isAsc = false);
        Task<TEntity> GetAsync(ObjectId id);
    }
}
