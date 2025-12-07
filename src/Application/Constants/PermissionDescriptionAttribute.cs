using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Application.Constants
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class PermissionDescriptionAttribute: Attribute
    {
        public string ParentCode { get; }
        public string Name { get; }
        public string DisplayName { get; }
        public int Sort { get; }

        public PermissionDescriptionAttribute(string parentCode,string name, string displayName, int sort)
        {
            ParentCode = parentCode;
            Name = name;
            DisplayName = displayName;
            Sort = sort;
        }
    }
}
