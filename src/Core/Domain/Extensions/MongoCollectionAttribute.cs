using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Core.Domain.Extensions
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MongoCollectionAttribute : Attribute
    {
        public string Name { get; }
        public MongoCollectionAttribute(string name) => Name = name;
    }
}
