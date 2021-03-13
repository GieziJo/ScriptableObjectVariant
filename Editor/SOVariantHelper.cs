using NUnit.Framework;
using UnityEngine.Animations;

namespace Giezi.Tools
{
    public static class SOVariantHelper<T>
    {
        public static void SetParent(T parent, T child)
        {
            AssertSOVariant(parent);
            AssertSOVariant(child);
        
        }
    
        private static void AssertSOVariant(T obj) => Assert.IsTrue(obj.GetType().IsDefined(typeof(SOVariantAttribute), true));
    }
}