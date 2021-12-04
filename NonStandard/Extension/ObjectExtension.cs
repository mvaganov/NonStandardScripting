//https://stackoverflow.com/questions/129389/how-do-you-do-a-deep-copy-of-an-object-in-net

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Collections;
using NonStandard.Extension.ArrayExtensions;

namespace NonStandard.Extension
{
    public static class ObjectExtension {
        // https://answers.unity.com/questions/206665/typegettypestring-does-not-work-in-unity.html
        public static Type GetType( string TypeName, bool searchAllAssemblies = false ) {
            // Try Type.GetType() first. This will work with types defined
            // by the Mono runtime, in the same assembly as the caller, etc.
            var type = Type.GetType( TypeName );
            // If it worked, then we're done here
            if( type != null ) return type;
            // If the TypeName is a full name, then we can try loading the defining assembly directly
            if( TypeName.Contains( "." ) ) {
                // Get the name of the assembly (Assumption is that we are using 
                // fully-qualified type names)
                var assemblyName = TypeName.Substring( 0, TypeName.IndexOf( '.' ) );
                 // Attempt to load the indicated Assembly
                var assembly = Assembly.Load( assemblyName );
                if( assembly == null ) return null;
                // Ask that assembly to return the proper Type
                type = assembly.GetType( TypeName );
                if( type != null ) return type;
            }
            if (!searchAllAssemblies) return null;
            // If we still haven't found the proper type, we can enumerate all of the 
            // loaded assemblies and see if any of them define the type
            var currentAssembly = Assembly.GetExecutingAssembly();
            var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
            foreach( var assemblyName in referencedAssemblies ) {
                // Load the referenced assembly
                var assembly = Assembly.Load( assemblyName );
                if( assembly != null ) {
                    // See if that assembly defines the named type
                    type = assembly.GetType( TypeName );
                    if( type != null ) return type;
                }
            }
            // The type just couldn't be found...
            return null;
        }
        
        private static readonly MethodInfo CloneMethod = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPrimitive(this Type type)
        {
            if (type == typeof(String)) return true;
            return (type.IsValueType & type.IsPrimitive);
        }

        public static Object Copy(this Object originalObject)
        {
            return InternalCopy(originalObject, new Dictionary<Object, Object>(new ReferenceEqualityComparer()));
        }
        private static Object InternalCopy(Object originalObject, IDictionary<Object, Object> visited)
        {
            if (originalObject == null) return null;
            var typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect)) return originalObject;
            if (visited.ContainsKey(originalObject)) return visited[originalObject];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            var cloneObject = CloneMethod.Invoke(originalObject, null);
            if (typeToReflect.IsArray)
            {
                var arrayType = typeToReflect.GetElementType();
                if (IsPrimitive(arrayType) == false)
                {
                    Array clonedArray = (Array)cloneObject;
                    clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                }

            }
            visited.Add(originalObject, cloneObject);
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
            return cloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect)
        {
            if (typeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
                CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
            }
        }

        private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null)
        {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                var clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }
        public static T Copy<T>(this T original)
        {
            return (T)Copy((Object)original);
        }
    }

    public class ReferenceEqualityComparer : EqualityComparer<Object>
    {
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }
        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }

    namespace ArrayExtensions
    {
        public static class ArrayExtensions
        {
            public static void ForEach(this Array array, Action<Array, int[]> action)
            {
                if (array.LongLength == 0) return;
                ArrayTraverse walker = new ArrayTraverse(array);
                do action(array, walker.Position);
                while (walker.Step());
            }

            public static bool ValueEquals(this IList a, IList b) {
                if (a == null || b == null) return a == null && b == null;
                if (a.Count != b.Count) return false;
                for (int i = 0; i < a.Count; ++i) { if (!a[i].Equals(b[i])) { return false; } }
                return true;
            }

            public static int GetAllHashCodes(this IList a) {
                int hCode = a.GetHashCode();
                for (int i = 0; i < a.Count; ++i) { hCode += a[i].GetHashCode(); }
                return hCode;
            }
        }

        internal class ArrayTraverse
        {
            public int[] Position;
            private int[] maxLengths;

            public ArrayTraverse(Array array)
            {
                maxLengths = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i)
                {
                    maxLengths[i] = array.GetLength(i) - 1;
                }
                Position = new int[array.Rank];
            }

            public bool Step()
            {
                for (int i = 0; i < Position.Length; ++i)
                {
                    if (Position[i] < maxLengths[i])
                    {
                        Position[i]++;
                        for (int j = 0; j < i; j++)
                        {
                            Position[j] = 0;
                        }
                        return true;
                    }
                }
                return false;
            }
        }
    }

}