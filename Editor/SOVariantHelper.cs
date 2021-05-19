// ===============================
// AUTHOR          : J. Giezendanner
// CREATE DATE     : 13.03.2020
// MODIFIED DATE   : 
// PURPOSE         : Helper for scriptable object variants
// SPECIAL NOTES   : 
// ===============================
// Change History:
//==================================


using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Giezi.Tools
{
    public static class SOVariantHelper<T> where T : ScriptableObject
    {
        public static bool SetParent(T child, T parent)
        {
            AssertIsSOVariant(parent);
            AssertIsSOVariant(child);
            
            SOVariant<T> soVariant = new SOVariant<T>(child);
            return soVariant.SetParent(parent);
        }

        public static void ChangeFieldOverrideState(T target, string name, bool isOverridden)
        {
            AssertIsSOVariant(target);
            
            SOVariant<T> soVariant = new SOVariant<T>(target);
            soVariant.NotifyOverrideChangeInState(name, isOverridden);
        }

        public static void ChangeFieldValue(T target, string name, object value)
        {
            AssertIsSOVariant(target);
            
            SOVariant<T> soVariant = new SOVariant<T>(target);
            soVariant.ChangeValue(name, value);   
        }

        public static void SetFieldOverrideAndSetValue(T target, string name, object value)
        {
            AssertIsSOVariant(target);
            
            SOVariant<T> soVariant = new SOVariant<T>(target);
            soVariant.NotifyOverrideChangeInState(name, true);
            soVariant.ChangeValue(name, value);   
        }

        public static void SetParentOverrideValue(T child, T parent, string name, object value)
        {
            AssertIsSOVariant(parent);
            AssertIsSOVariant(child);
            
            SOVariant<T> soVariant = new SOVariant<T>(child);
            soVariant.SetParent(parent);
            
            soVariant.NotifyOverrideChangeInState(name, true);
            soVariant.ChangeValue(name, value);
        }

        public static void SetParentOverrideValues(T child, T parent, Dictionary<string, object> values)
        {
            AssertIsSOVariant(parent);
            AssertIsSOVariant(child);
            
            SOVariant<T> soVariant = new SOVariant<T>(child);
            soVariant.SetParent(parent);

            foreach (KeyValuePair<string, object> value in values)
            {
                soVariant.NotifyOverrideChangeInState(value.Key, true);
                soVariant.ChangeValue(value.Key, value.Value);
            }
        }
    
        private static void AssertIsSOVariant(T obj) => Assert.IsTrue(obj.GetType().IsDefined(typeof(SOVariantAttribute), true));
    }
}