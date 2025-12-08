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
    /// Base controller
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
        /// Current logged-in user ID
        /// </summary>
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub")
                          ?? string.Empty;

        public BaseController(IAppService<TEntity, TBaseDto, TCreateDto, TUpdateDto, TQueryDto, TBasesDto> service)
        {
            _service = service;
        }
        /// <summary>
        /// Create DTO
        /// </summary>
        [HttpPost]
        public virtual async Task<IActionResult> CreateAsync([FromBody] TCreateDto createDto)
        {

            try
            {
                var createdDto = await _service.AddAsync(createDto);
                if (createdDto == null)
                    return BadRequest(ApiResponse<object>.ErrorResponse("400", "Operation failed"));

                return Ok(ApiResponse<TBaseDto>.SuccessResponse(createdDto, "Added successfully"));
            }
            catch (Exception ex)
            {
                //var userMsg = ChineseMessageExtractor.Extract(ex);
                return Ok(ApiResponse<object>.ErrorResponse("400", "Operation failed, please check if the submitted data is valid or duplicated"));
            }
        }

        /// <summary>
        /// Update DTO
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
                return Ok(ApiResponse<object>.ErrorResponse("400", "Operation failed, please check if the submitted data is valid or duplicated"));
            }
        }

        /// <summary>
        /// Get DTO by Id
        /// </summary>
        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetAsync(string id)
        {
            var dto = await _service.GetAsync(id);
            if (dto == null)
                return Ok(ApiResponse<object>.ErrorResponse("404", "Resource not found"));
            return Ok(ApiResponse<TBaseDto>.SuccessResponse(dto, "Query successful"));
        }

        /// <summary>
        /// Get single DTO by conditions
        /// </summary>
        [HttpGet("one")]
        public virtual async Task<IActionResult> GetOneAsync([FromQuery] TQueryDto queryDto)
        {
            // If queryDto is null, create a new instance
            if (queryDto == null)
            {
                queryDto = new TQueryDto();
            }

            var dto = await _service.GetOneAsync(queryDto);
            if (dto == null)
                return Ok(ApiResponse<object>.ErrorResponse("404", "Resource not found"));
            return Ok(ApiResponse<TBaseDto>.SuccessResponse(dto, "Query successful"));
        }
        
        /// <summary>
        /// Query paged data
        /// </summary>
        /// <param name="queryDto">Query conditions</param>
        /// <param name="pageNumber">Current page number, default 1</param>
        /// <param name="pageSize">Page size, default 20 records</param>
        /// <param name="sortField"></param>
        /// <param name="isAsc"></param>
        /// <returns></returns>
        [HttpGet("page-list")]
        public virtual async Task<IActionResult> GetListPageAsync([FromQuery] TQueryDto queryDto, int pageNumber = 1, int pageSize = 20, string sortField = "CreateTime", bool isAsc = false)
        {
            // If queryDto is null, create a new instance
            if (queryDto == null)
            {
                queryDto = new TQueryDto();
            }
            
            var pagedResult = await _service.GetListPageAsync(queryDto, pageNumber, pageSize, sortField, isAsc);
            return Ok(ApiResponse<PagedResultDto<TBasesDto>>.SuccessResponse(pagedResult, "Query successful"));
        }

        /// <summary>
        /// Get all data
        /// </summary>
        /// <returns></returns>

        [HttpGet("all")]
        public virtual async Task<IActionResult> GetAllAsync()
        {
            var all = await _service.GetAllAsync();
            return Ok(ApiResponse<List<TBasesDto>>.SuccessResponse(all, "Query successful"));
        }

    /// <summary>
    /// Get all value/label data
    /// </summary>
        /// <returns></returns>
        [HttpGet("options")]
        public virtual async Task<IActionResult> GetOptionsAsync()
        {
            var all = await _service.GetOptionsAsync();

            return Ok(ApiResponse<List<OptionDto>>.SuccessResponse(all, "Query successful"));
        }

        /// <summary>
        /// Get all value/label data
        /// </summary>
        /// <returns></returns>
        [HttpGet("options-list")]
        public virtual async Task<IActionResult> GetOptionsAsync([FromQuery] TQueryDto queryDto, string sortField = "CreateTime", bool isAsc = false)
        {
            var all = await _service.GetOptionsAsync(queryDto, sortField, isAsc);

            return Ok(ApiResponse<List<OptionDto>>.SuccessResponse(all, "Query successful"));
        }

        /// <summary>
        /// Delete
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
Usage examples:

1. Get a single entity by conditions:
   GET /api/[controller]/one?name=test-name&isEnabled=true

2. Get entity by ID:
   GET /api/[controller]/123456789012345678901234

3. Paged query:
   GET /api/[controller]/page-list?pageNumber=1&pageSize=20&sortField=CreateTime&isAsc=false

4. Get all entities:
   GET /api/[controller]/all

5. Get option data:
   GET /api/[controller]/options

6. Create entity:
   POST /api/[controller]
   Body: { "name": "Test", "isEnabled": true }

7. Update entity:
   PUT /api/[controller]/123456789012345678901234
   Body: { "id": "123456789012345678901234", "name": "Updated Name" }

8. Delete entity:
   DELETE /api/[controller]/123456789012345678901234
*/ 
