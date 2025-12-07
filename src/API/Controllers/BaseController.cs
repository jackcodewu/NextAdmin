using NextAdmin.API.Extensions;
using NextAdmin.API.Models;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.Interfaces;
using NextAdmin.Core.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace NextAdmin.API.Controllers
{
    /// <summary>
    /// 基本控制器
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class BaseController<TEntity, TBaseDto, TCreateDto, TUpdateDto, TQueryDto, TBasesDto> : ControllerBase
        where TEntity : AggregateRoot
        where TBaseDto : BaseDto, new()
        where TCreateDto : CreateDto, new()
        where TUpdateDto : UpdateDto, new()
        where TQueryDto : QueryPageDto<TQueryDto, TEntity>, new()
        where TBasesDto : BasesDto, new()
    {
        protected readonly IAppService<TEntity, TBaseDto, TCreateDto, TUpdateDto, TQueryDto, TBasesDto> _service;

        /// <summary>
        /// 当前登录用户Id
        /// </summary>
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub")
                          ?? string.Empty;

        public BaseController(IAppService<TEntity, TBaseDto, TCreateDto, TUpdateDto, TQueryDto, TBasesDto> service)
        {
            _service = service;
        }
        /// <summary>
        /// 新增DTO
        /// </summary>
        [HttpPost]
        public virtual async Task<IActionResult> CreateAsync([FromBody] TCreateDto createDto)
        {

            try
            {
                var createdDto = await _service.AddAsync(createDto);
                if (createdDto == null)
                    return BadRequest(ApiResponse<object>.ErrorResponse("400", "操作失败"));

                return Ok(ApiResponse<TBaseDto>.SuccessResponse(createdDto, "添加成功"));
            }
            catch (Exception ex)
            {
                //var userMsg = ChineseMessageExtractor.Extract(ex);
                return Ok(ApiResponse<object>.ErrorResponse("400", "操作失败,请检测提交的数据是否符合规范或是否重复"));
            }
        }

        /// <summary>
        /// 更新DTO
        /// </summary>
        [HttpPut("{id}")]
        public virtual async Task<IActionResult> UpdateAsync(string id, [FromBody] TUpdateDto updateDto)
        {
            try
            {
                if (id != updateDto.Id)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("400", "ID in URL does not match ID in body."));
                }

                var result = await _service.UpdateAsync(updateDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(ApiResponse<object>.ErrorResponse("400", "操作失败,请检测提交的数据是否符合规范或是否重复"));
            }
        }

        /// <summary>
        /// 根据Id获取DTO
        /// </summary>
        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetAsync(string id)
        {
            var dto = await _service.GetAsync(id);
            if (dto == null)
                return Ok(ApiResponse<object>.ErrorResponse("404", "资源未找到"));
            return Ok(ApiResponse<TBaseDto>.SuccessResponse(dto, "查询成功"));
        }

        /// <summary>
        /// 根据条件获取单个DTO
        /// </summary>
        [HttpGet("one")]
        public virtual async Task<IActionResult> GetOneAsync([FromQuery] TQueryDto queryDto)
        {
            // 如果 queryDto 为 null，创建一个新的实例
            if (queryDto == null)
            {
                queryDto = new TQueryDto();
            }

            var dto = await _service.GetOneAsync(queryDto);
            if (dto == null)
                return Ok(ApiResponse<object>.ErrorResponse("404", "资源未找到"));
            return Ok(ApiResponse<TBaseDto>.SuccessResponse(dto, "查询成功"));
        }
        
        /// <summary>
        /// 查询分页数据
        /// </summary>
        /// <param name="queryDto">查询条件</param>
        /// <param name="pageNumber">当前页码，默认1</param>
        /// <param name="pageSize">每页数据量，默认20条记录</param>
        /// <param name="sortField"></param>
        /// <param name="isAsc"></param>
        /// <returns></returns>
        [HttpGet("page-list")]
        public virtual async Task<IActionResult> GetListPageAsync([FromQuery] TQueryDto queryDto, int pageNumber = 1, int pageSize = 20, string sortField = "CreateTime", bool isAsc = false)
        {
            // 如果 queryDto 为 null，创建一个新的实例
            if (queryDto == null)
            {
                queryDto = new TQueryDto();
            }
            
            var pagedResult = await _service.GetListPageAsync(queryDto, pageNumber, pageSize, sortField, isAsc);
            return Ok(ApiResponse<PagedResultDto<TBasesDto>>.SuccessResponse(pagedResult, "查询成功"));
        }

        /// <summary>
        /// 获取所有数据
        /// </summary>
        /// <returns></returns>

        [HttpGet("all")]
        public virtual async Task<IActionResult> GetAllAsync()
        {
            var all = await _service.GetAllAsync();
            return Ok(ApiResponse<List<TBasesDto>>.SuccessResponse(all, "查询成功"));
        }

    /// <summary>
    /// 获取所有value/lable数据
    /// </summary>
        /// <returns></returns>
        [HttpGet("options")]
        public virtual async Task<IActionResult> GetOptionsAsync()
        {
            var all = await _service.GetOptionsAsync();

            return Ok(ApiResponse<List<OptionDto>>.SuccessResponse(all, "查询成功"));
        }

        /// <summary>
        /// 获取所有value/lable数据
        /// </summary>
        /// <returns></returns>
        [HttpGet("options-list")]
        public virtual async Task<IActionResult> GetOptionsAsync([FromQuery] TQueryDto queryDto, string sortField = "CreateTime", bool isAsc = false)
        {
            var all = await _service.GetOptionsAsync(queryDto, sortField, isAsc);

            return Ok(ApiResponse<List<OptionDto>>.SuccessResponse(all, "查询成功"));
        }

        /// <summary>
        /// 删除
        /// </summary>
        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> DeleteAsync(string id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result.Success)
            {
                return Ok(result);
            }

            return Ok(result);
        }
    }
} 

/*
使用示例：

1. 根据条件获取单个实体：
   GET /api/[controller]/one?name=测试名称&isEnabled=true

2. 根据ID获取实体：
   GET /api/[controller]/123456789012345678901234

3. 分页查询：
   GET /api/[controller]/page-list?pageNumber=1&pageSize=20&sortField=CreateTime&isAsc=false

4. 获取所有实体：
   GET /api/[controller]/all

5. 获取选项数据：
   GET /api/[controller]/options

6. 创建实体：
   POST /api/[controller]
   Body: { "name": "测试", "isEnabled": true }

7. 更新实体：
   PUT /api/[controller]/123456789012345678901234
   Body: { "id": "123456789012345678901234", "name": "更新后的名称" }

8. 删除实体：
   DELETE /api/[controller]/123456789012345678901234
*/ 
