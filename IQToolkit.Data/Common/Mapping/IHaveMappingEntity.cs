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
    public interface IHaveMappingEntity
    {
        MappingEntity Entity { get; }
    }
}
