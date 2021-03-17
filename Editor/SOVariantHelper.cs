using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace Giezi.Tools
{
    public static class SOVariantHelper<T> where T : ScriptableObject
    {
        public static void SetParent(T child, T parent)
        {
            AssertIsSOVariant(parent);
            AssertIsSOVariant(child);
            
            SOVariant<T> soVariant = new SOVariant<T>(child);
            soVariant.SetParent(parent);
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
    
        private static void AssertIsSOVariant(T obj) => Assert.IsTrue(obj.GetType().IsDefined(typeof(SOVariantAttribute), true));
    }
}