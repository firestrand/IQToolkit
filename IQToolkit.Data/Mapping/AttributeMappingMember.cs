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
    sealed class AttributeMappingMember
    {
        readonly MemberInfo _member;
        readonly MemberAttribute _attribute;
        readonly AttributeMappingEntity _nested;

        internal AttributeMappingMember(MemberInfo member, MemberAttribute attribute, AttributeMappingEntity nested)
        {
            this._member = member;
            this._attribute = attribute;
            this._nested = nested;
        }

        internal MemberInfo Member
        {
            get { return this._member; }
        }

        internal ColumnAttribute Column
        {
            get { return this._attribute as ColumnAttribute; }
        }

        internal AssociationAttribute Association
        {
            get { return this._attribute as AssociationAttribute; }
        }

        internal AttributeMappingEntity NestedEntity
        {
            get { return this._nested; }
        }
    }
}
