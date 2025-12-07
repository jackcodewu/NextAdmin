using System.Collections.Generic;

namespace NextAdmin.Application.DTOs.Bases.QueryPages
{
    /// <summary>
    /// 通用分页结果DTO
    /// </summary>
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; }
        public long Total { get; set; }

        public PagedResultDto(long total,List<T> items)
        {
            Total = total;
            Items = items;
        }
    }
} 
