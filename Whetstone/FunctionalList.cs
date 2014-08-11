using System;

using System.Collections.Generic;
using System.Collections;

using System.Linq;

namespace Whetstone
{
	public class ConsCell<Ty> : IEnumerable<Ty>{
		Ty item;
		IEnumerable<Ty> next;

		public ConsCell (Ty item, IEnumerable<Ty> next)
		{
			this.item = item;
			this.next = next;
		}
				
		public IEnumerator<Ty> GetEnumerator(){
			//TODO: We want this in a sense to function recursively.
			//Here we manually unroll first, this happens once for each append.
			//This (n) appends result in an IEnumerable that takes O(n^2) time to enumerate.
			//Is there a more efficient way to express this?
			
			yield return item;

			foreach(Ty t in next){
				yield return t;
			}

			yield break;
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator(){
			return GetEnumerator();
		}
	}

	public class AppendCell<Ty> : IEnumerable<Ty>{
		Ty item;
		IEnumerable<Ty> first;

		public AppendCell (IEnumerable<Ty> first, Ty item)
		{
			this.item = item;
			this.first = first;
		}

		public IEnumerator<Ty> GetEnumerator(){
			//TODO: We want this in a sense to function recursively.
			//Here we manually unroll first, this happens once for each append.
			//Is there a more efficient way to express this?

			foreach(Ty t in first){
				yield return t;
			}

			yield return item;
			yield break;
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator(){
			return GetEnumerator();
		}
	}

	//TODO is this the best way to do this.
	public class EmptyList<Ty> : IEnumerable<Ty>{

		public static EmptyList<Ty> NULL = new EmptyList<Ty>();

		private EmptyList(){} //No public constructing allowed!

		public IEnumerator<Ty> GetEnumerator(){
			yield break;
		}

		IEnumerator System.Collections.IEnumerable.GetEnumerator(){
			return GetEnumerator();
		}
	}

	public static class FunctionalListExtensions{
		public static IEnumerable<Ty> Cons<Ty>(this Ty head, IEnumerable<Ty> tail){
			return new ConsCell<Ty>(head, tail);
		}
		public static IEnumerable<Ty> Append<Ty>(this IEnumerable<Ty> list, Ty toAdd){
			return new AppendCell<Ty>(list, toAdd);
		}
		public static IEnumerable<Ty> Flatten1<Ty>(this IEnumerable<IEnumerable<Ty>> lists){
			return lists.Aggregate((IEnumerable<Ty>)EmptyList<Ty>.NULL, (sum, val) => sum.Concat(val));
		}
	}
}

