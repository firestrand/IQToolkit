// Copyright (c) Microsoft Corporation.  All rights reserved.
// This source code is made available under the terms of the Microsoft Public License (MS-PL)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IQToolkit.Data.Common
{
    public abstract class MappingTable
    {
    }

    public abstract class AdvancedMapping : BasicMapping
    {
        public abstract bool IsNestedEntity(MappingEntity entity, MemberInfo member);
        public abstract IList<MappingTable> GetTables(MappingEntity entity);
        public abstract string GetAlias(MappingTable table);
        public abstract string GetAlias(MappingEntity entity, MemberInfo member);
        public abstract string GetTableName(MappingTable table);
        public abstract bool IsExtensionTable(MappingTable table);
        public abstract string GetExtensionRelatedAlias(MappingTable table);
        public abstract IEnumerable<string> GetExtensionKeyColumnNames(MappingTable table);
        public abstract IEnumerable<MemberInfo> GetExtensionRelatedMembers(MappingTable table);

        public override bool IsRelationship(MappingEntity entity, MemberInfo member)
        {
            return base.IsRelationship(entity, member)
                   || IsNestedEntity(entity, member);
        }

        public override object CloneEntity(MappingEntity entity, object instance)
        {
            object clone = base.CloneEntity(entity, instance);

            // need to clone nested entities too
            foreach (var mi in GetMappedMembers(entity))
            {
                if (IsNestedEntity(entity, mi))
                {
                    MappingEntity nested = GetRelatedEntity(entity, mi);
                    var nestedValue = mi.GetValue(instance);
                    if (nestedValue != null)
                    {
                        var nestedClone = CloneEntity(nested, mi.GetValue(instance));
                        mi.SetValue(clone, nestedClone);
                    }
                }
            }

            return clone;
        }

        public override bool IsModified(MappingEntity entity, object instance, object original)
        {
            if (base.IsModified(entity, instance, original))
                return true;

            // need to check nested entities too
            foreach (var mi in GetMappedMembers(entity))
            {
                if (IsNestedEntity(entity, mi))
                {
                    MappingEntity nested = GetRelatedEntity(entity, mi);
                    if (IsModified(nested, mi.GetValue(instance), mi.GetValue(original)))
                        return true;
                }
            }

            return false;
        }

        public override QueryMapper CreateMapper(QueryTranslator translator)
        {
            return new AdvancedMapper(this, translator);
        }
    }

    public class AdvancedMapper : BasicMapper
    {
        private readonly AdvancedMapping _mapping;

        public AdvancedMapper(AdvancedMapping mapping, QueryTranslator translator)
            : base(mapping, translator)
        {
            _mapping = mapping;
        }

        public virtual IEnumerable<MappingTable> GetDependencyOrderedTables(MappingEntity entity)
        {
            var lookup = _mapping.GetTables(entity).ToLookup(t => _mapping.GetAlias(t));
            return
                _mapping.GetTables(entity).Sort(
                    t => _mapping.IsExtensionTable(t) ? lookup[_mapping.GetExtensionRelatedAlias(t)] : null);
        }

        public override EntityExpression GetEntityExpression(Expression root, MappingEntity entity)
        {
            // must be some complex type constructed from multiple columns
            var assignments = new List<EntityAssignment>();
            foreach (MemberInfo mi in _mapping.GetMappedMembers(entity))
            {
                if (!_mapping.IsAssociationRelationship(entity, mi))
                {
                    Expression me = _mapping.IsNestedEntity(entity, mi) ? GetEntityExpression(root, _mapping.GetRelatedEntity(entity, mi)) : GetMemberExpression(root, entity, mi);
                    if (me != null)
                    {
                        assignments.Add(new EntityAssignment(mi, me));
                    }
                }
            }

            return new EntityExpression(entity, BuildEntityExpression(entity, assignments));
        }

        public override Expression GetMemberExpression(Expression root, MappingEntity entity, MemberInfo member)
        {
            if (_mapping.IsNestedEntity(entity, member))
            {
                MappingEntity subEntity = _mapping.GetRelatedEntity(entity, member);
                return GetEntityExpression(root, subEntity);
            }
            else
            {
                return base.GetMemberExpression(root, entity, member);
            }
        }

        public override ProjectionExpression GetQueryExpression(MappingEntity entity)
        {
            var tables = _mapping.GetTables(entity);
            if (tables.Count <= 1)
            {
                return base.GetQueryExpression(entity);
            }

            var aliases = new Dictionary<string, TableAlias>();
            MappingTable rootTable = tables.Single(ta => !_mapping.IsExtensionTable(ta));
            var tex = new TableExpression(new TableAlias(), entity, _mapping.GetTableName(rootTable));
            aliases.Add(_mapping.GetAlias(rootTable), tex.Alias);
            Expression source = tex;

            foreach (MappingTable table in tables.Where(t => _mapping.IsExtensionTable(t)))
            {
                TableAlias joinedTableAlias = new TableAlias();
                string extensionAlias = _mapping.GetAlias(table);
                aliases.Add(extensionAlias, joinedTableAlias);

                List<string> keyColumns = _mapping.GetExtensionKeyColumnNames(table).ToList();
                List<MemberInfo> relatedMembers = _mapping.GetExtensionRelatedMembers(table).ToList();
                string relatedAlias = _mapping.GetExtensionRelatedAlias(table);

                TableAlias relatedTableAlias;
                aliases.TryGetValue(relatedAlias, out relatedTableAlias);

                TableExpression joinedTex = new TableExpression(joinedTableAlias, entity, _mapping.GetTableName(table));

                Expression cond = null;
                for (int i = 0, n = keyColumns.Count; i < n; i++)
                {
                    var memberType = TypeHelper.GetMemberType(relatedMembers[i]);
                    var colType = GetColumnType(entity, relatedMembers[i]);
                    var relatedColumn = new ColumnExpression(memberType, colType, relatedTableAlias,
                                                             _mapping.GetColumnName(entity, relatedMembers[i]));
                    var joinedColumn = new ColumnExpression(memberType, colType, joinedTableAlias, keyColumns[i]);
                    var eq = joinedColumn.Equal(relatedColumn);
                    cond = (cond != null) ? cond.And(eq) : eq;
                }

                source = new JoinExpression(JoinType.SingletonLeftOuter, source, joinedTex, cond);
            }

            var columns = new List<ColumnDeclaration>();
            GetColumns(entity, aliases, columns);
            SelectExpression root = new SelectExpression(new TableAlias(), columns, source, null);

            Expression projector = GetEntityExpression(root, entity);
            var selectAlias = new TableAlias();
            var pc = ColumnProjector.ProjectColumns(Translator.Linguist.Language, projector, null, selectAlias,
                                                    root.Alias);
            var proj = new ProjectionExpression(
                new SelectExpression(selectAlias, pc.Columns, root, null),
                pc.Projector
                );

            return (ProjectionExpression) Translator.Police.ApplyPolicy(proj, entity.ElementType);
        }

        private void GetColumns(MappingEntity entity, Dictionary<string, TableAlias> aliases,
                                List<ColumnDeclaration> columns)
        {
            foreach (MemberInfo mi in _mapping.GetMappedMembers(entity))
            {
                if (!_mapping.IsAssociationRelationship(entity, mi))
                {
                    if (_mapping.IsNestedEntity(entity, mi))
                    {
                        GetColumns(_mapping.GetRelatedEntity(entity, mi), aliases, columns);
                    }
                    else if (_mapping.IsColumn(entity, mi))
                    {
                        string name = _mapping.GetColumnName(entity, mi);
                        string aliasName = _mapping.GetAlias(entity, mi);
                        TableAlias alias;
                        aliases.TryGetValue(aliasName, out alias);
                        var colType = GetColumnType(entity, mi);
                        ColumnExpression ce = new ColumnExpression(TypeHelper.GetMemberType(mi), colType, alias, name);
                        ColumnDeclaration cd = new ColumnDeclaration(name, ce, colType);
                        columns.Add(cd);
                    }
                }
            }
        }

        public override Expression GetInsertExpression(MappingEntity entity, Expression instance,
                                                       LambdaExpression selector)
        {
            var tables = _mapping.GetTables(entity);
            if (tables.Count < 2)
            {
                return base.GetInsertExpression(entity, instance, selector);
            }

            var commands = new List<Expression>();

            var map = GetDependentGeneratedColumns(entity);
            var vexMap = new Dictionary<MemberInfo, Expression>();

            foreach (var table in GetDependencyOrderedTables(entity))
            {
                var tableAlias = new TableAlias();
                var tex = new TableExpression(tableAlias, entity, _mapping.GetTableName(table));
                MappingTable table1 = table; //Removed modified closure
                var assignments = GetColumnAssignments(tex, instance, entity,
                                                       (e, m) =>
                                                       _mapping.GetAlias(e, m) == _mapping.GetAlias(table1) &&
                                                       !_mapping.IsGenerated(e, m),
                                                       vexMap
                    );
                var totalAssignments = assignments.Concat(
                    GetRelatedColumnAssignments(tex, entity, table, vexMap)
                    );
                commands.Add(new InsertCommand(tex, totalAssignments));

                List<MemberInfo> members;
                if (map.TryGetValue(_mapping.GetAlias(table), out members))
                {
                    var d = GetDependentGeneratedVariableDeclaration(entity, table, members, instance, vexMap);
                    commands.Add(d);
                }
            }

            if (selector != null)
            {
                commands.Add(GetInsertResult(entity, instance, selector, vexMap));
            }

            return new BlockCommand(commands);
        }

        private Dictionary<string, List<MemberInfo>> GetDependentGeneratedColumns(MappingEntity entity)
        {
            //TODO: CanInsertPersonWithAssociatedAddressUsingExtensionTableCapability failing Person isn't getting the alias mapping correctly
            return
                (from xt in _mapping.GetTables(entity).Where(t => _mapping.IsExtensionTable(t))
                 group xt by _mapping.GetExtensionRelatedAlias(xt))
                    .ToDictionary(
                        g => g.Key,
                        g => g.SelectMany(xt => _mapping.GetExtensionRelatedMembers(xt)).Distinct().ToList()
                    );
        }

        // make a variable declaration / initialization for dependent generated values
        private CommandExpression GetDependentGeneratedVariableDeclaration(MappingEntity entity, MappingTable table,
                                                                           List<MemberInfo> members, Expression instance,
                                                                           Dictionary<MemberInfo, Expression> map)
        {
            // first make command that retrieves the generated ids if any
            DeclarationCommand genIdCommand = null;
            var generatedIds =
                _mapping.GetMappedMembers(entity).Where(
                    m => _mapping.IsPrimaryKey(entity, m) && _mapping.IsGenerated(entity, m)).ToList();
            if (generatedIds.Count > 0)
            {
                genIdCommand = GetGeneratedIdCommand(entity, members, map);

                // if that's all there is then just return the generated ids
                if (members.Count == generatedIds.Count)
                {
                    return genIdCommand;
                }
            }

            // next make command that retrieves the generated members
            // only consider members that were not generated ids
            members = members.Except(generatedIds).ToList();

            var tableAlias = new TableAlias();
            var tex = new TableExpression(tableAlias, entity, _mapping.GetTableName(table));

            Expression where;
            if (generatedIds.Count > 0)
            {
                where = generatedIds.Select((m, i) =>
                                            GetMemberExpression(tex, entity, m).Equal(map[m])
                    ).Aggregate((x, y) => x.And(y));
            }
            else
            {
                where = GetIdentityCheck(tex, entity, instance);
            }

            TableAlias selectAlias = new TableAlias();
            var columns = new List<ColumnDeclaration>();
            var variables = new List<VariableDeclaration>();
            foreach (var mi in members)
            {
                ColumnExpression col = (ColumnExpression) GetMemberExpression(tex, entity, mi);
                columns.Add(new ColumnDeclaration(_mapping.GetColumnName(entity, mi), col, col.QueryType));
                ColumnExpression vcol = new ColumnExpression(col.Type, col.QueryType, selectAlias, col.Name);
                variables.Add(new VariableDeclaration(mi.Name, col.QueryType, vcol));
                map.Add(mi, new VariableExpression(mi.Name, col.Type, col.QueryType));
            }

            var genMembersCommand = new DeclarationCommand(variables,
                                                           new SelectExpression(selectAlias, columns, tex, where));

            if (genIdCommand != null)
            {
                return new BlockCommand(genIdCommand, genMembersCommand);
            }

            return genMembersCommand;
        }

        private IEnumerable<ColumnAssignment> GetColumnAssignments(
            Expression table, Expression instance, MappingEntity entity,
            Func<MappingEntity, MemberInfo, bool> fnIncludeColumn,
            Dictionary<MemberInfo, Expression> map)
        {
            foreach (var m in _mapping.GetMappedMembers(entity))
            {
                if (_mapping.IsColumn(entity, m) && fnIncludeColumn(entity, m))
                {
                    yield return new ColumnAssignment(
                        (ColumnExpression) GetMemberExpression(table, entity, m),
                        GetMemberAccess(instance, m, map)
                        );
                }
                else if (_mapping.IsNestedEntity(entity, m))
                {
                    var assignments = GetColumnAssignments(
                        table,
                        Expression.MakeMemberAccess(instance, m),
                        _mapping.GetRelatedEntity(entity, m),
                        fnIncludeColumn,
                        map
                        );
                    foreach (var ca in assignments)
                    {
                        yield return ca;
                    }
                }
            }
        }

        private IEnumerable<ColumnAssignment> GetRelatedColumnAssignments(Expression expr, MappingEntity entity,
                                                                          MappingTable table,
                                                                          Dictionary<MemberInfo, Expression> map)
        {
            if (_mapping.IsExtensionTable(table))
            {
                var keyColumns = _mapping.GetExtensionKeyColumnNames(table).ToArray();
                var relatedMembers = _mapping.GetExtensionRelatedMembers(table).ToArray();
                for (int i = 0, n = keyColumns.Length; i < n; i++)
                {
                    MemberInfo member = relatedMembers[i];
                    Expression exp = map[member];
                    yield return new ColumnAssignment((ColumnExpression) GetMemberExpression(expr, entity, member), exp)
                        ;
                }
            }
        }

        private Expression GetMemberAccess(Expression instance, MemberInfo member,
                                           Dictionary<MemberInfo, Expression> map)
        {
            Expression exp;
            if (map == null || !map.TryGetValue(member, out exp))
            {
                exp = Expression.MakeMemberAccess(instance, member);
            }
            return exp;
        }

        public override Expression GetUpdateExpression(MappingEntity entity, Expression instance,
                                                       LambdaExpression updateCheck, LambdaExpression selector,
                                                       Expression @else)
        {
            var tables = _mapping.GetTables(entity);
            if (tables.Count < 2)
            {
                return base.GetUpdateExpression(entity, instance, updateCheck, selector, @else);
            }

            var commands = new List<Expression>();
            foreach (var table in GetDependencyOrderedTables(entity))
            {
                TableExpression tex = new TableExpression(new TableAlias(), entity, _mapping.GetTableName(table));
                MappingTable table1 = table; //Moddified Closure removal
                var assignments = GetColumnAssignments(tex, instance, entity,
                                                       (e, m) =>
                                                       _mapping.GetAlias(e, m) == _mapping.GetAlias(table1) &&
                                                       _mapping.IsUpdatable(e, m), null);
                var where = GetIdentityCheck(tex, entity, instance);
                commands.Add(new UpdateCommand(tex, where, assignments));
            }

            if (selector != null)
            {
                commands.Add(
                    new IFCommand(
                        Translator.Linguist.Language.GetRowsAffectedExpression(commands[commands.Count - 1]).GreaterThan
                            (Expression.Constant(0)),
                        GetUpdateResult(entity, instance, selector),
                        @else
                        )
                    );
            }
            else if (@else != null)
            {
                commands.Add(
                    new IFCommand(
                        Translator.Linguist.Language.GetRowsAffectedExpression(commands[commands.Count - 1]).
                            LessThanOrEqual(Expression.Constant(0)),
                        @else,
                        null
                        )
                    );
            }

            Expression block = new BlockCommand(commands);

            if (updateCheck != null)
            {
                var test = GetEntityStateTest(entity, instance, updateCheck);
                return new IFCommand(test, block, null);
            }

            return block;
        }

        private Expression GetIdentityCheck(TableExpression root, MappingEntity entity, Expression instance,
                                            MappingTable table)
        {
            if (_mapping.IsExtensionTable(table))
            {
                var keyColNames = _mapping.GetExtensionKeyColumnNames(table).ToArray();
                var relatedMembers = _mapping.GetExtensionRelatedMembers(table).ToArray();

                Expression where = null;
                for (int i = 0, n = keyColNames.Length; i < n; i++)
                {
                    var relatedMember = relatedMembers[i];
                    var cex = new ColumnExpression(TypeHelper.GetMemberType(relatedMember),
                                                   GetColumnType(entity, relatedMember), root.Alias, keyColNames[n]);
                    var nex = GetMemberExpression(instance, entity, relatedMember);
                    var eq = cex.Equal(nex);
                    where = (where != null) ? where.And(eq) : where;
                }
                return where;
            }
            return base.GetIdentityCheck(root, entity, instance);
        }

        public override Expression GetDeleteExpression(MappingEntity entity, Expression instance,
                                                       LambdaExpression deleteCheck)
        {
            var tables = _mapping.GetTables(entity);
            if (tables.Count < 2)
            {
                return base.GetDeleteExpression(entity, instance, deleteCheck);
            }

            var commands = new List<Expression>();
            foreach (var table in GetDependencyOrderedTables(entity).Reverse())
            {
                TableExpression tex = new TableExpression(new TableAlias(), entity, _mapping.GetTableName(table));
                var where = GetIdentityCheck(tex, entity, instance);
                commands.Add(new DeleteCommand(tex, where));
            }

            Expression block = new BlockCommand(commands);

            if (deleteCheck != null)
            {
                var test = GetEntityStateTest(entity, instance, deleteCheck);
                return new IFCommand(test, block, null);
            }

            return block;
        }

        protected override Expression GetInsertResult(MappingEntity entity, Expression instance,
                                                      LambdaExpression selector, Dictionary<MemberInfo, Expression> map)
        {
            var tables = _mapping.GetTables(entity);
            if (tables.Count <= 1)
            {
                return base.GetInsertResult(entity, instance, selector, map);
            }
            var aliases = new Dictionary<string, TableAlias>();
            MappingTable rootTable = tables.Single(ta => !_mapping.IsExtensionTable(ta));
            var tableExpression = new TableExpression(new TableAlias(), entity, _mapping.GetTableName(rootTable));
            var aggregator = Aggregator.GetAggregator(selector.Body.Type,
                                                      typeof (IEnumerable<>).MakeGenericType(selector.Body.Type));
            aliases.Add(_mapping.GetAlias(rootTable), tableExpression.Alias);
            Expression source = tableExpression;
            foreach (MappingTable table in tables.Where(t => _mapping.IsExtensionTable(t)))
            {
                TableAlias joinedTableAlias = new TableAlias();
                string extensionAlias = _mapping.GetAlias(table);
                aliases.Add(extensionAlias, joinedTableAlias);
                List<string> keyColumns = _mapping.GetExtensionKeyColumnNames(table).ToList();
                List<MemberInfo> relatedMembers = _mapping.GetExtensionRelatedMembers(table).ToList();
                string relatedAlias = _mapping.GetExtensionRelatedAlias(table);
                TableAlias relatedTableAlias;
                aliases.TryGetValue(relatedAlias, out relatedTableAlias);
                TableExpression joinedTex = new TableExpression(joinedTableAlias, entity, _mapping.GetTableName(table));
                Expression cond = null;
                for (int i = 0, n = keyColumns.Count; i < n; i++)
                {
                    var memberType = TypeHelper.GetMemberType(relatedMembers[i]);
                    var colType = GetColumnType(entity, relatedMembers[i]);
                    var relatedColumn = new ColumnExpression(memberType, colType, relatedTableAlias,
                                                             _mapping.GetColumnName(entity, relatedMembers[i]));
                    var joinedColumn = new ColumnExpression(memberType, colType, joinedTableAlias, keyColumns[i]);
                    var eq = joinedColumn.Equal(relatedColumn);
                    cond = (cond != null) ? cond.And(eq) : eq;
                }
                source = new JoinExpression(JoinType.SingletonLeftOuter, source, joinedTex, cond);
            }
            Expression where;
            DeclarationCommand genIdCommand = null;
            var generatedIds =
                _mapping.GetMappedMembers(entity).Where(
                    m => _mapping.IsPrimaryKey(entity, m) && _mapping.IsGenerated(entity, m)).ToList();
            if (generatedIds.Count > 0)
            {
                if (map == null || !generatedIds.Any(m => map.ContainsKey(m)))
                {
                    var localMap = new Dictionary<MemberInfo, Expression>();
                    genIdCommand = GetGeneratedIdCommand(entity, generatedIds.ToList(), localMap);
                    map = localMap;
                }
                var mex = selector.Body as MemberExpression;
                if (mex != null && _mapping.IsPrimaryKey(entity, mex.Member) && _mapping.IsGenerated(entity, mex.Member))
                {
                    if (genIdCommand != null)
                    {
                        return new ProjectionExpression(
                            genIdCommand.Source,
                            new ColumnExpression(mex.Type, genIdCommand.Variables[0].QueryType,
                                                 genIdCommand.Source.Alias, genIdCommand.Source.Columns[0].Name),
                            aggregator
                            );
                    }
                    TableAlias alias = new TableAlias();
                    var colType = GetColumnType(entity, mex.Member);
                    return new ProjectionExpression(
                        new SelectExpression(alias, new[] {new ColumnDeclaration("", map[mex.Member], colType)},
                                             null, null),
                        new ColumnExpression(TypeHelper.GetMemberType(mex.Member), colType, alias, ""),
                        aggregator
                        );
                }
                where = generatedIds.Select((m, i) =>
                                            GetMemberExpression(source, entity, m).Equal(map[m])
                    ).Aggregate((x, y) => x.And(y));
            }
            else
            {
                where = GetIdentityCheck(tableExpression, entity, instance);
            }
            var columns = new List<ColumnDeclaration>();
            GetColumns(entity, aliases, columns);
            SelectExpression root = new SelectExpression(new TableAlias(), columns, source, null);
            Expression typeProjector = GetEntityExpression(tableExpression, entity);
            Expression selection = DbExpressionReplacer.Replace(selector.Body, selector.Parameters[0], typeProjector);
            TableAlias newAlias = new TableAlias();
            var pc = ColumnProjector.ProjectColumns(Translator.Linguist.Language, selection, null, newAlias,
                                                    tableExpression.Alias);
            var pe = new ProjectionExpression(
                new SelectExpression(newAlias, pc.Columns, root, where),
                pc.Projector,
                aggregator
                );
            if (genIdCommand != null)
            {
                return new BlockCommand(genIdCommand, pe);
            }
            return pe;
        }

        protected override Expression GetIdentityCheck(Expression root, MappingEntity entity, Expression instance)
        {
            var tables = _mapping.GetTables(entity);
            if (tables.Count <= 1) //Default to base if there are no extension tables
            {
                base.GetIdentityCheck(root, entity, instance);
            }
            var members = _mapping.GetMappedMembers(entity);
            Expression retVal = null;
            foreach (var memberInfo in members)
            {
                if (_mapping.IsPrimaryKey(entity, memberInfo))
                {
                    if (retVal == null)
                    {
                        retVal =
                            GetMemberExpression(root, entity, memberInfo).Equal(Expression.MakeMemberAccess(instance,
                                                                                                            memberInfo));
                    }
                    else
                    {
                        retVal.And(
                            GetMemberExpression(root, entity, memberInfo).Equal(Expression.MakeMemberAccess(instance,
                                                                                                            memberInfo)));
                    }
                }
            }
            return retVal;
        }
    }
}