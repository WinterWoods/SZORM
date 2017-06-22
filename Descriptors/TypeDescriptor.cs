using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using SZORM.Core.Emit;
using SZORM.Core.Visitors;
using SZORM.DbExpressions;
using SZORM.Exceptions;
using SZORM.Infrastructure;
using SZORM.InternalExtensions;

namespace SZORM.Descriptors
{
    public class TypeDescriptors
    {
       
        static readonly ConcurrentDictionary<Type, List<Type>> InstanceCache = new ConcurrentDictionary<Type, List<Type>>();
        //如果初始化多个链接,要进行多次缓存
        public static ConcurrentDictionary<Type,TypeDescriptor> GetTypeDescriptors(DbContext dbContext)
        {
            Type type = dbContext.GetType();
            //必须是从我的基类继承过来的才可以
            if (type.BaseType.FullName != "SZORM.DbContext") throw new SZORMException("必须继承SZORM.DbContext");
            ConcurrentDictionary<Type,TypeDescriptor> result=new ConcurrentDictionary<Type, TypeDescriptor>();
            List<Type> types=null;
            if (!InstanceCache.TryGetValue(type,out types))
            {
                types = new List<Type>();
                ConcurrentDictionary<Type, TypeDescriptor> _result = new ConcurrentDictionary<Type, TypeDescriptor>();
                var properties = type.GetProperties();
                foreach (var item in properties)
                {
                    Type _tmp = item.PropertyType.GetGenericArguments()[0];
                    TypeDescriptor instance = new TypeDescriptor(_tmp, item, type.GetMember(item.Name)[0]);

                    _result.TryAdd(_tmp, instance);
                    types.Add(_tmp);
                }
                result = _result;
            }
            foreach (var item in types)
            {
                var typeDesc = TypeDescriptor.GetDescriptor(item);
                object obj = Assembly.GetAssembly(typeDesc.PropertyInfo.PropertyType).CreateInstance(typeDesc.PropertyInfo.PropertyType.FullName);
                SetValue(dbContext,obj, typeDesc.MemberInfo);
                SetDbContextValue(dbContext, obj, typeDesc.DbContextMemberInfo);
                result.TryAdd(item,typeDesc);
            }
            return result;
        }
        public static TypeDescriptor GetTypeDescriptor(DbContext  dbContext ,Type type)
        {
            TypeDescriptor result;
            if (GetTypeDescriptors(dbContext).TryGetValue(type,out result))
            {
                throw new Exception("错误错误,联系管理员或修改代码吧.");
            }
            return result;
        }
        public static void SetValue(DbContext dbContext, object value, MemberInfo membrInfo)
        {
            Action<object, object> _valueSetter;
            if (Monitor.TryEnter(dbContext))
            {
                try
                {
                    _valueSetter = DelegateGenerator.CreateValueSetter(membrInfo);
                }
                finally
                {
                    Monitor.Exit(dbContext);
                }
            }
            else
            {
                membrInfo.SetMemberValue(dbContext, value);
                return;
            }

            _valueSetter(dbContext, value);
        }
        public static void SetDbContextValue(DbContext dbContext, object dbSetObject, MemberInfo membrInfo)
        {
            Action<object, object> _valueSetter;
            if (Monitor.TryEnter(dbSetObject))
            {
                try
                {
                    _valueSetter = DelegateGenerator.CreateValueSetter(membrInfo);
                }
                finally
                {
                    Monitor.Exit(dbSetObject);
                }
            }
            else
            {
                membrInfo.SetMemberValue(dbSetObject, dbContext);
                return;
            }

            _valueSetter(dbSetObject, dbContext);
        }


    }

    /// <summary>
    /// 类型描述
    /// </summary>
    public class TypeDescriptor
    {
        Dictionary<MemberInfo, MappingMemberDescriptor> _mappingMemberDescriptors;
        Dictionary<MemberInfo, DbColumnAccessExpression> _memberColumnMap;

        DefaultExpressionVisitor _visitor = null;

        public PropertyInfo PropertyInfo { get; private set; }
        public Type EntityType { get; private set; }
        public DbTable Table { get; private set; }
        public SZTableAttribute SZTableAttribute { get; set; }
        public MemberInfo MemberInfo { get; set; }
        public MemberInfo DbContextMemberInfo { get; set; }

        MappingMemberDescriptor _primaryKey = null;
        public MappingMemberDescriptor PrimaryKey { get { return this._primaryKey; } }

