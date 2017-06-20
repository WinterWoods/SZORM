﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SZORM.Descriptors;
using SZORM.Infrastructure;
using SZORM.Mapper;
using SZORM.Query.Mapping;

namespace SZORM.Query.Internals
{
    class InternalSqlQuery<T> : IEnumerable<T>, IEnumerable
    {
        DbContext _dbContext;
        string _sql;
        CommandType _cmdType;
        DbParam[] _parameters;

        public InternalSqlQuery(DbContext dbContext, string sql, CommandType cmdType, DbParam[] parameters)
        {
            this._dbContext = dbContext;
            this._sql = sql;
            this._cmdType = cmdType;
            this._parameters = parameters;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new QueryEnumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }


        struct QueryEnumerator : IEnumerator<T>
        {
            InternalSqlQuery<T> _internalSqlQuery;

            IDataReader _reader;
            IObjectActivator _objectActivator;

            T _current;
            bool _hasFinished;
            bool _disposed;
            public QueryEnumerator(InternalSqlQuery<T> internalSqlQuery)
            {
                this._internalSqlQuery = internalSqlQuery;
                this._reader = null;
                this._objectActivator = null;

                this._current = default(T);
                this._hasFinished = false;
                this._disposed = false;
            }

            public T Current { get { return this._current; } }

            object IEnumerator.Current { get { return this._current; } }

            public bool MoveNext()
            {
                if (this._hasFinished || this._disposed)
                    return false;

                if (this._reader == null)
                {
                    this.Prepare();
                }

                if (this._reader.Read())
                {
                    this._current = (T)this._objectActivator.CreateInstance(this._reader);
                    return true;
                }
                else
                {
                    this._reader.Close();
                    this._current = default(T);
                    this._hasFinished = true;
                    return false;
                }
            }

            public void Dispose()
            {
                if (this._disposed)
                    return;

                if (this._reader != null)
                {
                    if (!this._reader.IsClosed)
                        this._reader.Close();
                    this._reader.Dispose();
                    this._reader = null;
                }

                if (!this._hasFinished)
                {
                    this._hasFinished = true;
                }

                this._current = default(T);
                this._disposed = true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            void Prepare()
            {
                Type type = typeof(T);

                if (MappingTypeSystem.IsMappingType(type))
                {
                    Mapping.MappingField mf = new MappingField(type, 0);
                    this._objectActivator = mf.CreateObjectActivator();
                    this._reader = this.ExecuteReader();
                    return;
                }

                this._reader = this.ExecuteReader();
                this._objectActivator = GetObjectActivator(type, this._reader);
            }
            IDataReader ExecuteReader()
            {
                IDataReader reader = this._internalSqlQuery._dbContext.InternalAdoSession.ExecuteReader(this._internalSqlQuery._sql, this._internalSqlQuery._parameters, this._internalSqlQuery._cmdType);
                return reader;
            }

            static ObjectActivator GetObjectActivator(Type type, IDataReader reader)
            {
                List<CacheInfo> caches;
                if (!ObjectActivatorCache.TryGetValue(type, out caches))
                {
                    if (!Monitor.TryEnter(type))
                    {
                        return CreateObjectActivator(type, reader);
                    }

                    try
                    {
                        caches = ObjectActivatorCache.GetOrAdd(type, new List<CacheInfo>(1));
                    }
                    finally
                    {
                        Monitor.Exit(type);
                    }
                }

                CacheInfo cache = TryGetCacheInfoFromList(caches, reader);

                if (cache == null)
                {
                    lock (caches)
                    {
                        cache = TryGetCacheInfoFromList(caches, reader);
                        if (cache == null)
                        {
                            ObjectActivator activator = CreateObjectActivator(type, reader);
                            cache = new CacheInfo(activator, reader);
                            caches.Add(cache);
                        }
                    }
                }

                return cache.ObjectActivator;
            }
            static ObjectActivator CreateObjectActivator(Type type, IDataReader reader)
            {
                ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    throw new ArgumentException(string.Format("The type of '{0}' does't define a none parameter constructor.", type.FullName));

                EntityConstructorDescriptor constructorDescriptor = EntityConstructorDescriptor.GetInstance(constructor);
                EntityMemberMapper mapper = constructorDescriptor.GetEntityMemberMapper();
                Func<IDataReader, ReaderOrdinalEnumerator, ObjectActivatorEnumerator, object> instanceCreator = constructorDescriptor.GetInstanceCreator();
                List<IValueSetter> memberSetters = PrepareValueSetters(type, reader, mapper);
                return new ObjectActivator(instanceCreator, null, null, memberSetters, null);
            }
            static List<IValueSetter> PrepareValueSetters(Type type, IDataReader reader, EntityMemberMapper mapper)
            {
                List<IValueSetter> memberSetters = new List<IValueSetter>(reader.FieldCount);

                MemberInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
                MemberInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField);
                List<MemberInfo> members = new List<MemberInfo>(properties.Length + fields.Length);
                members.AddRange(properties);
                members.AddRange(fields);

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string name = reader.GetName(i);
                    var member = members.Where(a => a.Name == name).FirstOrDefault();
                    if (member == null)
                    {
                        member = members.Where(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (member == null)
                            continue;
                    }
                    IMRM mMapper = mapper.TryGetMappingMemberMapper(member);
                    if (mMapper == null)
                        continue;

                    MappingMemberBinder memberBinder = new MappingMemberBinder(mMapper, i);
                    memberSetters.Add(memberBinder);
                }

                return memberSetters;
            }
            static CacheInfo TryGetCacheInfoFromList(List<CacheInfo> caches, IDataReader reader)
            {
                CacheInfo cache = null;
                for (int i = 0; i < caches.Count; i++)
                {
                    var item = caches[i];
                    if (item.IsTheSameFields(reader))
                    {
                        cache = item;
                        break;
                    }
                }

                return cache;
            }

            static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, List<CacheInfo>> ObjectActivatorCache = new System.Collections.Concurrent.ConcurrentDictionary<Type, List<CacheInfo>>();
        }

        public class CacheInfo
        {
            Tuple<string, Type>[] _readerFields;
            ObjectActivator _objectActivator;
            public CacheInfo(ObjectActivator activator, IDataReader reader)
            {
                int fieldCount = reader.FieldCount;
                var readerFields = new Tuple<string, Type>[fieldCount];

                for (int i = 0; i < fieldCount; i++)
                {
                    readerFields[i] = new Tuple<string, Type>(reader.GetName(i), reader.GetFieldType(i));
                }

                this._readerFields = readerFields;
                this._objectActivator = activator;
            }

            public ObjectActivator ObjectActivator { get { return this._objectActivator; } }

            public bool IsTheSameFields(IDataReader reader)
            {
                Tuple<string, Type>[] readerFields = this._readerFields;
                int fieldCount = reader.FieldCount;

                if (fieldCount != readerFields.Length)
                    return false;

                for (int i = 0; i < fieldCount; i++)
                {
                    Tuple<string, Type> tuple = readerFields[i];
                    if (reader.GetFieldType(i) != tuple.Item2 || reader.GetName(i) != tuple.Item1)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

    }
}
