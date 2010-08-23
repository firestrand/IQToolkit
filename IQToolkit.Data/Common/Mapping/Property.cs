using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace IQToolkit.Data.Common.Mapping
{
    public class Property
    {
        public string PropertyId { get; set; }
        public Type PropertyType { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
    public class ColumnProperty : Property
    {
        public SqlDbType ColumnSqlDbType { get; set; }
    }
    public class AssociationProperty : Property
    {
        public IList<ColumnProperty> EntityKeys { get; set; }
        public MappingEntity RelatedEntity { get; set; }
        public IList<ColumnProperty> RelatedKeys { get; set; }
    }
}
