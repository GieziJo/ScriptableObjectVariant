// ===============================
// AUTHOR          : J. Giezendanner
// CREATE DATE     : 20.01.2020
// MODIFIED DATE   : 
// PURPOSE         : Helper to get field infos recursively
// SPECIAL NOTES   : 
// ===============================
// Change History:
//==================================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Giezi.Tools
{
    public static class FieldInfoHelper
    {
        private static BindingFlags bindFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                                BindingFlags.Static | BindingFlags.Instance;

        public static FieldInfo GetFieldRecursively(Type type, string attributeName)
        {
            if (type == null)
                return null;
            FieldInfo fieldInfo = null;
            fieldInfo = type.GetField(attributeName, bindFlags);
            if (fieldInfo is null)
                return GetFieldRecursively(type.BaseType, attributeName);
            else
                return fieldInfo;
        }

        public static IEnumerable<FieldInfo> GetAllFields(Type t)
        {
            if (t == null || t == typeof(ScriptableObject))
                return Enumerable.Empty<FieldInfo>();
            return t.GetFields(bindFlags).Concat(GetAllFields(t.BaseType));
        }
    }
}