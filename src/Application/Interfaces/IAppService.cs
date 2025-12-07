using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Core.Domain.Entities;
using MongoDB.Bson;

namespace NextAdmin.Application.Interfaces
{
    /// <summary>
    /// 通用应用服务接口，定义常用CRUD、批量、分页、条件、删除、统计等方法
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
        // 1. 获取
        Task<TBaseDto> GetAsync(string id);
        Task<TBaseDto> GetOneAsync(TQueryDto TQueryDto);
        Task<TEntity> GetOneEntityAsync(TQueryDto TQueryDto);
        Task<List<TBasesDto>> GetsAsync(TQueryDto TQueryDto, string sortField = "CreateTime", bool isAsc = false);
        Task<List<TBasesDto>> GetAllAsync();
        Task<PagedResultDto<TBasesDto>> GetListPageAsync(TQueryDto queryDto, int pageNumber, int pageSize,string sortField = "CreateTime", bool isAsc = false);
        Task<ApiResponse> DeleteAsync(string id);
        // 5. 统计
        Task<long> CountAsync(TQueryDto TQueryDto);
        Task<List<OptionDto>> GetOptionsAsync();
        /// <summary>
        /// 获取选项数据（带查询条件和排序）
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <param name="sortField">排序字段</param>
        /// <param name="isAsc">是否升序</param>
        /// <returns>选项数据列表</returns>
        Task<List<OptionDto>> GetOptionsAsync(TQueryDto queryDto, string sortField = "CreateTime", bool isAsc = false);
        Task<TBaseDto> AddAsync(TEntity entity);
        Task<List<TEntity>> GetAllEntityAsync();
        Task<List<TEntity>> GetsEntityAsync(TQueryDto TQueryDto, string sortField = "CreateTime", bool isAsc = false);
        Task<TEntity> GetAsync(ObjectId id);
    }
}
