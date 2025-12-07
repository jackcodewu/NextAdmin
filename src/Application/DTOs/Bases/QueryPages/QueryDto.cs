using NextAdmin.Core.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Application.DTOs.Bases.QueryPages
{
    public abstract class QueryDto<TEntity> : BaseDto
        where TEntity : AggregateRoot
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public new string? Id { get; set; }


        public abstract FilterDefinition<TEntity> ToExpression();
    }
}
