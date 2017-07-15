using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SZORM;
using SZORM.Descriptors;
using SZORM.Factory;
using SZORM.Factory.Models;
using SZORM.Query;
namespace SZORM
{
    public partial class DbContext
    {
        /// <summary>
        /// 每次只进行一次检查
        /// </summary>
        static Dictionary<string, bool> isok = new Dictionary<string, bool>();
        private bool isUPDataBase = true;
        public void InitDb()
        {
            
            //开始检查数据结构
            lock (isok)
            {
                if (!isok.ContainsKey(_dbConfig.ConnectionStr))
                {
                    //开始验证数据库结构
                    if (isUPDataBase)
                    {
                       
                        CreateTable();
                        
                    }
                    isok.Add(_dbConfig.ConnectionStr, true);
                }
            }
        }
        private void CreateTable()
        {
            //初始化所有的属性
            InternalAdoSession.BeginTransaction();
            //初始化库
            int i = 0;
            _dbStructCheck = _dbContextServiceProvider.CreateStructureCheck();
            List<TableModel> TableModels = _dbStructCheck.TableList(this);
            foreach (var typeDescriptor in _typeDescriptors)
            {
                //如果是视图就不检查
                var __typeDescriptor = typeDescriptor.Value;
                    if (__typeDescriptor.SZTableAttribute.IsView) continue;
                bool __tablehas = false;
                foreach (var item in TableModels)
                {

                    if (__typeDescriptor.Table.Name.ToUpper() == item.Name.ToUpper())
                    {
                        //如果存在
                        __tablehas = true;
                        //就去检查所有的字段是否一致,
                        List<ColumnModel> ColumnModels = _dbStructCheck.ColumnList(this, item.Name.ToUpper());
                        foreach (var item1 in __typeDescriptor.MappingMemberDescriptors)
                        {
                            bool __fieldhas = false;
                            var itemMember = item1.Value;
                            //itemMember.
                            foreach (var column in ColumnModels)
                            {
                                if (column.Name.ToUpper() == itemMember.Column.Name.ToUpper())
                                {
                                    //开始比对,是否一样
                                    if (itemMember.SZColumnAttribute.Required != column.Required
                                        || GetColumn(itemMember).ColumnFullType != column.ColumnFullType)
                                    {
                                        _dbStructCheck.ColumnEdit(this, __typeDescriptor.Table.Name, GetColumn(itemMember));
                                    }
                                    __fieldhas = true;
                                    break;
                                }
                            }
                            if (!__fieldhas)
                            {
                                //如果不存在字段则添加
                                _dbStructCheck.ColumnAdd(this, __typeDescriptor.Table.Name, GetColumn(itemMember));
                            }
                        } 
                    }
                }
                if (!__tablehas)
                {
                    //如果不存在
                    TableModel table = new TableModel();
                    table.Name = __typeDescriptor.Table.Name;
                    foreach (var item in __typeDescriptor.MappingMemberDescriptors)
                    {
                        table.Columns.Add(GetColumn(item.Value));
                    }

                    _dbStructCheck.CreateTable(this, table);
                    i++;

                }
            }
            InternalAdoSession.CommitTransaction();
            //开始更新数据库
            var uplist = SZORM_Upgrades.AsQuery().OrderByDesc(o => o.Version).Take(1).ToList();
            if (uplist.Any())
            {
                dbVersion = uplist[0].Version;
            }
            else
            {
                SZORM_Upgrade up = new SZORM_Upgrade();
                up.UPTime = DateTime.Now;
                up.Version = 0;
                up.UPContent = "首次创建数据库.";
                UPDBVersion(up);
                _internalAdoSession.BeginTransaction();
                Initialization();
                _internalAdoSession.CommitTransaction();
            }
            _internalAdoSession.BeginTransaction();
            UpdataDBExce();
            _internalAdoSession.CommitTransaction();
            
        }
        protected abstract void Initialization();
        private int? dbVersion = -1;
        protected decimal? DBVersion { get { return dbVersion; } }
        /// <summary>
        /// 重写次方法.使用DBVersion查看数据库版本,第一次为1,初始化的时候运行一次
        /// </summary>
        protected abstract void UpdataDBExce();
        /// <summary>
        /// 更新数据库版本
        /// </summary>
        /// <param name="up"></param>
        protected void UPDBVersion(SZORM_Upgrade up)
        {
            if (up.Version <= dbVersion) throw new Exception("更新后的版本必须大于现有版本");

            up.UPTime = DateTime.Now;
            SZORM_Upgrades.Add(up);

            dbVersion = up.Version;
        }
        private ColumnModel GetColumn(MappingMemberDescriptor memberDescriptor)
        {
            ColumnModel model = new ColumnModel();
            model.Name = memberDescriptor.Column.Name;
            model.Required = memberDescriptor.SZColumnAttribute.Required;
            model.IsKey = memberDescriptor.SZColumnAttribute.IsKey;
            //if(memberDescriptor.MemberInfoType)fieldPros[j].PropertyType.GetGenericArguments()[0].FullName
            if (memberDescriptor.MemberInfoType.Name == "Nullable`1")
            {
                var tmpType = memberDescriptor.MemberInfoType.GetGenericArguments()[0];
                model.type = tmpType;
            }
            else
            {
                model.type = memberDescriptor.MemberInfoType;
            }

            model.IsText = memberDescriptor.SZColumnAttribute.MaxLength == 0;
            model.MaxLength = memberDescriptor.SZColumnAttribute.MaxLength;
            model.NumberPrecision = memberDescriptor.SZColumnAttribute.NumberPrecision;
            model.NumberSize = memberDescriptor.SZColumnAttribute.NumberSize;
            model.ColumnFullType = _dbStructCheck.FieldType(model);
            return model;
        }
    }
}
