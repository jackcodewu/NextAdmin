using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Application.DTOs.Bases
{
    public class OptionDto
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string value { get; set; }

        public string label { get; set; }

    }

}
