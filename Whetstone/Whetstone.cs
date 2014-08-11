using System;

using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

using System.Linq;

namespace Whetstone{
	public static class WhetStone
	{
		public static void ForEach<A> (this IEnumerable<A> e, Action<A> a)
		{
			foreach (A item in e) {
				a(item);
			}
		}
		public static IEnumerable<TResult> Map<TSource, TResult>(this IEnumerable<TSource> e, Func<TSource, TResult> mapping){
			return e.Select (mapping);
		}

		//The classic antifunctional higher order function, map in place applies an operation in place to every element of an array.
		public static A[] MapInPlace<A>(this A[] arr, Func<A, A> op){
			for(int i = 0; i < arr.Length; i++){
				arr[i] = op(arr[i]);
			}
			return arr;
		}
		public static IEnumerable<A> Filter<A>(this IEnumerable<A> e, Func<A, bool> predicate){
			return e.Where(predicate);
		}
		public static A Fold<A, B>(this IEnumerable<B> e, Func<B, B, A> reducer){
			return e.Fold(reducer);
		}

		// An extremely clean an concise way to represent a set of any type as a string.
		public static string FoldToString<Ty>(this IEnumerable<Ty> a, Func<Ty, string> objToStr, string l = "{ ", string r = " }", string d = ", ")
		{

			StringBuilder sb = new StringBuilder(l);

			IEnumerator<Ty> enumerator = a.GetEnumerator ();

			if(enumerator.MoveNext ()){
				sb.Append (objToStr(enumerator.Current));
			}

			while(enumerator.MoveNext ())
			{
				sb.Append(d);
				sb.Append (objToStr(enumerator.Current));
			}

			sb.Append (r);
			return sb.ToString ();
		}

		public static string FoldToString<Ty>(this IEnumerable<Ty> data, string l = "{ ", string r = " }", string d = ", "){
			return FoldToString (data, a => a.ToString(), l, r, d);
		}

		/*
		//TODO why doesn't this work?
		public static A ArgMax<A, B>(this IEnumerable<A> data, Func<A, B> f){
			return data.ElementAtMax(f);
		}
		*/

		///*
		//TODO is there a comparable interface or something to use instead of double?
		public static A ArgMax<A>(this IEnumerable<A> data, Func<A, double> f){
			A max = data.First();
			double val = Double.MinValue;
			foreach(A a in data){
				double thisVal = f(a);
				if(thisVal > val){
					val = thisVal;
					max = a;
				}
			}
			return max;
		}
		//*/

		//TODO: Does this concept exist, and if so what is its name?

		public static void ForEachAdjacentPair<Ty>(this IEnumerable<Ty> vals, Action<Ty, Ty> f){
			IEnumerator<Ty> e = vals.GetEnumerator();

			while(e.MoveNext ()){
				Ty a = e.Current;
				e.MoveNext ();
				Ty b = e.Current;

				f(a, b);
			}
		}

		//TODO: Does this exist?  It's a bit like a mix between "Filter" and "GroupBy"
		public static Tuple<IEnumerable<Ty>, IEnumerable<Ty>> Partition<Ty>(this IEnumerable<Ty> data, Predicate<Ty> predicate){
			List<Ty> a = new List<Ty>();
			List<Ty> b = new List<Ty>();
			foreach(Ty t in data){
				(predicate(t) ? a : b).Add(t);
			}
			return new Tuple<IEnumerable<Ty>, IEnumerable<Ty>>(a, b);
		}

		public static Tuple<List<Ty>, List<Ty>> SplitToLists<Ty> (this IEnumerable<Ty> data, int count)
		{
			List<Ty> l1 = new List<Ty> (count);
			List<Ty> l2 = new List<Ty> ();

			IEnumerator<Ty> e = data.GetEnumerator ();
			for (int i = 0; i < count; i++) {
				if(!e.MoveNext ()) break;
				l1.Add (e.Current);
			}
			while(e.MoveNext ()){
				l2.Add (e.Current);
			}
			return new Tuple<List<Ty>, List<Ty>>(l1, l2);
		}

		//TODO: Is there a name for this?  This works like fold but on a mutable object.  For use with afluent interfaces.
		//This is a strange hybrid between functional and object oriented, perhaps this function is an unholy hybrid that is best left unwritten.
		public static TRes FoldAfluent<TSrc, TRes>(this IEnumerable<TSrc> source, TRes acc, Action<TRes, TSrc> f){
			foreach(TSrc t in source){
				f(acc, t);
			}
			return acc;
		}
		

		//This should really be there already.

		public static bool IsEmpty<A>(this IEnumerable<A> data){
			return !data.Any ();
		}

		//Lists
		
		public static T[] Shuffle<T>(this IEnumerable<T> source, Random rng)
		{
		    T[] arr = source.ToArray();

		    for (int i = 0; i < arr.Length; i++)
		    {
		        int rIndex = rng.Next(i, arr.Length);
				arr.Swap (i, rIndex);
		    }
			return arr;
		}

		public static T[] Shuffle<T>(this IEnumerable<T> source){
			return source.Shuffle (new Random());
		}

		//TODO: Move to ArrayUtilities
		public static void Swap<A>(this A[] arr, int i0, int i1){
			A temp = arr[i0];
			arr[i0] = arr[i1];
			arr[i1] = temp;
		}




		public static IEnumerable<Ty> Order<Ty>(this IEnumerable<Ty> items){
			return items.OrderBy(a => a);
		}

		public static IEnumerable<Ty> OrderDescending<Ty>(this IEnumerable<Ty> items){
			return items.OrderByDescending(a => a);
		}

		//Returns the top k items from the enumerable, or all if fewer than k exist.  Gives no guarantee about the order of the returned results.
		/*
		public static IEnumerable<Ty> TopUnordered<Ty>(this List<Ty> items, int k){
			//TODO Implement this to use expected linear time Kth order statistics.
		}
		*/
		public static IEnumerable<Ty> TopUnordered<Ty>(this IEnumerable<Ty> items, int k){
			//return items.ToList().TopUnordered(k);
			return items.Order().Take(k);
		}

		//TODO percentiles.
	}

	/*
	//TODO is there a way to make this type agnostic?
	public class EmptySet<Type> : IEnumerable<Type>{
        public IEnumerator<T> GetEnumerator() 
        { 
			//Never do anything.
        } 
 
        // We must implement this method because  
        // IEnumerable<T> inherits IEnumerable 
        IEnumerator IEnumerable.GetEnumerator() 
        { 
            return GetEnumerator(); 
        } 
    } 

	*/

	public static class StringUtilities{
		//String/text

		public static string[] RegexSplit(this string str, string pattern){
			return Regex.Split(str, pattern);
		}

		public static string RegexReplace(this string str, string pattern, string replacement){
			return Regex.Replace (str, pattern, replacement);
		}
	}

	public struct ArraySlice_t<A> : IEnumerable<A>{
		public A[] Array; //TODO rename these?  They are the same as ArraySeg<A> right now.
		public uint Count;
		public uint StartIndex;

		public ArraySlice_t(A[] Arr, uint StartIndex, uint Count){
			this.Array = Arr;
			this.Count = Count;
			this.StartIndex = StartIndex;
		}

		public IEnumerator<A> GetEnumerator(){
			uint endIndex = StartIndex + Count;
			for(uint i = StartIndex; i < endIndex; i++){
				yield return Array[i];
			}
		}
		IEnumerator IEnumerable.GetEnumerator(){
			return GetEnumerator ();
		}

		public A this[int i]{
			get { return Array[i + StartIndex]; }
			set { Array[i + StartIndex] = value; }
		}

	}

}