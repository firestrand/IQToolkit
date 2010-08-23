using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using IQToolkit.Data.Common;
using IQToolkit.Data.Common.Mapping;

namespace IQToolkit.Data.Mapping
{
    sealed class MappingMember
    {
        readonly MemberAttribute _attribute;
        readonly MappingEntity _nested;

        internal MappingMember(Type type, MemberAttribute attribute, AttributeMappingEntity nested)
        {
            MemberType = type;
            this._attribute = attribute;
            this._nested = nested;
        }

        internal Type MemberType { get; private set; }

        internal ColumnAttribute Column
        {
            get { return this._attribute as ColumnAttribute; }
        }

        internal AssociationAttribute Association
        {
            get { return this._attribute as AssociationAttribute; }
        }

        internal MappingEntity NestedEntity
        {
            get { return this._nested; }
        }
    }
}
