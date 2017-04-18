using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace SZORM
{
    /// <summary>
    /// 
    /// </summary>
    public static class ExtensionDataToList
    {
        //缓存高效
        public static List<TResult> ToList<TResult>(this DataTable dt,EntityModel _table) where TResult : class, new()
        {
            List<EntityPropertyModel> prlist = new List<EntityPropertyModel>();
            Type t = typeof(TResult);
            _table.Fields.FindAll(f => dt.Columns.IndexOf(f.Name) != -1).ForEach(f => prlist.Add(f));
            List<TResult> result = new List<TResult>();

            foreach (DataRow row in dt.Rows)
            {
                result.Add(row.TableToEntity<TResult>(prlist));
            }
            return result;
        }
        private static TResult TableToEntity<TResult>(this DataRow row, List<EntityPropertyModel> prlist) where TResult : class, new()
        {
            TResult result = new TResult();
            foreach (var _field in prlist)
            {
                if (row[_field.Name] != DBNull.Value)
                {
                    if (_field.IsEnmu)
                    {
                        foreach (var value in _field.Property.PropertyType.GetGenericArguments()[0].GetEnumValues())
                        {
                            if (((int)value).ToString() == row[_field.Name].ToString())
                            {
                                _field.Property.SetValue(result, value, null);
                            }
                        }
                        var tttt = _field.Property.PropertyType.GetGenericArguments()[0].GetProperties();// (result, (Enum)int.Parse(row[_field.Name].ToString()), null);
                        //_field.Property.SetValue(result, Enum.Parse(_field.Property.PropertyType.GetGenericArguments()[0], (int)(row[_field.Name].ToString())), null);
                    }
                    else if (_field.FieldType == "System.Boolean")
                    {
                        object obj = row[_field.Name];
                        if (obj.ToString() == "1")
                            _field.Property.SetValue(result, true, null);
                        else
                            _field.Property.SetValue(result, false, null);
                    }
                    else
                    {
                        object obj = row[_field.Name];
                        _field.Property.SetValue(result, obj, null);
                    }
                }
            }
            return result;
        }
        private static TResult DataReaderToEntity<TResult>(this IDataReader row, List<EntityPropertyModel> prlist) where TResult : class, new()
        {
            TResult result = new TResult();
            foreach (var _field in prlist)
            {
                if (row[_field.Name] != DBNull.Value)
                {
                    if (_field.IsEnmu)
                    {
                        foreach (var value in _field.Property.PropertyType.GetGenericArguments()[0].GetEnumValues())
                        {
                            if (((int)value).ToString() == row[_field.Name].ToString())
                            {
                                _field.Property.SetValue(result, value, null);
                            }
                        }
                        var tttt = _field.Property.PropertyType.GetGenericArguments()[0].GetProperties();// (result, (Enum)int.Parse(row[_field.Name].ToString()), null);
                        //_field.Property.SetValue(result, Enum.Parse(_field.Property.PropertyType.GetGenericArguments()[0], (int)(row[_field.Name].ToString())), null);
                    }
                    else if (_field.FieldType == "System.Boolean")
                    {
                        object obj = row[_field.Name];
                        if (obj.ToString() == "1")
                            _field.Property.SetValue(result, true, null);
                        else
                            _field.Property.SetValue(result, false, null);
                    }
                    else
                    {
                        object obj = row[_field.Name];
                        _field.Property.SetValue(result, obj, null);
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 用于递归,根据递归条件进行上级递归,下级递归,
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="list">需要递归的数据</param>
        /// <param name="top">需要从那个节点进行递归</param>
        /// <param name="fun">参数1:为当前判断的节点值top,如果top中,参数2:为当前循环的值list</param>
        /// <returns></returns>
        public static List<TResult> GetSubset<TResult>(this List<TResult> list, TResult top, Func<TResult, TResult, bool> fun)
        {
            List<TResult> retsult = new List<TResult>();
            
            foreach (var tr in list)
            {
                if (fun(top,tr))
                {
                    retsult.Add(tr);
                    retsult.AddRange(GetSubset(list, tr, fun));
                }
            }
            return retsult;
        }

        //public static List<TResult> ToList<TResult>(this IDataReader dr, bool isClose = true)
        //{
        //    IDataReaderEntityBuilder<TResult> eblist = IDataReaderEntityBuilder<TResult>.CreateBuilder(dr);
        //    List<TResult> list = new List<TResult>();
        //    if (dr == null) return list;
        //    while (dr.Read()) list.Add(eblist.Build(dr));
        //    if (isClose) { dr.Close(); dr.Dispose(); dr = null; }
        //    return list;
        //}
        /// <summary>
        /// 追个读取数据
        /// </summary>
        /// <typeparam name="TResult">返回读取数据类型</typeparam>
        /// <param name="dr">IDataReader数据</param>
        /// <param name="_table">结构信息</param>
        /// <param name="action">用于返回数据</param>
        /// <param name="Next">吓一条方法</param>
        public static void ToListReader<TResult>(this IDataReader dr, EntityModel _table, Action<TResult> action, out Action Next, out Action Close) where TResult : class, new()
        {

            List<string> drColumns = new List<string>();
            int len = dr.FieldCount;
            for (int j = 0; j < len; j++) drColumns.Add(dr.GetName(j).Trim());

            List<EntityPropertyModel> prlist = new List<EntityPropertyModel>();
            _table.Fields.FindAll(f => drColumns.IndexOf(f.Name) != -1).ForEach(f => prlist.Add(f));
            Next = new Action(() =>
            {
                dr.Read();
                if (action != null)
                {
                    action(dr.DataReaderToEntity<TResult>(prlist));
                }
            });
            Close = new Action(() => {
                dr.Close();
            });
        }
    }
    public class IDataReaderEntityBuilder<Entity>
    {
        private static readonly MethodInfo getValueMethod =
        typeof(IDataRecord).GetMethod("get_Item", new Type[] { typeof(int) });
        private static readonly MethodInfo isDBNullMethod =
            typeof(IDataRecord).GetMethod("IsDBNull", new Type[] { typeof(int) });
        private delegate Entity Load(IDataRecord dataRecord);

        private Load handler;
        private IDataReaderEntityBuilder() { }
        public Entity Build(IDataRecord dataRecord)
        {
            return handler(dataRecord);
        }
        public static IDataReaderEntityBuilder<Entity> CreateBuilder(IDataRecord dataRecord)
        {
            PropertyInfo[] propertyInfos = typeof(Entity).GetProperties();
            IDataReaderEntityBuilder<Entity> dynamicBuilder = new IDataReaderEntityBuilder<Entity>();
            DynamicMethod method = new DynamicMethod("DynamicCreateEntity", typeof(Entity),
                    new Type[] { typeof(IDataRecord) }, typeof(Entity), true);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder result = generator.DeclareLocal(typeof(Entity));
            generator.Emit(OpCodes.Newobj, typeof(Entity).GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Stloc, result);
            for (int i = 0; i < dataRecord.FieldCount; i++)
            {
                PropertyInfo propertyInfo = propertyInfos.FirstOrDefault(f => f.Name.ToUpper() == dataRecord.GetName(i));
                Label endIfLabel = generator.DefineLabel();
                if (propertyInfo != null && propertyInfo.GetSetMethod() != null)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Callvirt, isDBNullMethod);
                    generator.Emit(OpCodes.Brtrue, endIfLabel);
                    generator.Emit(OpCodes.Ldloc, result);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Callvirt, getValueMethod);
                    generator.Emit(OpCodes.Unbox_Any, dataRecord.GetFieldType(i));
                    generator.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod());
                    generator.MarkLabel(endIfLabel);
                }
            }
            generator.Emit(OpCodes.Ldloc, result);
            generator.Emit(OpCodes.Ret);
            dynamicBuilder.handler = (Load)method.CreateDelegate(typeof(Load));
            return dynamicBuilder;
        }
    }

    public class DataTableEntityBuilder<Entity>
    {
        private static readonly MethodInfo getValueMethod = typeof(DataRow).GetMethod("get_Item", new Type[] { typeof(int) });
        private static readonly MethodInfo isDBNullMethod = typeof(DataRow).GetMethod("IsNull", new Type[] { typeof(int) });
        private delegate Entity Load(DataRow dataRecord);

        private Load handler;
        private DataTableEntityBuilder() { }

        public Entity Build(DataRow dataRecord)
        {
            return handler(dataRecord);
        }
        public static DataTableEntityBuilder<Entity> CreateBuilder(DataRow dataRecord)
        {
            PropertyInfo[] propertyInfos = typeof(Entity).GetProperties();
            DataTableEntityBuilder<Entity> dynamicBuilder = new DataTableEntityBuilder<Entity>();
            DynamicMethod method = new DynamicMethod("DynamicCreateEntity", typeof(Entity), new Type[] { typeof(DataRow) }, typeof(Entity), true);
            ILGenerator generator = method.GetILGenerator();
            LocalBuilder result = generator.DeclareLocal(typeof(Entity));
            generator.Emit(OpCodes.Newobj, typeof(Entity).GetConstructor(Type.EmptyTypes));
            generator.Emit(OpCodes.Stloc, result);

            for (int i = 0; i < dataRecord.ItemArray.Length; i++)
            {
                //PropertyInfo propertyInfo = typeof(Entity).GetProperty(dataRecord.Table.Columns[i].ColumnName);
                PropertyInfo propertyInfo = propertyInfos.FirstOrDefault(f => f.Name.ToUpper() == dataRecord.Table.Columns[i].ColumnName); 
                Label endIfLabel = generator.DefineLabel();
                if (propertyInfo != null && propertyInfo.GetSetMethod() != null)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Callvirt, isDBNullMethod);
                    generator.Emit(OpCodes.Brtrue, endIfLabel);
                    generator.Emit(OpCodes.Ldloc, result);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Callvirt, getValueMethod);
                    generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
                    generator.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod());
                    generator.MarkLabel(endIfLabel);
                }
            }
            generator.Emit(OpCodes.Ldloc, result);
            generator.Emit(OpCodes.Ret);
            dynamicBuilder.handler = (Load)method.CreateDelegate(typeof(Load));
            return dynamicBuilder;
        }
    }


}
