using System;

using System.Collections.Generic;
using System.Linq;

namespace Whetstone
{
	public static class ArrayUtilities{

		public static A[] SliceCopy<A>(this A[] inArr, uint start, uint len){
			return Slice(inArr, start, len).ToArray();
		}

		public static ArraySlice_t<A> Slice<A>(this A[] inArr, uint start, uint len){
			return new ArraySlice_t<A>(inArr, start, len);
		}
	}

	public static class ArrayMathUtilities{

		//TODO: How to write generic "math code": this needs to work for float, double, and decimal type.  Need a way to mandate that arithmetic operators are defined? Is there a generic constraint for this?
		//C# does not yet support this.
		public static double[] NormalizeSumToInPlace(this double[] input, double sumVal){
			double sum = input.Sum();
			double invSum = 1 / sum;
			for(int i = 0; i < input.Length; i++){
				input[i] *= invSum;
			}
			return input;
		}
		public static double[] NormalizeSumInPlace(this double[] input){
			return NormalizeSumToInPlace (input, 1.0);
		}

		public static double[] NormalizeSumTo(this double[] input, double sumVal){
			return ((double[])input.Clone()).NormalizeSumToInPlace(sumVal);
		}
		public static double[] NormalizeSum(this double[] input){
			return input.NormalizeSumTo(1.0);
		}


		public static double[] NormalizeInPlace(this double[] input){
			double len = input.Select (item => item * item).Sum ().Sqrt ();
			input.MapInPlace(item => item *= 1 / len);
			return input;
		}

		public static int MaxIndex(this double[] input){
			int maxIndex = 0;
			for(int i = 1; i < input.Length; i++){
				if(input[i] > input[maxIndex]){
					maxIndex = i;
				}
			}
			return maxIndex;
		}
	}

	public static class DictionaryExtensions{
		public static ValueType GetWithDefault<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dict, KeyType key){
			ValueType val;
			if(dict.TryGetValue(key, out val)){
				return val;
			}
			else{
				return default(ValueType);
			}
		}

		//Why is this not allowed?
		/*
		public static ValueType GetWithDefault<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dict, KeyType key) where KeyType : new() {
			ValueType val;
			if(dict.TryGetValue(key, out val)){
				return val;
			}
			else{
				return new ValueType();
			}
		}
		*/

		public static ValueType GetWithDefault<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dict, KeyType key, ValueType def){
			ValueType val;
			if(dict.TryGetValue(key, out val)){
				return val;
			}
			else{
				//dict[key] = def;
				return def;
			}
		}

		public static ValueType GetWithDefault<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dict, KeyType key, Func<ValueType> gen){
			ValueType val;
			if(dict.TryGetValue(key, out val)){
				return val;
			} else {
				ValueType v = gen();
				//dict[key] = v;
				return v;
			}
		}

		public static ValueType GetWithDefaultAndAdd<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dict, KeyType key, ValueType def){
			ValueType val;
			if(dict.TryGetValue(key, out val)){
				return val;
			}
			else{
				dict[key] = def;
				return def;
			}
		}

		public static ValueType GetWithDefaultAndAdd<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dict, KeyType key, Func<ValueType> gen){
			ValueType val;
			if(dict.TryGetValue(key, out val)){
				return val;
			} else {
				ValueType v = gen();
				dict[key] = v;
				return v;
			}
		}

		//Why would the standard libraries provide a KeyValuePair type, but not a function to add it to a dictionary?  
		public static void Add<KeyType, ValueType>(this Dictionary<KeyType, ValueType> dict, KeyValuePair<KeyType, ValueType> item){
			dict.Add (item.Key, item.Value);
		}

		public static Dictionary<KeyType, ValueType> ToDictionary<KeyType, ValueType>(this IEnumerable<KeyValuePair<KeyType, ValueType>> items){
			Dictionary<KeyType, ValueType> dict = new Dictionary<KeyType, ValueType>();
			items.ForEach(item => dict.Add (item));
			return dict;
		}

		public static Dictionary<KeyType, int> IndexLookupDictionary<KeyType>(this IEnumerable<KeyType> items){
			return items.Select ((item, index) => new KeyValuePair<KeyType, int>(item, index)).ToDictionary ();
		}
	}

	public static class MatrixExtensions{

		// A matrix is a Ty[,].  The row number (or y coordinate) is the first argument, and the column number the second.
		// Row a, column b in matrix A is thus represented as A[a,b]

		public static IEnumerable<Ty> EnumerateRow<Ty>(this Ty[,] matrix, int row){
			for(int i = matrix.GetLowerBound (1); i <= matrix.GetUpperBound (1); i++){
				yield return matrix[row, i];
			}
		}

		public static IEnumerable<Ty> EnumerateColumn<Ty>(this Ty[,] matrix, int col){
			for(int i = matrix.GetLowerBound (0); i <= matrix.GetUpperBound (0); i++){
				yield return matrix[i, col];
			}
		}

		public static IEnumerable<IEnumerable<Ty>> EnumerateRows<Ty>(this Ty[,] matrix){
			for(int i = matrix.GetLowerBound (0); i <= matrix.GetUpperBound(0); i++){
				yield return matrix.EnumerateRow (i);
			}
		}

		//ROW/COLUMN SUMMATION
		public static int SumRow(this int[,] matrix, int row){
			return matrix.EnumerateRow (row).Sum ();
		}

		public static double SumRow(this double[,] matrix, int row){
			return matrix.EnumerateRow(row).Sum ();
		}
		
		public static int SumColumn(this int[,] matrix, int column){
			return matrix.EnumerateColumn(column).Sum();
		}

		public static double SumColumn(this double[,] matrix, int column){
			return matrix.EnumerateColumn(column).Sum ();
		}
	}
}

