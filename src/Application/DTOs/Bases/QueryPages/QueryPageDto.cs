using NextAdmin.Core.Domain.Entities;
using System;
using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Bson;

namespace NextAdmin.Application.DTOs.Bases.QueryPages
{
    public abstract class QueryPageDto<TQueryDto, TEntity> : QueryDto<TEntity>
        where TQueryDto : QueryDto<TEntity>
        where TEntity : AggregateRoot
    {
        ///// <summary>
        ///// Page number
        ///// </summary>
        //public int? PageNumber { get; set; } = 1;

        ///// <summary>
        ///// Items per page
        ///// </summary>
        //public int? PageSize { get; set; } = 20;

        /// <summary>
        /// Multiple ID collection
        /// </summary>
        public List<string>? Ids { get;  set; }

        /// <summary>
        /// Is date range, if true, defaults to last month time range
        /// </summary>
        public bool IsTimeRange { get; set; }

        /// <summary>
        /// Start time, defaults to one month ago
        /// </summary>
        public virtual DateTime? StartTime { get; set; } = DateTime.Now.Date.AddMonths(-1);

        /// <summary>
        /// End time, defaults to current time
        /// </summary>
        public virtual DateTime? EndTime { get; set; } = DateTime.Now.Date.AddDays(1);

        protected FilterDefinitionBuilder<TEntity> builder;

        /// <summary>
        /// Get base MongoDB native filter (time range filter)
        /// </summary>
        /// <returns>MongoDB native filter</returns>
        private FilterDefinition<TEntity> GetBaseFilter()
        {
            var builder = Builders<TEntity>.Filter;
            var filter = builder.Empty;
            if (!string.IsNullOrWhiteSpace(Id))
            {
                ObjectId.TryParse(Id, out ObjectId objId);
                if (objId != ObjectId.Empty)
                    filter &= builder.Eq(x => x.Id, objId);
            }


            if( Ids != null && Ids.Count > 0)
            {
                var objectIdList = new List<ObjectId>();
                foreach (var id in Ids)
                {
                    if (ObjectId.TryParse(id, out ObjectId objId))
                    {
                        objectIdList.Add(objId);
                    }
                }
                if (objectIdList.Count > 0)
                {
                    filter &= builder.In(x => x.Id, objectIdList);
                }
            }

            if (IsTimeRange)
            {
                if (StartTime.HasValue)
                {
                    filter &= builder.Gte(x => x.CreateTime, StartTime.Value);
                }

                if (EndTime.HasValue)
                {
                    filter &= builder.Lte(x => x.CreateTime, EndTime.Value);
                }
            }

            return filter;
        }

        public override FilterDefinition<TEntity> ToExpression()
        {
            return GetBaseFilter();
        }

    }
}
