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
        ///// 页码
        ///// </summary>
        //public int? PageNumber { get; set; } = 1;

        ///// <summary>
        ///// 每页条数
        ///// </summary>
        //public int? PageSize { get; set; } = 20;

        /// <summary>
        /// 多ID集合
        /// </summary>
        public List<string>? Ids { get;  set; }

        /// <summary>
        /// 是否日期范围，如果true，则默认最近一个月时间范围
        /// </summary>
        public bool IsTimeRange { get; set; }

        /// <summary>
        /// 开始时间,默认一个月
        /// </summary>
        public virtual DateTime? StartTime { get; set; } = DateTime.Now.Date.AddMonths(-1);

        /// <summary>
        /// 结束时间，默认当前时间
        /// </summary>
        public virtual DateTime? EndTime { get; set; } = DateTime.Now.Date.AddDays(1);

        protected FilterDefinitionBuilder<TEntity> builder;

        /// <summary>
        /// 获取基类的MongoDB原生过滤器（时间范围过滤）
        /// </summary>
        /// <returns>MongoDB原生过滤器</returns>
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
