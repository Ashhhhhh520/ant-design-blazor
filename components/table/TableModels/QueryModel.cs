﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using AntDesign.Filters;

namespace AntDesign.TableModels
{
    public class QueryModel
    {
        public int PageIndex { get; }

        public int PageSize { get; }

        public int StartIndex { get; }

        [Obsolete("Please use StartIndex")]
        public int OffsetRecords => StartIndex;

        public IList<ITableSortModel> SortModel { get; private set; }

        public IList<ITableFilterModel> FilterModel { get; private set; }

        public QueryModel()
        {
            this.SortModel = new List<ITableSortModel>();
            this.FilterModel = new List<ITableFilterModel>();
        }

        internal QueryModel(int pageIndex, int pageSize, int startIndex)
        {
            this.PageSize = pageSize;
            this.PageIndex = pageIndex > 0 ? pageIndex : (int)Math.Ceiling((double)startIndex / pageSize);
            this.StartIndex = startIndex > 0 ? startIndex : (pageIndex - 1) * pageSize;
            this.SortModel = new List<ITableSortModel>();
            this.FilterModel = new List<ITableFilterModel>();
        }

#if NET5_0_OR_GREATER
        [JsonConstructor]
#endif

        public QueryModel(int pageIndex, int pageSize, int startIndex, IList<ITableSortModel> sortModel, IList<ITableFilterModel> filterModel)
            : this(pageIndex, pageSize, startIndex)
        {
            this.SortModel = sortModel;
            this.FilterModel = filterModel;
        }
    }

    public class QueryModel<TItem> : QueryModel, ICloneable
    {
        internal QueryModel(int pageIndex, int pageSize, int startIndex) : base(pageIndex, pageSize, startIndex)
        {
        }

#if NET5_0_OR_GREATER
        [JsonConstructor]
#endif

        public QueryModel(int pageIndex, int pageSize, int startIndex, IList<ITableSortModel> sortModel, IList<ITableFilterModel> filterModel)
            : base(pageIndex, pageSize, startIndex, sortModel, filterModel)
        {
        }

        internal void AddSortModel(ITableSortModel model)
        {
            SortModel.Add(model);
        }

        internal void AddFilterModel(ITableFilterModel model)
        {
            FilterModel.Add(model);
        }

        public IQueryable<TItem> ExecuteQuery(IQueryable<TItem> query)
        {
            foreach (var sort in SortModel.OrderBy(x => x.Priority))
            {
                query = sort.SortList(query);
            }

            foreach (var filter in FilterModel)
            {
                query = filter.FilterList(query);
            }

            return query;
        }

        /// <summary>
        /// Get current filters' expression for ORMs like Entity Framework.
        /// And you can get the filtered data by executing the expression with the data source.
        /// </summary>
        /// <returns></returns>
        public Expression<Func<TItem, bool>> GetFilterExpression()
        {
            if (!FilterModel.Any())
            {
                return Expression.Lambda<Func<TItem, bool>>(Expression.Constant(true, typeof(bool)), Expression.Parameter(typeof(TItem)));
            }
            var filters = FilterModel.Select(filter => filter.FilterExpression<TItem>());
            return filters.Aggregate(Combine);
        }

        public IQueryable<TItem> CurrentPagedRecords(IQueryable<TItem> query) => query.Skip(StartIndex).Take(PageSize);

        public Expression<Func<TItem, bool>>? GetQueryExpression()
        {
            if (!FilterModel.Any())
            {
                //return Expression.Lambda<Func<TItem, bool>>(Expression.Constant(true, typeof(bool)), Expression.Parameter(typeof(TItem)));
                return null;
            }
            var filters = FilterModel.Select(filter => filter.FilterExpression<TItem>());
            return filters.Aggregate(Combine);
        }

        Expression<Func<TItem, bool>> Combine(Expression<Func<TItem, bool>> expr1, Expression<Func<TItem, bool>> expr2)
        {
            var combineExp = Expression.Lambda<Func<TItem, bool>>(Expression.AndAlso(expr1.Body, expr2.Body), expr1.Parameters);
            return combineExp;
        }

        public object Clone()
        {
            var sorters = this.SortModel.Select(x => x.Clone() as ITableSortModel).ToList();
            var filters = this.FilterModel.ToList();
            return new QueryModel<TItem>(PageIndex, PageSize, StartIndex, sorters, filters);
        }

        private Expression<Func<TItem, bool>> Combine(Expression<Func<TItem, bool>> expr1, Expression<Func<TItem, bool>> expr2)
        {
            var combineExp = Expression.Lambda<Func<TItem, bool>>(Expression.AndAlso(expr1.Body, expr2.Body), expr1.Parameters);
            return combineExp;
        }
    }
}
