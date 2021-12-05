using System;
using System.Collections.Generic;
using System.Text;

namespace NonStandard.Extension {
	public static class IListExtension {
		public static int IndexOf<T>(this IList<T> list, T value) where T : IComparable {
			for(int i = 0; i < list.Count; ++i) { if (list[i].CompareTo(value) == 0) return i; }
			return -1;
		}
		public static int IndexOfLast<T>(this IList<T> list, T value) where T : IComparable {
			for (int i = list.Count-1; i >= 0; --i) { if (list[i].CompareTo(value) == 0) return i; }
			return -1;
		}
		public static int BinarySearchIndexOf<T>(this IList<T> list, T value, IComparer<T> comparer = null) {
			if (list == null) { throw new ArgumentNullException("list"); }
			if (comparer == null) { comparer = Comparer<T>.Default; }
			return list.BinarySearchIndexOf(value, comparer.Compare);
		}
		public static int BinarySearchIndexOf<T>(this IList<T> list, T value, Func<T, T, int> comparer) {
			if (list == null) { throw new ArgumentNullException("list"); }
			int lower = 0, upper = list.Count - 1;
			while (lower <= upper) {
				int middle = lower + (upper - lower) / 2, comparisonResult = comparer.Invoke(value, list[middle]);
				if (comparisonResult == 0) return middle;
				if (comparisonResult < 0) upper = middle - 1;
				else lower = middle + 1;
			}
			return ~lower;
		}
		public static int BinarySearchInsert<T>(this IList<T> list, T value, Func<T, T, int> comparer) {
			if (list == null) { throw new ArgumentNullException("list"); }
			int result = BinarySearchIndexOf(list, value, comparer);
			int index = result;
			if (index < 0) { index = ~index; }
			list.Insert(index, value);
			return result;
		}
		public static bool IsSorted<T>(this IList<T> list, IComparer<T> comparer = null) {
			if (list == null) { throw new ArgumentNullException("list"); }
			if (comparer == null) { comparer = Comparer<T>.Default; }
			for(int i = 1; i < list.Count; ++i) {
				if (comparer.Compare(list[i - 1], list[i]) > 0) return false;
			}
			return true;
		}
		public static void ForEach<T>(this IList<T> source, Action<T> action) { ForEach(source, action, 0, source.Count); }
		public static void ForEach<T>(this IList<T> source, Action<T> action, int index, int length) {
			for (int i = 0; i < length; ++i) { action.Invoke(source[index + i]); }
		}
		public static void SetEach<T>(this IList<T> source, T value) { for (int i = 0; i < source.Count; ++i) { source[i] = value; } }
		public static void SetEach<T>(this IList<T> source, Func<int, T> action) { SetEach(source, action, 0, source.Count); }
		public static void SetEach<T>(this IList<T> source, Func<int, T> action, int index, int length) {
			for (int i = 0; i < length; ++i) { source[i] = action.Invoke(index + i); }
		}
		public static T[] GetRange<T>(this IList<T> source, int index, int length) {
			T[] list = new T[length];
			for (int i = 0; i < length; ++i) { list[i] = source[index + i]; }
			return list;
		}
		public static int FindIndex<T>(this IList<T> list, Func<T, bool> predicate) {
			for (int i = 0; i < list.Count; ++i) { if (predicate(list[i])) return i; }
			return -1;
		}
		public static List<int> FindIndexes<T>(this IList<T> list, Func<T, bool> predicate) {
			List<int> indexes = new List<int>();
			for (int i = 0; i < list.Count; ++i) { if (predicate(list[i])) { indexes.Add(i); } }
			return indexes;
		}
		public static T Find<T>(this IList<T> list, Func<T, bool> predicate) {
			for (int i = 0; i < list.Count; ++i) { if (predicate(list[i])) return list[i]; }
			return default(T);
		}
		public static List<T> FindAll<T>(this IList<T> list, Func<T, bool> predicate) {
			List<T> result = new List<T>();
			for (int i = 0; i < list.Count; ++i) { if (predicate(list[i])) result.Add(list[i]); }
			return result;
		}
		public static int CountEach<T>(this IList<T> list, Func<T, bool> predicate) {
			int count = 0;
			for (int i = 0; i < list.Count; ++i) { if (predicate(list[i])) ++count; }
			return count;
		}
		public static int Sum<T>(this IList<T> list, Func<T, int> valueFunction) {
			int sum = 0; for (int i = 0; i < list.Count; ++i) { sum += valueFunction(list[i]); }
			return sum;
		}
		public static float Sum<T>(this IList<T> list, Func<T, float> valueFunction) {
			float sum = 0; for (int i = 0; i < list.Count; ++i) { sum += valueFunction(list[i]); }
			return sum;
		}
		public static int[] GetNestedIndex<T>(this IList<IList<T>> list, int flatIndex) {
			int[] path = new int[2] { -1, -1 };
			int original = flatIndex;
			if (flatIndex >= 0) {
				for (int i = 0; i < list.Count; ++i) {
					if (flatIndex < list[i].Count) { path[0] = i; path[1] = flatIndex; break; }
					flatIndex -= list[i].Count;
				}
			}
			if (path[0] < 0 || path[1] < 0) {
				throw new Exception("could not convert " + original + " into index from " + list.Count + " lists totalling " +
					list.Sum(l => l.Count) + " elements");
			}
			return path;
		}
		public static T GetFromNestedIndex<T>(this IList<IList<T>> list, int[] nestedIndex) {
			return list[nestedIndex[0]][nestedIndex[1]];
		}
		/// <summary>
		/// essentially converts into an array
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <returns></returns>
		public static T[] SubList<T>(this IList<T> list) { return SubList(list, 0, list.Count); }
		public static T[] SubList<T>(this IList<T> list, int startIndex) {
			return SubList(list, startIndex, list.Count - startIndex);
		}

		public static T[] SubList<T>(this IList<T> list, int startIndex, int length) {
			T[] result = new T[length];
			if (startIndex + length > list.Count) {
				throw new IndexOutOfRangeException($"length parameter is too large, index limit {startIndex + length} exceeds Count {list.Count}.");
			}
			for (int i = 0; i < length; ++i) {
				result[i] = list[i + startIndex];
			}
			return result;
		}

		public static T Min<T>(this IList<T> self) where T : IComparable<T> {
			if (self == null || self.Count == 0) return default(T);
			T min = self[0];
			for (int i = 1; i < self.Count; ++i) { if (self[i].CompareTo(min) < 0) { min = self[i]; } }
			return min;
		}
		public static T Max<T>(this IList<T> self) where T : IComparable<T> {
			if (self == null || self.Count == 0) return default(T);
			T max = self[0];
			for (int i = 1; i < self.Count; ++i) { if (self[i].CompareTo(max) > 0) { max = self[i]; } }
			return max;
		}
	}
}