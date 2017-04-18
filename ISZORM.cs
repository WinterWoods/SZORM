using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using SZORM.Factory;

namespace SZORM
{
    public interface ISZORM
    {
        DbContext Context { get; }
        //定义sql 如 where  select 
        Expression ExpressionWhere { get; set; }
        List<string> ExpressionSelect { get; set; }
        Dictionary<string, string> ExpressionOrderBy { get; set; }
        string ExpressionGroup { get; set; }
        SqlAndParametersModel wheresql { get; set; }
        int BeginRowNum { get; set; }
        int EndRowNum { get; set; }
        bool IsGenSql { get; set; }
    }
    public interface ISZORM<out T> : ISZORM
    {
    }
    public class GroupData<T>
    {
        public T Data { get; set; }
        public long Count { get; set; }
        public long Sum { get; set; }
        public long Avg { get; set; }
    }
    internal class JoinModel<TSource>
    {
        public ISZORM<TSource> Source { get; set; }
        public Expression Join { get; set; }
    }
}
