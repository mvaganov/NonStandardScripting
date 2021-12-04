using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NonStandard.Data.Parse;

namespace NonStandard.Extension {
	public static class ReflectionExtension {
		public static Type GetICollectionType(this Type type) {
			foreach (Type i in type.GetInterfaces()) {
				if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>)) {
					return i.GetGenericArguments()[0];
				}
			}
			return null;
		}
		public static Type GetIListType(this Type type) {
			foreach (Type i in type.GetInterfaces()) {
				if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)) {
					return i.GetGenericArguments()[0];
				}
			}
			return null;
		}
		public static KeyValuePair<Type, Type> GetIDictionaryType(this Type type) {
			foreach (Type i in type.GetInterfaces()) {
				if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
					return new KeyValuePair<Type, Type>(i.GetGenericArguments()[0], i.GetGenericArguments()[1]);
				}
			}
			if (type.BaseType != null) { return GetIDictionaryType(type.BaseType); }
			return new KeyValuePair<Type, Type>(null, null);
		}
		public static bool TryCompare(this Type type, object a, object b, out int compareValue) {
			foreach (Type i in type.GetInterfaces()) {
				if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>)) {
					MethodInfo compareTo = type.GetMethod("CompareTo", new Type[]{ type });
					compareValue = (int)compareTo.Invoke(a, new object[] { b });
					return true;
				}
			}
			compareValue = 0;
			return false;
		}
		public static Type[] GetSubClasses(this Type type) {
			Type[] allLocalTypes;
			try {
				allLocalTypes = type.Assembly.GetTypes();
			} catch(Exception) {
				//Show.Error("unable to get assembly subclasses of "+type+":"+e);
				return Type.EmptyTypes;
			}
			List<Type> subTypes = new List<Type>();
			for (int i = 0; i < allLocalTypes.Length; ++i) {
				Type t = allLocalTypes[i];
				if (t.IsClass && !t.IsAbstract && t.IsSubclassOf(type)) { subTypes.Add(t); }
			}
			return subTypes.ToArray();
		}

		public static object GetNewInstance(this Type t) { return Activator.CreateInstance(t); }

		public static System.Type[] GetTypesInNamespace(this Assembly assembly, string nameSpace, bool includeComponentTypes = false) {
			if (assembly == null) {
				assembly = System.Reflection.Assembly.GetExecutingAssembly();
			}
			System.Type[] types = assembly.GetTypes().Where(t =>
				System.String.Equals(t.Namespace, nameSpace, System.StringComparison.Ordinal)
				&& (includeComponentTypes || !t.ToString().Contains('+'))).ToArray();
			return types;
		}
		public static List<string> TypeNamesWithoutNamespace(System.Type[] validTypes, string namespaceToClean) {
			List<string> list = new List<string>();
			for (int i = 0; i < validTypes.Length; ++i) {
				string typename = validTypes[i].ToString();
				typename = typename.RemoveFromFront(namespaceToClean + ".");
				list.Add(typename);
			}
			return list;
		}

		public static IList<string> GetStackFullPath(int stackDepth = 1, int stackStart = 1) {
			StackTrace stackTrace = new StackTrace(stackStart + 1, true);
			int len = Math.Min(stackDepth, stackTrace.FrameCount);
			List<string> stack = new List<string>();
			for (int i = 0; i < len; ++i) {
				StackFrame f = stackTrace.GetFrame(i);
				if (f == null) break;
				string path = f.GetFileName();
				if (path == null) break;
				stack.Add(path + ":" + f.GetFileLineNumber());
			}
			return stack;
		}
		public static string GetStack(int stackDepth = 1, int stackStart = 1, string separator = ", ") {
			StringBuilder sb = new StringBuilder();
			IList<string> stack = GetStackFullPath(stackDepth, stackStart);
			for (int i = 0; i < stack.Count; ++i) {
				string path = stack[i];
				int fileStart = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
				if (fileStart < 0) fileStart = path.LastIndexOf(System.IO.Path.AltDirectorySeparatorChar);
				if (sb.Length > 0) sb.Append(separator);
				sb.Append(path.Substring(fileStart + 1));
			}
			return sb.ToString();
		}
		public static IEnumerable<MethodInfo> FindMethodsWithAttribute<T>(this Type typeToSearchIn) where T : Attribute {
			List<MethodInfo> list = new List<MethodInfo>();
			MethodInfo[] methods = typeToSearchIn.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			Type attributeType = typeof(T);
			for (int i = 0; i < methods.Length; ++i) {
				MethodInfo m = methods[i];
				IEnumerable<object> attributes = m.GetCustomAttributes();
				foreach (object obj in attributes) {
					bool found = obj.GetType().IsAssignableFrom(attributeType);
					if(found) { list.Add(m); }
				}
			}
			return list;
		}
	}
}