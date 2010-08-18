using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;

namespace IQToolkit.Data.Mapping
{
    using Common;
    sealed class AttributeMappingEntity : MappingEntity
    {
        readonly string _tableId;
        readonly Type _elementType;
        readonly Type _entityType;
        readonly ReadOnlyCollection<MappingTable> _tables;
        readonly Dictionary<string, AttributeMappingMember> _mappingMembers;

        internal AttributeMappingEntity(Type elementType, string tableId, Type entityType, IEnumerable<TableBaseAttribute> attrs, IEnumerable<AttributeMappingMember> mappingMembers)
        {
            this._tableId = tableId;
            this._elementType = elementType;
            this._entityType = entityType;
            this._tables = attrs.Select(a => (MappingTable)new AttributeMappingTable(this, a)).ToReadOnly();
            this._mappingMembers = mappingMembers.ToDictionary(mm => mm.Member.Name);
        }

        public override string TableId
        {
            get { return this._tableId; }
        }

        public override Type ElementType
        {
            get { return this._elementType; }
        }

        public override Type EntityType
        {
            get { return this._entityType; }
        }

        internal ReadOnlyCollection<MappingTable> Tables
        {
            get { return this._tables; }
        }

        internal AttributeMappingMember GetMappingMember(string name)
        {
            AttributeMappingMember mm = null;
            this._mappingMembers.TryGetValue(name, out mm);
            return mm;
        }

        internal IEnumerable<MemberInfo> MappedMembers
        {
            get { return this._mappingMembers.Values.Select(mm => mm.Member); }
        }
    }
}
