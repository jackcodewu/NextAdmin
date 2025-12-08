using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;


namespace NextAdmin.Application.Extensions
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ObjectIdAttribute : ValidationAttribute
    {
        public bool AllowEmpty { get; set; }

        public ObjectIdAttribute() : base("Not a valid ObjectId") { }

        public override bool IsValid(object value)
        {
            if (value is null) return true;          // Let [Required] handle it
            if (value is not string s) return false;
            if (string.IsNullOrWhiteSpace(s)) return AllowEmpty;
            if (!ObjectId.TryParse(s, out var oid)) return false;
            if (!AllowEmpty && oid == ObjectId.Empty) return false;
            return true;
        }
    }
}
