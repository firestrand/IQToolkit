using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IQToolkit.Data.Common;
using IQToolkit.Data.Common.Mapping;

namespace IQToolkit.Data
{
    public class EntityTable<T> : Query<T>, IEntityTable<T>, IHaveMappingEntity
    {
        readonly MappingEntity _entity;
        readonly EntityProvider _provider;

        public EntityTable(EntityProvider provider, MappingEntity entity)
            : base(provider, typeof(IEntityTable<T>))
        {
            this._provider = provider;
            this._entity = entity;
        }

        public MappingEntity Entity
        {
            get { return this._entity; }
        }

        new public IEntityProvider Provider
        {
            get { return this._provider; }
        }

        public string TableId
        {
            get { return this._entity.TableId; }
        }

        public Type EntityType
        {
            get { return this._entity.EntityType; }
        }

        public T GetById(object id)
        {
            var dbProvider = this.Provider;
            if (dbProvider != null)
            {
                IEnumerable<object> keys = id as IEnumerable<object>;
                if (keys == null)
                    keys = new object[] { id };
                Expression query = ((EntityProvider)dbProvider).Mapping.GetPrimaryKeyQuery(this._entity, this.Expression, keys.Select(v => Expression.Constant(v)).ToArray());
                return this.Provider.Execute<T>(query);
            }
            return default(T);
        }

        object IEntityTable.GetById(object id)
        {
            return this.GetById(id);
        }

        public int Insert(T instance)
        {
            return Updatable.Insert(this, instance);
        }

        int IEntityTable.Insert(object instance)
        {
            return this.Insert((T)instance);
        }

        public int Delete(T instance)
        {
            return Updatable.Delete(this, instance);
        }

        int IEntityTable.Delete(object instance)
        {
            return this.Delete((T)instance);
        }

        public int Update(T instance)
        {
            return Updatable.Update(this, instance);
        }

        int IEntityTable.Update(object instance)
        {
            return this.Update((T)instance);
        }

        public int InsertOrUpdate(T instance)
        {
            return Updatable.InsertOrUpdate(this, instance);
        }

        int IEntityTable.InsertOrUpdate(object instance)
        {
            return this.InsertOrUpdate((T)instance);
        }
    }
}