using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using IQToolkit.Data.Common.Mapping;

namespace IQToolkit.Data.Mapping
{
    using Common;
    sealed class AttributeMappingEntity : MappingEntity
    {
        readonly ReadOnlyCollection<MappingTable> _tables;
        readonly ConcurrentDictionary<string, MappingMember> _mappingMembers;

        internal AttributeMappingEntity(string tableId, IEnumerable<TableBaseAttribute> attrs, IEnumerable<MappingMember> mappingMembers):base(tableId)
        {
            this._tables = attrs.Select(a => (MappingTable)new AttributeMappingTable(this, a)).ToReadOnly();
            this._mappingMembers = new ConcurrentDictionary<string, MappingMember>(mappingMembers.ToDictionary(mm => mm..Member.Name));
        }

        internal ReadOnlyCollection<MappingTable> Tables
        {
            get { return this._tables; }
        }

        internal MappingMember GetMappingMember(string name)
        {
            MappingMember mm = null;
            this._mappingMembers.TryGetValue(name, out mm);
            return mm;
        }

        internal IEnumerable<MemberInfo> MappedMembers
        {
            get { return this._mappingMembers.Values.Select(mm => mm.Member); }
        }
    }
}
