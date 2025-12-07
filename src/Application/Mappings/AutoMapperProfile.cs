using AutoMapper;
using NextAdmin.Application.DTOs;
using NextAdmin.Application.DTOs.Roles;
using NextAdmin.Application.DTOs.Users;
using NextAdmin.Application.DTOs.Tenants;
using NextAdmin.Core.Domain.Entities;
using NextAdmin.Core.Domain.Entities.Sys;
using NextAdmin.Shared.Enums;
using MongoDB.Bson;

namespace NextAdmin.Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        /// <summary>
        /// 安全解析JSON字符串，性能优化版本
        /// </summary>
        /// <param name="jsonString">JSON字符串</param>
        /// <returns>解析后的对象</returns>
        private static object ParseJsonSafely(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return new object();

            try
            {
                // 使用 System.Text.Json 进行快速解析
                using var document = System.Text.Json.JsonDocument.Parse(jsonString);
                return System.Text.Json.JsonSerializer.Deserialize<object>(jsonString);
            }
            catch
            {
                return new object();
            }
        }

        public AutoMapperProfile()
        {
            // 允许映射带有私有 setter 的属性
           ShouldMapProperty = p => p.GetMethod.IsPublic || (p.SetMethod != null && (p.SetMethod.IsPrivate || p.SetMethod.IsFamily));

            // 全局类型转换器
            CreateMap<string, ObjectId>().ConvertUsing(s => string.IsNullOrEmpty(s) ? ObjectId.Empty : ObjectId.Parse(s));
            CreateMap<ObjectId, string>().ConvertUsing(oid => oid.ToString());


            //// 全局枚举与字符串映射
            //CreateMap<DeviceStatus, string>().ConvertUsing(e => e.ToString());
            //CreateMap<string, DeviceStatus>().ConvertUsing(s => Enum.Parse<DeviceStatus>(s));
            

            // Role相关
            CreateMap<ApplicationRole, RoleDto>().ReverseMap();
            CreateMap<ApplicationRole, RolesDto>().ReverseMap();
            CreateMap<CreateRoleDto, ApplicationRole>().ReverseMap();
                //.ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.TenantId) ? (ObjectId?)null : ObjectId.Parse(src.TenantId))
                //).ReverseMap();
            
            // User相关
            CreateMap<ApplicationUser, UserDto>().ReverseMap();
            CreateMap<ApplicationUser, UsersDto>().ReverseMap();
            CreateMap<CreateUserDto, ApplicationUser>().ReverseMap();
            CreateMap<UpdateUserDto, ApplicationUser>().ReverseMap();

            // Tenant相关
            CreateMap<Tenant, TenantDto>().ReverseMap();
            CreateMap<CreateTenantDto, Tenant>().ReverseMap();
            CreateMap<UpdateTenantDto, Tenant>().ReverseMap();

        }
    }
} 
