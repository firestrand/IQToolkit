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
    sealed class AttributeMapper : AdvancedMapper
    {
        private AttributeMapping _mapping;

        public AttributeMapper(AttributeMapping mapping, QueryTranslator translator)
            : base(mapping, translator)
        {
            this._mapping = mapping;
        }
    }
}
