using System.ComponentModel.DataAnnotations;
using AspNetCore.Identity.Mongo.Model;
using NextAdmin.Core.Domain.Entities.Sys;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Core.Domain.Entities;

public class ApplicationRole : MongoRole<ObjectId>
{
    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsSystemRole { get; set; } = false;

    public ObjectId TenantId { get; set; } = ObjectId.Empty;

    /// <summary>
    /// 公司名称
    /// </summary>
    public string TenantName { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();


    /// <summary>
    /// 角色拥有的权限码
    /// </summary>
    public List<string> PermissionCodes { get; private set; } = new();

    private List<Permission> permissions = new();
    /// <summary>
    /// 角色拥有的权限
    /// </summary>
    public List<Permission> Permissions
    {
        get { return permissions; }
        set
        {
            permissions = value;
            PermissionCodes = permissions.SelectMany(p => GetAllPermissionCodes(p)).ToList();
        }
    }

    /// <summary>
    ///角色拥有的菜单
    /// </summary>
    public List<Menu> Menus { get; set; } = new();

    public List<Menu> DeepCloneMenus(List<Menu> templates)
    {
        return templates
            .Select(m => new Menu
            {
                Path = m.Path,
                Title = m.Title,
                Name = m.Name,
                Icon = m.Icon,
                Component = m.Component,
                ParentId = m.ParentId,
                IsHide = m.IsHide,
                IsKeepAlive = m.IsKeepAlive,
                IsAffix = m.IsAffix,
                IsLink = m.IsLink,
                IsIframe = m.IsIframe,
                Sort = m.Sort,
                Redirect = m.Redirect,
                Children = m.Children != null ? DeepCloneMenus(m.Children) : null,
            })
            .ToList();
    }   

    private IEnumerable<string> GetAllPermissionCodes(Permission permission)
    {
        if (
            permission.IsEnabled
            && !string.IsNullOrEmpty(permission.Code)
            && permission.Code.Contains(".")
        )
            yield return permission.Code;

        if (permission.IsEnabled && permission.Children.Any())
        {
            foreach (var child in permission.Children)
            {
                foreach (var code in GetAllPermissionCodes(child))
                {
                    yield return code;
                }
            }
        }
    }

}
