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
    sealed class AttributeMappingTable : MappingTable
    {
        readonly AttributeMappingEntity _entity;
        readonly TableBaseAttribute _attribute;

        internal AttributeMappingTable(AttributeMappingEntity entity, TableBaseAttribute attribute)
        {
            this._entity = entity;
            this._attribute = attribute;
        }

        public AttributeMappingEntity Entity
        {
            get { return this._entity; }
        }

        public TableBaseAttribute Attribute
        {
            get { return this._attribute; }
        }
    }
}
