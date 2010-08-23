// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using IQToolkit.Data.Common.Mapping;

namespace IQToolkit.Data.Common
{
    /// <summary>
    /// Defines mapping information & rules for the query provider
    /// </summary>
    public abstract class QueryMapping
    {
        /// <summary>
        /// Get the meta entity that maps between the CLR type 'entityType' and the database table, yet
        /// is represented publicly as 'elementType'.
        /// </summary>
        /// <param name="entityID"></param>
        /// <returns></returns>
        public abstract MappingEntity GetEntity(string entityID);

        public abstract IEnumerable<Property> GetMappedProperties(MappingEntity entity);

        public abstract bool IsPrimaryKey(MappingEntity entity, Property property);

        public virtual IEnumerable<Property> GetPrimaryKeyMembers(MappingEntity entity)
        {
            return this.GetMappedProperties(entity).Where(m => m.IsPrimaryKey);
        }

        /// <summary>
        /// Determines if a property is mapped as a relationship
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public abstract bool IsRelationship(MappingEntity entity, Property property);

        /// <summary>
        /// Determines if a relationship property refers to a single entity (as opposed to a collection.)
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public virtual bool IsSingletonRelationship(MappingEntity entity, Property property)
        {
            if (!this.IsRelationship(entity, property))
                return false;
            Type ieType = TypeHelper.FindIEnumerable(property.PropertyType);
            return ieType == null;
        }

        /// <summary>
        /// Determines whether a given expression can be executed locally. 
        /// (It contains no parts that should be translated to the target environment.)
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public virtual bool CanBeEvaluatedLocally(Expression expression)
        {
            // any operation on a query can't be done locally
            ConstantExpression cex = expression as ConstantExpression;
            if (cex != null)
            {
                IQueryable query = cex.Value as IQueryable;
                if (query != null && query.Provider == this)
                    return false;
            }
            MethodCallExpression mc = expression as MethodCallExpression;
            if (mc != null &&
                (mc.Method.DeclaringType == typeof(Enumerable) ||
                 mc.Method.DeclaringType == typeof(Queryable) ||
                 mc.Method.DeclaringType == typeof(Updatable))
                 )
            {
                return false;
            }
            if (expression.NodeType == ExpressionType.Convert &&
                expression.Type == typeof(object))
                return true;
            return expression.NodeType != ExpressionType.Parameter &&
                   expression.NodeType != ExpressionType.Lambda;
        }

        public abstract object GetPrimaryKey(MappingEntity entity, object instance);
        public abstract Expression GetPrimaryKeyQuery(MappingEntity entity, Expression source, Expression[] keys);
        public abstract IEnumerable<EntityInfo> GetDependentEntities(MappingEntity entity, object instance);
        public abstract IEnumerable<EntityInfo> GetDependingEntities(MappingEntity entity, object instance);
        public abstract object CloneEntity(MappingEntity entity, object instance);
        public abstract bool IsModified(MappingEntity entity, object instance, object original);

        public abstract QueryMapper CreateMapper(QueryTranslator translator);
    }
}