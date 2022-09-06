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
			if (type == null) {
				fields = Array.Empty<FieldInfo>();
				props = Array.Empty<PropertyInfo>();
				fieldNames = propNames = Array.Empty<string>();
				return;
			}
			if (type == compiledForType) { return; }
			compiledForType = type;
			fields = type.GetFields();
			props = type.GetProperties();
			Array.Sort(fields, (a, b) => a.Name.CompareTo(b.Name));
			Array.Sort(props, (a, b) => a.Name.CompareTo(b.Name));
			fieldNames = Array.ConvertAll(fields, f => f.Name);
			propNames = Array.ConvertAll(props, p => p.Name);
		}
		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append(fieldNames.JoinToString(", "));
			if (fieldNames.Length > 0 && propNames.Length > 0) { sb.Append(", "); }
			sb.Append(propNames.JoinToString(", "));
			return sb.ToString();
		}
		public FieldInfo GetField(string name) {
			int index = ReflectionParseExtension.FindIndexWithWildcard(fieldNames, name, true); return (index < 0) ? null : fields[index];
		}
		public PropertyInfo GetProperty(string name) {
			int index = ReflectionParseExtension.FindIndexWithWildcard(propNames, name, true); return (index < 0) ? null : props[index];
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
