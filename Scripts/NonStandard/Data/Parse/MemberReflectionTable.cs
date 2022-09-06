using NonStandard.Extension;
using System;
using System.Reflection;
using System.Text;

namespace NonStandard.Data.Parse {
	public class MemberReflectionTable {
		private Type compiledForType;
		public string[] fieldNames, propNames;
		public FieldInfo[] fields;
		public PropertyInfo[] props;

		public void SetType(Type type) {
			if (type == compiledForType) { return; }
			compiledForType = type;
			fields = null;
			props = null;
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append(fieldNames.JoinToString(", "));
			if (fieldNames.Length > 0 && propNames.Length > 0) { sb.Append(", "); }
			sb.Append(propNames.JoinToString(", "));
			return sb.ToString();
		}

		public FieldInfo GetField(string name) {
			LoadReflectionFieldTable();
			int index = ReflectionParseExtension.FindIndexWithWildcard(fieldNames, name, true);
			return (index < 0) ? null : fields[index];
		}

		private void LoadReflectionFieldTable() {
			if (fields != null) { return; }
			if (compiledForType == null) {
				fields = Array.Empty<FieldInfo>();
				fieldNames = Array.Empty<string>();
				return;
			}
			fields = compiledForType.GetFields();
			Array.Sort(fields, (a, b) => a.Name.CompareTo(b.Name));
			fieldNames = Array.ConvertAll(fields, f => f.Name);
		}

		public PropertyInfo GetProperty(string name) {
			LoadReflectionPropsTable();
			int index = ReflectionParseExtension.FindIndexWithWildcard(propNames, name, true);
			return (index < 0) ? null : props[index];
		}

		private void LoadReflectionPropsTable() {
			if (props != null) { return; }
			if (compiledForType == null) {
				props = Array.Empty<PropertyInfo>();
				propNames = Array.Empty<string>();
				return;
			}
			props = compiledForType.GetProperties();
			Array.Sort(props, (a, b) => a.Name.CompareTo(b.Name));
			propNames = Array.ConvertAll(props, p => p.Name);
		}

		public bool TryGetMemberDetails(string memberName, out Type memberType, out FieldInfo field, out PropertyInfo prop) {
			field = GetField(memberName);
			if (field != null) {
				memberType = field.FieldType;
				prop = null;
			} else {
				prop = GetProperty(memberName);
				if (prop != null) {
					memberType = prop.PropertyType;
				} else {
					memberType = null;
					return false;
				}
			}
			return true;
		}
	}
}
