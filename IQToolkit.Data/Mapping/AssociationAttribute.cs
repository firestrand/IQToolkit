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
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class AssociationAttribute : MemberAttribute
    {
        public string Name { get; set; }
        public string KeyMembers { get; set; }
        public string RelatedEntityID { get; set; }
        public Type RelatedEntityType { get; set; }
        public string RelatedKeyMembers { get; set; }
        public bool IsForeignKey { get; set; }
    }
}
