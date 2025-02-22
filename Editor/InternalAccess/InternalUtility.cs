using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;

namespace elZach.Access{

    public class InternalUtility
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern object GetStructValueInternal(
            string assemblyName,
            string nameSpace,
            string className);
    }
    
    public static class InternalUtilityExtensions
    {
        public static object GetInternalStructValue(this SerializedProperty property)
#if UNITY_2022_1_OR_NEWER && false //for now this spams serialization callbacks...
            => property.structValue;
#else
            => property.GetTargetObjectOfProperty();
#endif
        
        //https://forum.unity.com/threads/get-a-general-object-value-from-serializedproperty.327098/#post-7569286
        private static object GetTargetObjectOfProperty(this SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            return GetTargetObjectFromPath(obj, path);
        }
        private static object GetTargetObjectFromPath(object obj, string path)
        {
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                }
            }
            return obj;
        }
 
        private static object GetValue_Imp(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();
 
            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(source);
 
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                    return p.GetValue(source, null);
 
                type = type.BaseType;
            }
            return null;
        }
 
        private static object GetValue_Imp(object source, string name, int index)
        {
            var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;
 
            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }
        
        public static List<object> GetObjectHierarchy(this SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var hierarchy = new List<object>() {obj};
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index);
                    hierarchy.Add(obj);
                }
                else
                {
                    obj = GetValue_Imp(obj, element);
                    hierarchy.Add(obj);
                }
            }
            return hierarchy;
        }

        public static object GetParentObject(this SerializedProperty prop)
        {
            if (prop.depth == 0) return prop.serializedObject.targetObject;
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            if (path.Contains(".")) path = path.Substring(0, path.LastIndexOf("."));
            else return prop.serializedObject.targetObject;
            return GetTargetObjectFromPath(prop.serializedObject.targetObject, path);
        }
        
        public static SerializedProperty FindPropertyByAutoPropertyName(this SerializedProperty obj, string propName)
        {
            return obj.FindPropertyRelative($"<{propName}>k__BackingField");
        }
    }
}