        public TypeDescriptor(Type t, PropertyInfo propertyInfo, MemberInfo memberInfo)
        {
            this.EntityType = t;
            PropertyInfo = propertyInfo;
            MemberInfo = memberInfo;
            DbContextMemberInfo = propertyInfo.PropertyType.GetMember("DbContext")[0];
            this.Init();
            InstanceCache.TryAdd(t, this);
        }
        public Dictionary<MemberInfo, MappingMemberDescriptor> MappingMemberDescriptors { get { return this._mappingMemberDescriptors; } }
        void Init()
        {
            this.InitTableInfo();
            this.InitMemberInfo();
            this.InitMemberColumnMap();
        }
        void InitTableInfo()
        {
            Type t = this.EntityType;
            var tableFlags = t.GetCustomAttributes(typeof(SZTableAttribute), false);

            if (tableFlags.Length > 0)
            {
                SZTableAttribute = (SZTableAttribute)tableFlags.First();
                if (string.IsNullOrEmpty(SZTableAttribute.TableName))
                {
                    SZTableAttribute.TableName = t.Name;
                }
                if (string.IsNullOrEmpty(SZTableAttribute.DisplayName))
                    SZTableAttribute.DisplayName = t.Name;
            }
            else
            {
                SZTableAttribute = new SZTableAttribute();
                SZTableAttribute.DisplayName = t.Name;
                SZTableAttribute.TableName = t.Name;
            }
            this.Table = new DbTable(SZTableAttribute.TableName);



        }
        void InitMemberInfo()
        {
            List<MappingMemberDescriptor> mappingMemberDescriptors = this.ExtractMappingMemberDescriptors();
            
            this._mappingMemberDescriptors = new Dictionary<MemberInfo, MappingMemberDescriptor>(mappingMemberDescriptors.Count);
            foreach (MappingMemberDescriptor mappingMemberDescriptor in mappingMemberDescriptors)
            {
                this._mappingMemberDescriptors.Add(mappingMemberDescriptor.MemberInfo, mappingMemberDescriptor);
                if (mappingMemberDescriptor.SZColumnAttribute.IsKey)
                {
                    _primaryKey = mappingMemberDescriptor;
                }
            }
        }
        void InitMemberColumnMap()
        {
            Dictionary<MemberInfo, DbColumnAccessExpression> memberColumnMap = new Dictionary<MemberInfo, DbColumnAccessExpression>(this._mappingMemberDescriptors.Count);
            foreach (var kv in this._mappingMemberDescriptors)
            {
                memberColumnMap.Add(kv.Key, new DbColumnAccessExpression(this.Table, kv.Value.Column));
            }

            this._memberColumnMap = memberColumnMap;
        }
        public MappingMemberDescriptor TryGetMappingMemberDescriptor(MemberInfo memberInfo)
        {
            memberInfo = memberInfo.AsReflectedMemberOf(this.EntityType);
            MappingMemberDescriptor memberDescriptor;
            this._mappingMemberDescriptors.TryGetValue(memberInfo, out memberDescriptor);
            return memberDescriptor;
        }
        public DbColumnAccessExpression TryGetColumnAccessExpression(MemberInfo memberInfo)
        {
            memberInfo = memberInfo.AsReflectedMemberOf(this.EntityType);
            DbColumnAccessExpression dbColumnAccessExpression;
            this._memberColumnMap.TryGetValue(memberInfo, out dbColumnAccessExpression);
            return dbColumnAccessExpression;
        }
        List<MappingMemberDescriptor> ExtractMappingMemberDescriptors()
        {
            var members = this.EntityType.GetMembers(BindingFlags.Public | BindingFlags.Instance);

            List<MappingMemberDescriptor> mappingMemberDescriptors = new List<MappingMemberDescriptor>();
            foreach (var member in members)
            {
                if (ShouldMap(member) == false)
                    continue;

                if (MappingTypeSystem.IsMappingType(member.GetMemberType()))
                {
                    MappingMemberDescriptor memberDescriptor = new MappingMemberDescriptor(member, this);
                    mappingMemberDescriptors.Add(memberDescriptor);
                }
            }

            return mappingMemberDescriptors;
        }
        public DefaultExpressionVisitor Visitor
        {
            get
            {
                if (this._visitor == null)
                    this._visitor = new DefaultExpressionVisitor(this);

                return this._visitor;
            }
        }
        static bool ShouldMap(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Property)
            {
                if (((PropertyInfo)member).GetSetMethod() == null)
                    return false;//对于没有公共的 setter 直接跳过
                return true;
            }
            else if (member.MemberType == MemberTypes.Field)
            {
                return true;
            }
            else
                return false;//只支持公共属性和字段
        }

        static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, TypeDescriptor> InstanceCache = new System.Collections.Concurrent.ConcurrentDictionary<Type, TypeDescriptor>();

        public static TypeDescriptor GetDescriptor(Type type)
        {
            TypeDescriptor instance;
            if (!InstanceCache.TryGetValue(type, out instance))
            {
                throw new Exception("没有缓存,错误.");
            }

            return instance;
        }
    }
}
