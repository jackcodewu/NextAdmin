using NextAdmin.API.Controllers;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Bases;
using NextAdmin.Application.DTOs.Bases.QueryPages;
using NextAdmin.Application.Interfaces;
using NextAdmin.Core.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace NextAdmin.API.Extensions
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GenericController<TEntity, TBaseDto, TCreateDto, TUpdateDto, TQueryDto, TBasesDto>
     : BaseController<TEntity, TBaseDto, TCreateDto, TUpdateDto, TQueryDto, TBasesDto>
     where TEntity : AggregateRoot
     where TBaseDto : BaseDto, new()
     where TCreateDto : CreateDto, new()
     where TUpdateDto : UpdateDto, new()
     where TQueryDto : QueryPageDto<TQueryDto, TEntity>, new()
     where TBasesDto : BasesDto, new()
    {
        public GenericController(IAppService<TEntity, TBaseDto, TCreateDto, TUpdateDto, TQueryDto, TBasesDto> service)
            : base(service)
        {
        }
    }
}
