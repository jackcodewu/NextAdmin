using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace NextAdmin.Application.DTOs.Bases
{
    /// <summary>
    /// Base DTO for update operations
    /// </summary>
    public class UpdateDto
    {
        /// <summary>
        /// Primary key ID
        /// </summary>
        [Required(ErrorMessage = "Primary key cannot be empty")]
        [StringLength(30, ErrorMessage = "Primary key length cannot exceed 30")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Name
        /// </summary>
        [StringLength(100, ErrorMessage = "Name length cannot exceed 100")]
        public string? Name { get; set; }
    }
}
