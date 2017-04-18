using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace SZORM
{
    internal class ReflectionCache
    {
        static AutoResetEvent MessageLock = new AutoResetEvent(true);
        private static Hashtable list { get; set; }
        public static List<EntityModel> DbContextGet(object _context)
        {
            if (list == null) list = new Hashtable();
            Type _contextType = _context.GetType();
            //必须是从我的基类继承过来的才可以
            if (_contextType.BaseType.FullName != "SZORM.DbContext") throw new Exception("必须继承SZORM.DbContext");
            //锁定
            MessageLock.WaitOne();
            //如果已经缓存直接返回
            if (list.ContainsKey(_contextType.FullName))
            {
                MessageLock.Set();
                return (List<EntityModel>)list[_contextType.FullName];
            }


            //创建表缓存
            List<EntityModel> _list = new List<EntityModel>();
            //获取他所有的属性
            List<PropertyInfo> pros = _contextType.GetProperties().ToList();

            for (int i = 0; i < pros.Count; i++)
            {
                //缓存对象
                EntityModel _table = new EntityModel();
                _table.ProName = pros[i].Name;
                _table.PropertyInfo = pros[i];
                //获取属性的类型
                object obj = Assembly.GetAssembly(pros[i].PropertyType).CreateInstance(pros[i].PropertyType.FullName);
                _table.PropertyFullName = pros[i].PropertyType.FullName;
                _table.PropertyType = pros[i].PropertyType;
                _table.Propertyobject = obj;
                _table.SetDbContextMethod = obj.GetType().GetMethod("SetDbContext", BindingFlags.Instance | BindingFlags.NonPublic);
                _table.SetTableMethod = obj.GetType().GetMethod("SetTableCache", BindingFlags.Instance | BindingFlags.NonPublic);
                //获取entity对象
                _table.EntityType = pros[i].PropertyType.GetGenericArguments()[0];
                _table.EntityName = _table.EntityType.Name;

                object[] AttsTable = _table.EntityType.GetCustomAttributes(typeof(SZTableAttribute), false);
                //只有第一个有效
                for (int m = 0; m < AttsTable.Length && m < 1; m++)
                {
                    _table.Att = (SZTableAttribute)AttsTable[m];
                }
                if (_table.Att == null)
                {
                    _table.Att = new SZTableAttribute();

                }
                if (string.IsNullOrEmpty(_table.Att.DisplayName)) _table.Att.DisplayName = _table.EntityName;
                if (string.IsNullOrEmpty(_table.Att.TableName)) _table.Att.TableName = _table.EntityName;
                //获取具体字段
                PropertyInfo[] fieldPros = _table.EntityType.GetProperties();

                for (int j = 0; j < fieldPros.Length; j++)
                {
                    EntityPropertyModel _field = new EntityPropertyModel();
                    _field.Property = fieldPros[j];
                    _field.Name = fieldPros[j].Name;

                    _field.FieldType = fieldPros[j].PropertyType.FullName;

                    //获取特性
                    object[] Atts = fieldPros[j].GetCustomAttributes(typeof(SZColumnAttribute), true);
                    for (int m = 0; m < Atts.Length && m < 1; m++)
                    {
                        _field.Att = (SZColumnAttribute)Atts[m];
                    }
                    if (_field.Att == null)
                    {
                        _field.Att = new SZColumnAttribute();

                    }
                    if (string.IsNullOrEmpty(_field.Att.DisplayName)) _field.Att.DisplayName = _field.Name;
                    if (string.IsNullOrEmpty(_field.Att.ColumnName)) _field.Att.ColumnName = _field.Name;
                    if (_field.Att.IsKey)
                    {
                        _field.Att.MaxLength = 32;
                        _field.Att.Required = true;
                    }
                    if (_field.FieldType.StartsWith("System.Nullable"))
                    {
                        _field.FieldType = fieldPros[j].PropertyType.GetGenericArguments()[0].FullName;
                        _field.IsGenericArguments = true;
                        _field.IsEnmu = fieldPros[j].PropertyType.GetGenericArguments()[0].IsEnum;
                        if (_field.IsEnmu)
                        {
                            _field.Att.MaxLength = fieldPros[j].PropertyType.GetGenericArguments()[0].GetEnumValues().Length.ToString().Length;
                        }
                    }
                    _table.Fields.Add(_field);
                    //f.FieldType=fieldPros[j].
                }
                _list.Add(_table);
            }
            list.Add(_contextType.FullName, _list);
            MessageLock.Set();
            return _list;

        }
    }
    public class EntityModel
    {
        public EntityModel()
        {
            Fields = new List<EntityPropertyModel>();
            Att = new SZTableAttribute();
        }
        /// <summary>
        /// 属性名称
        /// </summary>
        public string ProName { get; set; }
        /// <summary>
        /// 属性信息
        /// </summary>
        public PropertyInfo PropertyInfo { get; set; }
        /// <summary>
        /// SetDbContext 方法对象
        /// </summary>
        public MethodInfo SetDbContextMethod { get; set; }
        /// <summary>
        /// SetDbContext 方法对象
        /// </summary>
        public MethodInfo SetTableMethod { get; set; }
        /// <summary>
        /// 属性对象
        /// </summary>
        public object Propertyobject { get; set; }
        public Type PropertyType { get; set; }
        public string PropertyFullName { get; set; }
        /// <summary>
        /// 实体类类型
        /// </summary>
        public Type EntityType { get; set; }
        /// <summary>
        /// 实体类名,用于表名
        /// </summary>
        public string EntityName { get; set; }
        /// <summary>
        /// 字段列表
        /// </summary>
        public List<EntityPropertyModel> Fields { get; set; }

        public SZTableAttribute Att { get; set; }
    }
    public class EntityPropertyModel
    {
        public EntityPropertyModel()
        {
            Att = new SZColumnAttribute();
        }
        /// <summary>
        /// 属性原始对象
        /// </summary>
        public PropertyInfo Property { get; set; }
        /// <summary>
        /// 字段名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 字段原始类型 
        /// </summary>
        public string FieldType { get; set; }
        private bool isGenericArguments = false;
        /// <summary>
        /// 是否反类型
        /// </summary>
        public bool IsGenericArguments
        {
            get { return isGenericArguments; }
            set { isGenericArguments = value; }
        }
        private bool isEnmu = false;
        /// <summary>
        /// 是否枚举类型
        /// </summary>
        public bool IsEnmu
        {
            get { return isEnmu; }
            set { isEnmu = value; }
        }
        public SZColumnAttribute Att { get; set; }
    }
}
