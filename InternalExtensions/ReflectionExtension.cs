﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SZORM.InternalExtensions
{
    public static class ReflectionExtension
    {
        public static Type GetMemberType(this MemberInfo propertyOrField)
        {
            if (propertyOrField.MemberType == MemberTypes.Property)
                return ((PropertyInfo)propertyOrField).PropertyType;
            if (propertyOrField.MemberType == MemberTypes.Field)
                return ((FieldInfo)propertyOrField).FieldType;

            throw new ArgumentException();
        }

        public static void SetMemberValue(this MemberInfo propertyOrField, object obj, object value)
        {
            if (propertyOrField.MemberType == MemberTypes.Property)
                ((PropertyInfo)propertyOrField).SetValue(obj, value, null);
            else if (propertyOrField.MemberType == MemberTypes.Field)
                ((FieldInfo)propertyOrField).SetValue(obj, value);

            throw new ArgumentException();
        }
        public static object GetMemberValue(this MemberInfo propertyOrField, object obj)
        {
            if (propertyOrField.MemberType == MemberTypes.Property)
                return ((PropertyInfo)propertyOrField).GetValue(obj, null);
            else if (propertyOrField.MemberType == MemberTypes.Field)
                return ((FieldInfo)propertyOrField).GetValue(obj);

            throw new ArgumentException();
        }

        public static MemberInfo AsReflectedMemberOf(this MemberInfo propertyOrField, Type type)
        {
            if (propertyOrField.ReflectedType != type)
            {
                MemberInfo tempMember = null;
                if (propertyOrField.MemberType == MemberTypes.Property)
                {
                    tempMember = type.GetProperty(propertyOrField.Name);
                }
                else if (propertyOrField.MemberType == MemberTypes.Field)
                {
                    tempMember = type.GetField(propertyOrField.Name);
                }

                if (tempMember != null)
                    propertyOrField = tempMember;
            }

            return propertyOrField;
        }
        public static bool IsNullable(this Type type)
        {
            Type underlyingType;
            return IsNullable(type, out underlyingType);
        }
        public static bool IsNullable(this Type type, out Type underlyingType)
        {
            underlyingType = Nullable.GetUnderlyingType(type);
            return underlyingType != null;
        }
        public static Type GetUnderlyingType(this Type type)
        {
            Type underlyingType;
            if (!IsNullable(type, out underlyingType))
                underlyingType = type;

            return underlyingType;
        }
        public static bool IsAnonymousType(this Type type)
        {
            string typeName = type.Name;
            return typeName.Contains("<>") && typeName.Contains("__") && typeName.Contains("AnonymousType");
        }
        public static bool IsClass(this Type type)
        {
            return type.IsClass;
        }
        public static bool IsInterface(this Type type)
        {
            return type.IsInterface;
        }
        public static bool IsEnum(this Type type)
        {
            return type.IsEnum;
        }
        public static bool IsValueType(this Type type)
        {
            return type.IsValueType;
        }
        public static bool IsGenericType(this Type type)
        {
            return type.IsGenericType;
        }
    }
}
