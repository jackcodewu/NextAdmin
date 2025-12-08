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
        /// Safely parse JSON string, performance optimized version
        /// </summary>
        /// <param name="jsonString">JSON string</param>
        /// <returns>Parsed object</returns>
        private static object ParseJsonSafely(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return new object();

            try
            {
                // Use System.Text.Json for fast parsing
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
            // Allow mapping properties with private setters
           ShouldMapProperty = p => p.GetMethod.IsPublic || (p.SetMethod != null && (p.SetMethod.IsPrivate || p.SetMethod.IsFamily));

            // Global type converters
            CreateMap<string, ObjectId>().ConvertUsing(s => string.IsNullOrEmpty(s) ? ObjectId.Empty : ObjectId.Parse(s));
            CreateMap<ObjectId, string>().ConvertUsing(oid => oid.ToString());


            //// Global enum and string mapping
            //CreateMap<DeviceStatus, string>().ConvertUsing(e => e.ToString());
            //CreateMap<string, DeviceStatus>().ConvertUsing(s => Enum.Parse<DeviceStatus>(s));
            

            // Role-related
            CreateMap<ApplicationRole, RoleDto>().ReverseMap();
            CreateMap<ApplicationRole, RolesDto>().ReverseMap();
            CreateMap<CreateRoleDto, ApplicationRole>().ReverseMap();
                //.ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.TenantId) ? (ObjectId?)null : ObjectId.Parse(src.TenantId))
                //).ReverseMap();
            
            // User-related
            CreateMap<ApplicationUser, UserDto>().ReverseMap();
            CreateMap<ApplicationUser, UsersDto>().ReverseMap();
            CreateMap<CreateUserDto, ApplicationUser>().ReverseMap();
            CreateMap<UpdateUserDto, ApplicationUser>().ReverseMap();

            // Tenant-related
            CreateMap<Tenant, TenantDto>().ReverseMap();
            CreateMap<CreateTenantDto, Tenant>().ReverseMap();
            CreateMap<UpdateTenantDto, Tenant>().ReverseMap();

        }
    }
} 
