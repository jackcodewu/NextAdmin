using NextAdmin.Core.Domain.Extensions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextAdmin.Core.Domain.Entities.Sys;

/// <summary>
/// Permission entity
/// </summary>
[BsonDiscriminator(RootClass = true)]
[MongoCollection("permissions")]
public class Permission : AggregateRoot
{
    public Permission(ObjectId id)
        : base(id) { }

    /// <summary>
    /// Permission code (unique)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Permission description
    /// </summary>
    public string? CnName { get; set; }

    /// <summary>
    /// Parent permission ID (supports permission tree)
    /// </summary>
    public ObjectId? ParentId { get; set; } = ObjectId.Empty;

    public string TenantName { get; set; }

    /// <summary>
    /// Parent permission code
    /// </summary>
    public string ParentCode { get; set; }

    /// <summary>
    /// Sort order
    /// </summary>
    public int Sort {  get; set; }

    /// <summary>
    /// Child permissions
    /// </summary>
    public List<Permission> Children { get; set; } = new();

    public Permission DeepClone()
    {
        return new Permission(this.Id)
        {
            Id = this.Id,
            Name = this.Name,
            Code = this.Code,
            CnName = this.CnName,
            IsEnabled = this.IsEnabled,
            ParentId = this.ParentId,
            ParentCode = this.ParentCode,
            Sort = this.Sort,
            Children = this.Children?.Select(c => c.DeepClone()).ToList(),
        };
    }

    public static Permission GetByParentCode(List<Permission> permissions, string parentCode)
    {
        var permission = permissions.FirstOrDefault(p => p.Code == parentCode);
        if (permission != null)
            return permission;

        foreach (var item in permissions)
        {
            if (item.Children.Any())
            {
                var result = GetByParentCode(item.Children, parentCode);
                if (result != null)
                    return result;
            }
        }

        return null;
    }

    public static IEnumerable<Permission> GetPermissionsFromTree(Permission parentPermission)
    {
        yield return parentPermission;
        foreach (var child in parentPermission.Children)
        {
            foreach (var descendant in GetPermissionsFromTree(child))
                yield return descendant;
        }
    }

    public static Permission BuildPermissionTree(
        Permission parentPermissin,
        List<Permission> allPermissions,
        ObjectId TenantId
    )
    {
        parentPermissin.Children = allPermissions
            .Where(p => p.ParentCode == parentPermissin.Code)
            .ToList();

        if (parentPermissin.Children?.Any() == true)
        {
            parentPermissin.Children.ForEach(p =>
            {
                p.ParentId = parentPermissin.Id;
            });

            // Recursively process child nodes, only process nodes with Code that don't contain "."
            var childrenToProcess = parentPermissin
                .Children.Where(cp => !string.IsNullOrEmpty(cp.Code) && !cp.Code.Contains("."))
                .ToList();

            foreach (var child in childrenToProcess)
            {
                BuildPermissionTree(child, allPermissions, TenantId);
            }
        }

        return parentPermissin;
    }
}
