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
    public abstract class MappingEntity
    {
        public abstract string TableId { get; }
        public abstract Type ElementType { get; }
        public abstract Type EntityType { get; }
        /// <summary>
        /// Override ToString to give MappingEntity information
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("TableId:{0}\tElementType:{1}\tEntityType:{2}", TableId, ElementType, EntityType);
        }
        /// <summary>
        ///  Override equals to compare elements not object references
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            MappingEntity passedObj = obj as MappingEntity;
            if (passedObj == null) //Type case failed, wrong type
                return false;
            return passedObj.TableId == this.TableId &&
                passedObj.ElementType == this.ElementType &&
                passedObj.EntityType == this.EntityType;
        }
        /// <summary>
        /// Override GetHashCode and use ToString which should be equal for equal instance values
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
    }
}
