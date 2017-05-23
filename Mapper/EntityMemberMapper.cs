﻿using SZORM.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SZORM.Core.Emit;
using SZORM.Infrastructure;
using SZORM.InternalExtensions;

namespace SZORM.Mapper
{
    public class EntityMemberMapper
    {
        Dictionary<MemberInfo, IMRM> _mappingMemberMRMContainer;
        Dictionary<MemberInfo, Action<object, object>> _navigationMemberSetters;

        EntityMemberMapper(Type t)
        {
            this.Type = t;
            this.Init();
        }

        void Init()
        {
            Type t = this.Type;
            var members = t.GetMembers(BindingFlags.Public | BindingFlags.Instance);

            Dictionary<MemberInfo, IMRM> mappingMemberMRMContainer = new Dictionary<MemberInfo, IMRM>();
            Dictionary<MemberInfo, Action<object, object>> navigationMemberSetters = new Dictionary<MemberInfo, Action<object, object>>();

            foreach (var member in members)
            {
                Type memberType = null;
                PropertyInfo prop = null;
                FieldInfo field = null;

                if ((prop = member as PropertyInfo) != null)
                {
                    if (prop.GetSetMethod() == null)
                        continue;//对于没有公共的 setter 直接跳过
                    memberType = prop.PropertyType;
                }
                else if ((field = member as FieldInfo) != null)
                {
                    memberType = field.FieldType;
                }
                else
                    continue;//只支持公共属性和字段

                if (MappingTypeSystem.IsMappingType(memberType))
                {
                    IMRM mrm = MRMHelper.CreateMRM(member);
                    mappingMemberMRMContainer.Add(member, mrm);
                }
                else
                {
                    if (prop != null)
                    {
                        Action<object, object> valueSetter = DelegateGenerator.CreateValueSetter(prop);
                        navigationMemberSetters.Add(member, valueSetter);
                    }
                    else if (field != null)
                    {
                        Action<object, object> valueSetter = DelegateGenerator.CreateValueSetter(field);
                        navigationMemberSetters.Add(member, valueSetter);
                    }
                    else
                        continue;

                    continue;
                }
            }

            this._mappingMemberMRMContainer = Utils.Clone(mappingMemberMRMContainer);
            this._navigationMemberSetters = Utils.Clone(navigationMemberSetters);
        }

        public Type Type { get; private set; }

        public IMRM TryGetMemberMapper(MemberInfo memberInfo)
        {
            memberInfo = memberInfo.AsReflectedMemberOf(this.Type);
            IMRM mapper = null;
            this._mappingMemberMRMContainer.TryGetValue(memberInfo, out mapper);
            return mapper;
        }
        public Action<object, object> TryGetNavigationMemberSetter(MemberInfo memberInfo)
        {
            memberInfo = memberInfo.AsReflectedMemberOf(this.Type);
            Action<object, object> valueSetter = null;
            this._navigationMemberSetters.TryGetValue(memberInfo, out valueSetter);
            return valueSetter;
        }

        static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, EntityMemberMapper> InstanceCache = new System.Collections.Concurrent.ConcurrentDictionary<Type, EntityMemberMapper>();

        public static EntityMemberMapper GetInstance(Type type)
        {
            EntityMemberMapper instance;
            if (!InstanceCache.TryGetValue(type, out instance))
            {
                lock (type)
                {
                    if (!InstanceCache.TryGetValue(type, out instance))
                    {
                        instance = new EntityMemberMapper(type);
                        InstanceCache.GetOrAdd(type, instance);
                    }
                }
            }

            return instance;
        }
    }
}
