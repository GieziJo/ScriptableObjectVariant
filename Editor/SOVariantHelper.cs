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
using UnityEngine;
using UnityEngine.Assertions;

namespace Giezi.Tools
{
    public static class SOVariantHelper<T> where T : ScriptableObject
    {
        public static bool SetParent(T child, T parent, bool setToParentValue=true)
        {
            AssertIsSOVariant(parent);
            AssertIsSOVariant(child);
            
            SOVariant<T> soVariant = new SOVariant<T>(child);
            return soVariant.SetParent(parent, setToParentValue);
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

        public static void SetParentOverrideValue(T child, T parent, string name, object value, bool setToParentValue=true)
        {
            AssertIsSOVariant(parent);
            AssertIsSOVariant(child);
            
            SOVariant<T> soVariant = new SOVariant<T>(child);
            soVariant.SetParent(parent, setToParentValue);
            
            soVariant.NotifyOverrideChangeInState(name, true);
            soVariant.ChangeValue(name, value);
        }

        public static void SetParentOverrideValues(T child, T parent, Dictionary<string, object> values, bool setToParentValue=true)
        {
            AssertIsSOVariant(parent);
            AssertIsSOVariant(child);
            
            SOVariant<T> soVariant = new SOVariant<T>(child);
            soVariant.SetParent(parent, setToParentValue);

            foreach (KeyValuePair<string, object> value in values)
            {
                soVariant.NotifyOverrideChangeInState(value.Key, true);
                soVariant.ChangeValue(value.Key, value.Value);
            }
        }
    
        internal static void AssertIsSOVariant(T obj) => Assert.IsTrue(obj.GetType().IsDefined(typeof(SOVariantAttribute), true));
    }
}