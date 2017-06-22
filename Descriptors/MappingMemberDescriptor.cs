using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using SZORM.Core.Emit;
using SZORM.DbExpressions;
using SZORM.InternalExtensions;

namespace SZORM.Descriptors
{
    public class MappingMemberDescriptor : MemberDescriptor
    {
        Func<object, object> _valueGetter;
        Action<object, object> _valueSetter;
        public MappingMemberDescriptor(MemberInfo memberInfo, TypeDescriptor declaringTypeDescriptor)
            : base(memberInfo, declaringTypeDescriptor)
        {
            this.Initialize();
        }
        void Initialize()
        {
            SZColumnAttribute columnFlag = (SZColumnAttribute)this.MemberInfo.GetCustomAttributes(typeof(SZColumnAttribute), true).FirstOrDefault();
            if (columnFlag == null)
                columnFlag = new SZColumnAttribute();
            if (string.IsNullOrEmpty( columnFlag.DisplayName))
                columnFlag.DisplayName = this.MemberInfo.Name;
            if (string.IsNullOrEmpty(columnFlag.FieldName))
                columnFlag.FieldName = this.MemberInfo.Name;
            if (columnFlag.IsKey)
            {
                columnFlag.Required = true;
            }
            SZColumnAttribute = columnFlag;
            this.Column = new DbColumn(SZColumnAttribute.FieldName, this.MemberInfoType, columnFlag.DbType, columnFlag.MaxLength);
        }

        public SZColumnAttribute SZColumnAttribute { get; set; }
        public DbColumn Column { get; private set; }

        public object GetValue(object instance)
        {
            if (null == this._valueGetter)
            {
                if (Monitor.TryEnter(this))
                {
                    try
                    {
                        if (null == this._valueGetter)
                            this._valueGetter = DelegateGenerator.CreateValueGetter(this.MemberInfo);
                    }
                    finally
                    {
                        Monitor.Exit(this);
                    }
                }
                else
                {
                    return this.MemberInfo.GetMemberValue(instance);
                }
            }

            return this._valueGetter(instance);
        }
        public void SetValue(object instance, object value)
        {
            if (null == this._valueSetter)
            {
                if (Monitor.TryEnter(this))
                {
                    try
                    {
                        if (null == this._valueSetter)
                            this._valueSetter = DelegateGenerator.CreateValueSetter(this.MemberInfo);
                    }
                    finally
                    {
                        Monitor.Exit(this);
                    }
                }
                else
                {
                    this.MemberInfo.SetMemberValue(instance, value);
                    return;
                }
            }

            this._valueSetter(instance, value);
        }
    }
}
