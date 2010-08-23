using System;
using System.Collections.Concurrent;

namespace IQToolkit.Data.Common.Mapping
{
    public class MappingEntity
    {
        public string EntityId { get; private set; }
        public ConcurrentDictionary<string, Property> Properties { get; private set; }
        public MappingEntity()
        {
        }

        public MappingEntity(string entityId)
        {
            EntityId = entityId;
            Properties = new ConcurrentDictionary<string, Property>();
        }

        /// <summary>
        /// Override ToString to give MappingEntity information
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("EntityId:{0}",EntityId);
        }
        /// <summary>
        ///  Override equals to compare elements not object references
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            MappingEntity passedObj = obj as MappingEntity;
            if (passedObj == null) //Type cast failed, wrong type
                return false;
            return passedObj.EntityId == this.EntityId;
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
