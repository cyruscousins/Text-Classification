using System;

using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

using Whetstone;

namespace TextCharacteristicLearner
{

	public class Multiset<Tyvar> : Dictionary<Tyvar, uint>, EventSeriesConsumer<Tyvar>
	{

		public uint size = 0;

		public Multiset(){

		}

		public Multiset(IEnumerable<Tyvar> t){
			t.ForEach (Add);
		}

		public void Add(Tyvar s){
			uint val;
			TryGetValue(s, out val);
			this[s] = val + 1;
			size ++;
		}

		//TODO: Rename to AddMulti
		public void Add(Tyvar s, uint count){
			uint val;
			TryGetValue(s, out val);
			this[s] = val + count;
			size += count;
		}

		public void Add(IEnumerable<Tyvar> s){
			s.ForEach (Add);
		}

		public void ConsumeEventSeries(IEnumerable<Tyvar> s){
			Add (s);
		}

		public uint getCount(Tyvar s){
			uint ret;
			TryGetValue (s, out ret);
			return ret;
		}

		public double GetKeyFrac(Tyvar v){
			return (double)getCount (v) / (double)size;
		}

		public double GetKeyFracLaplace(Tyvar val){
			return ((double)getCount (val) + 1) / ((double)size + 1); //TODO is this laplacian smoothing?
		}

		public double GetKeyFracLaplace(Tyvar val, double smooth){
			return ((double)getCount (val) + smooth) / ((double)size + smooth); //TODO is this laplacian smoothing?
		}

		public void putVal(Tyvar s, uint val){
			base[s] = val;
		}

		/*
		private string flatten(string[] strs){
			return strs.Aggregate("", (sum, val) => sum + "|" + val);
		}
		*/

		public override string ToString(){
			return Keys.FoldToString (key => key + ":" + this[key]);
		}

		public string ToString(int count){
			return Keys.OrderByDescending (key => this[key]).Take (count).FoldToString (key => key + ":" + this[key]);
		}
	}

	public static class Multiset_Extensions{
		public static Multiset<A> sum<A>(this IEnumerable<Multiset<A>> sets){ //TODO rename union.
			Multiset<A> d = new Multiset<A>();
			//TODO add number
			sets.ForEach (aset => aset.ForEach(kvp => d.Add(kvp.Key, kvp.Value)));
			return d;		
		}
		public static MultisetKmer<A> sumKmers<A>(this IEnumerable<MultisetKmer<A>> sets){
			//TODO check they all have the same k?

			MultisetKmer<A> d = new MultisetKmer<A>(sets.First ().maxK);
			//TODO add number
			sets.ForEach (aset => aset.ForEach(kvp => d.AddKmer(kvp.Key, kvp.Value)));
			return d;		
		}

		public static void AddKmers<A>(this Multiset<Kmer<A>> thisSet, IEnumerable<A> toAdd, uint k){
			A[] toAddArr = toAdd.ToArray ();
			
			if(toAddArr.Length < k){
				return;
			}

			for(uint i = 0; i < toAddArr.Length - k; i++){
				thisSet.Add (new Kmer<A>(toAddArr, i, k));
			}
		}
	}
}

