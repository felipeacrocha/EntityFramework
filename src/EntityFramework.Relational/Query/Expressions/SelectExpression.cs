﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational.Query.Sql;
using Microsoft.Data.Entity.Relational.Utilities;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Microsoft.Data.Entity.Relational.Query.Expressions
{
    public class SelectExpression : ExtensionExpression
    {
        private readonly List<ColumnExpression> _projection = new List<ColumnExpression>();
        private readonly List<TableExpression> _tables = new List<TableExpression>();
        private readonly List<Ordering> _orderBy = new List<Ordering>();

        private int? _limit;
        private int? _offset;
        private bool _projectStar;

        private SelectExpression _subquery;
        private int? _subqueryAlias;

        private Expression _projectionExpression;
        private bool _isDistinct;

        public SelectExpression()
            : base(typeof(object))
        {
        }

        public virtual IReadOnlyList<TableExpression> Tables
        {
            get { return _tables; }
        }

        public virtual bool IsProjectStar
        {
            get { return _projectStar; }
        }

        public virtual SelectExpression Subquery
        {
            get { return _subquery; }
        }

        public virtual string SubqueryAlias
        {
            get { return "t" + _subqueryAlias; }
        }

        public virtual void AddTable([NotNull] TableExpression tableExpression)
        {
            Check.NotNull(tableExpression, "tableExpression");

            _tables.Add(tableExpression);
        }

        public virtual void AddTables([NotNull] IEnumerable<TableExpression> tableExpressions)
        {
            Check.NotNull(tableExpressions, "tableExpressions");

            _tables.AddRange(tableExpressions);
        }

        public virtual void ClearTables()
        {
            _tables.Clear();
        }

        public virtual bool HandlesQuerySource([NotNull] IQuerySource querySource)
        {
            Check.NotNull(querySource, "querySource");

            return _tables.Any(tableExpression => tableExpression.QuerySource == querySource);
        }

        public virtual TableExpression FindTableForQuerySource([NotNull] IQuerySource querySource)
        {
            Check.NotNull(querySource, "querySource");

            return _tables.Single(t => t.QuerySource == querySource);
        }

        public virtual bool IsDistinct
        {
            get { return _isDistinct; }
            set
            {
                if (_limit != null
                    || _offset != null)
                {
                    PushDownSubquery();
                }

                _isDistinct = value;
            }
        }

        public virtual int? Limit
        {
            get { return _limit; }
            set
            {
                Check.NotNull(value, "value");

                PushDownIfLimit();

                _limit = value;
            }
        }

        public virtual int? Offset
        {
            get { return _offset; }
            set
            {
                Check.NotNull(value, "value");

                if (_limit != null)
                {
                    PushDownSubquery();

                    _subquery._offset = null;

                    foreach (var ordering in _subquery.OrderBy)
                    {
                        _orderBy.Add(
                            new Ordering(
                                new ColumnExpression(((ColumnExpression)ordering.Expression).Property, SubqueryAlias),
                                ordering.OrderingDirection));
                    }
                }

                _offset = value;
            }
        }

        private void PushDownIfLimit()
        {
            if (_limit != null)
            {
                PushDownSubquery();
            }
        }

        private void PushDownIfDistinct()
        {
            if (_isDistinct)
            {
                PushDownSubquery();
            }
        }

        private void PushDownSubquery()
        {
            var subquery = new SelectExpression();

            var columnAliasCounter = 0;

            foreach (var columnExpression in _projection)
            {
                if (subquery._projection.FindIndex(ce => ce.Name == columnExpression.Name) != -1)
                {
                    columnExpression.Alias = "c" + columnAliasCounter++;
                }

                subquery._projection.Add(columnExpression);
            }

            subquery.AddTables(_tables);
            subquery.AddToOrderBy(_orderBy);

            subquery._limit = _limit;
            subquery._offset = _offset;
            subquery._isDistinct = _isDistinct;
            subquery._subquery = _subquery;
            subquery._subqueryAlias = _subqueryAlias;
            subquery._projectStar = _projectStar;

            _limit = null;
            _offset = null;
            _isDistinct = false;

            ClearTables();
            ClearProjection();
            ClearOrderBy();

            _projectStar = true;

            _subquery = subquery;
            _subqueryAlias = subquery._subqueryAlias != null
                ? subquery._subqueryAlias + 1
                : 0;
        }

        public virtual IReadOnlyList<ColumnExpression> Projection
        {
            get { return _projection; }
        }

        public virtual Expression ProjectionExpression
        {
            get { return _projectionExpression; }
        }

        public virtual void AddToProjection([NotNull] IProperty property, [NotNull] IQuerySource querySource)
        {
            Check.NotNull(property, "property");
            Check.NotNull(querySource, "querySource");

            if (GetProjectionIndex(property, querySource) == -1)
            {
                _projection.Add(new ColumnExpression(property, FindTableForQuerySource(querySource).Alias));
            }
        }

        public virtual void AddToProjection([NotNull] ColumnExpression columnExpression)
        {
            Check.NotNull(columnExpression, "columnExpression");

            if (_projection
                .FindIndex(ce =>
                    ce.Property == columnExpression.Property
                    && ce.TableAlias == columnExpression.TableAlias) == -1)
            {
                _projection.Add(columnExpression);
            }
        }

        public virtual void SetProjectionCaseExpression([NotNull] CaseExpression caseExpression)
        {
            Check.NotNull(caseExpression, "caseExpression");

            ClearProjection();

            _projectionExpression = caseExpression;
        }

        public virtual void SetProjectionExpression([NotNull] Expression expression)
        {
            Check.NotNull(expression, "expression");

            PushDownIfLimit();
            PushDownIfDistinct();

            ClearProjection();

            _projectionExpression = expression;
        }

        public virtual void ClearProjection()
        {
            _projection.Clear();
        }

        public virtual void RemoveRangeFromProjection(int index)
        {
            if (index < _projection.Count)
            {
                _projection.RemoveRange(index, _projection.Count - index);
            }
        }

        public virtual void RemoveFromProjection([NotNull] IEnumerable<Ordering> orderBy)
        {
            Check.NotNull(orderBy, "orderBy");

            _projection.RemoveAll(ce => orderBy.Any(o => o.Expression == ce));
        }

        public virtual int GetProjectionIndex([NotNull] IProperty property, [NotNull] IQuerySource querySource)
        {
            Check.NotNull(property, "property");
            Check.NotNull(querySource, "querySource");

            var table = FindTableForQuerySource(querySource);

            return _projection.FindIndex(ce => ce.Property == property && ce.TableAlias == table.Alias);
        }

        public virtual Expression Predicate { get; [param: CanBeNull] set; }

        public virtual ColumnExpression AddToOrderBy(
            [NotNull] IProperty property,
            [NotNull] IQuerySource querySource,
            OrderingDirection orderingDirection)
        {
            Check.NotNull(property, "property");
            Check.NotNull(property, "querySource");

            var columnExpression
                = new ColumnExpression(property, FindTableForQuerySource(querySource).Alias);

            _orderBy.Add(new Ordering(columnExpression, orderingDirection));

            return columnExpression;
        }

        public virtual void AddToOrderBy([NotNull] IEnumerable<Ordering> orderings)
        {
            Check.NotNull(orderings, "orderings");

            _orderBy.AddRange(orderings);
        }

        public virtual IReadOnlyList<Ordering> OrderBy
        {
            get { return _orderBy; }
        }

        public virtual void ClearOrderBy()
        {
            _orderBy.Clear();
        }

        public virtual void AddCrossJoin([NotNull] SelectExpression selectExpression)
        {
            Check.NotNull(selectExpression, "selectExpression");
            Contract.Assert(!selectExpression.OrderBy.Any());

            var joinedTable = selectExpression.Tables.Single();

            _tables.Add(
                new CrossJoinExpression(
                    joinedTable.Table,
                    joinedTable.Schema,
                    joinedTable.Alias,
                    joinedTable.QuerySource));

            _projection.AddRange(selectExpression.Projection);
        }

        public virtual InnerJoinExpression AddInnerJoin(
            [NotNull] SelectExpression selectExpression, bool mergeProjection)
        {
            Check.NotNull(selectExpression, "selectExpression");
            Contract.Assert(!selectExpression.OrderBy.Any());

            var joinedTable = selectExpression.Tables.Single();

            var innerJoinExpression
                = new InnerJoinExpression(
                    joinedTable.Table,
                    joinedTable.Schema,
                    joinedTable.Alias,
                    joinedTable.QuerySource);

            _tables.Add(innerJoinExpression);

            if (mergeProjection)
            {
                _projection.AddRange(selectExpression.Projection);
            }

            return innerJoinExpression;
        }

        public virtual void RemoveTable([NotNull] TableExpression tableExpression)
        {
            Check.NotNull(tableExpression, "tableExpression");

            _tables.Remove(tableExpression);
        }

        public override Expression Accept([NotNull] ExpressionTreeVisitor visitor)
        {
            Check.NotNull(visitor, "visitor");

            var specificVisitor = visitor as ISqlExpressionVisitor;

            if (specificVisitor != null)
            {
                return specificVisitor.VisitSelectExpression(this);
            }

            return base.Accept(visitor);
        }

        protected override Expression VisitChildren(ExpressionTreeVisitor visitor)
        {
            return this;
        }

        public override string ToString()
        {
            return new DefaultSqlQueryGenerator().GenerateSql(this);
        }
    }
}
