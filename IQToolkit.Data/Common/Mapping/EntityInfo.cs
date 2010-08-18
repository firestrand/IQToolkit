using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IQToolkit.Data.Common
{
    public struct EntityInfo
    {
        readonly object instance;
        readonly MappingEntity mapping;

        public EntityInfo(object instance, MappingEntity mapping)
        {
            this.instance = instance;
            this.mapping = mapping;
        }

        public object Instance
        {
            get { return this.instance; }
        }

        public MappingEntity Mapping
        {
            get { return this.mapping; }
        }
    }
}
