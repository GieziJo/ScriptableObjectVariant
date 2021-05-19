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
using Sirenix.OdinInspector;
using UnityEngine;

namespace Giezi.Tools
{
    internal static class FieldInfoHelper
    {
        private static BindingFlags bindFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                                BindingFlags.Static | BindingFlags.Instance;

        internal static FieldInfo GetFieldRecursively(Type type, string attributeName)
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

        internal static IEnumerable<FieldInfo> GetAllFields(Type t)
        {
            if (t == null || t == typeof(ScriptableObject) || t == typeof(SerializedScriptableObject))
                return Enumerable.Empty<FieldInfo>();
            return t.GetFields(bindFlags).Concat(GetAllFields(t.BaseType));
        }
    }
}