using System;

using System.Collections;
using System.Collections.Generic;

using Whetstone;
using System.Linq;

namespace TextCharacteristicLearner
{
	public class DiscreteEventSeries<Ty> : IEnumerable<Ty>
	{
		public Dictionary<string, string> labels;
		public Ty[] data;

		public DiscreteEventSeries (Dictionary<string, string> labels, Ty[] data)
		{
			this.labels = labels;
			this.data = data;
		}

		public IEnumerator<Ty> GetEnumerator(){
			return ((IEnumerable<Ty>)data).GetEnumerator(); //Late binding of Array generic types, sorry.
		}

		IEnumerator IEnumerable.GetEnumerator(){
			return GetEnumerator ();
		}
	}

	public static class DiscreteEventSeriesExtensions{

		//
		//Raw multiset:
		//
		public static void AddDiscreteEventSeries<Ty>(this Multiset<Ty> multiset, DiscreteEventSeries<Ty> series){
			//TODO: Is it faster to make the set into a multiset, and then add the counts, so there are fewer lookups in the bigger multiset?
			series.data.ForEach(a => multiset.Add (a));
		}
		public static Multiset<Ty> ToMultiset<Ty>(this DiscreteEventSeries<Ty> series){
			Multiset<Ty> multiset = new Multiset<Ty>();
			series.data.ForEach(a => multiset.Add (a)); //Doesn't use the above, as an optimization.
			return multiset;
		}
		
		public static Multiset<Ty> ToMultiset<Ty>(this IEnumerable<DiscreteEventSeries<Ty>> series){
			Multiset<Ty> multiset = new Multiset<Ty>();
			series.ForEach (multiset.AddDiscreteEventSeries);
			return multiset;
		}

		//
		//Kmer fixed k multiset
		//
		public static void AddDiscreteEventSeriesKmer<Ty>(this Multiset<Kmer<Ty>> multiset, DiscreteEventSeries<Ty> series, uint k ){
			//Ty[] arr = series.data.ToArray();
			Ty[] arr = series.data; //TODO: This is a decision.
			for(uint i = 0; i <= arr.Length - k; i++){
				multiset.Add (new Kmer<Ty>(arr, i, k));
			}
		}

		public static Multiset<Kmer<Ty>> ToMultisetKmer<Ty>(this DiscreteEventSeries<Ty> series, uint k){
			Multiset<Kmer<Ty>> multiset = new Multiset<Kmer<Ty>>();
			multiset.AddDiscreteEventSeriesKmer(series, k);
			return multiset;
		}
		
		public static Multiset<Kmer<Ty>> ToMultisetKmer<Ty>(this IEnumerable<DiscreteEventSeries<Ty>> series, uint k){
			Multiset<Kmer<Ty>> multiset = new Multiset<Kmer<Ty>>();
			series.ForEach (item => multiset.AddDiscreteEventSeriesKmer(item, k));
			return multiset;
		}

		//
		//Kmer variadic k multiset
		//
		public static void AddDiscreteEventSeriesVarKmer<Ty>(this MultisetKmer<Ty> multiset, DiscreteEventSeries<Ty> series, uint k ){
			Ty[] arr = series.data;
			multiset.ConsumeEventSeriesKmer(arr);
		}

		public static MultisetKmer<Ty> ToMultisetVarKmer<Ty>(this DiscreteEventSeries<Ty> series, uint k){
			MultisetKmer<Ty> multiset = new MultisetKmer<Ty>(k);
			multiset.AddDiscreteEventSeriesVarKmer(series, k);
			return multiset;
		}
		
		public static MultisetKmer<Ty> ToMultisetVarKmer<Ty>(this IEnumerable<DiscreteEventSeries<Ty>> series, uint k){
			MultisetKmer<Ty> multiset = new MultisetKmer<Ty>(k);
			series.ForEach (item => multiset.AddDiscreteEventSeriesVarKmer(item, k));
			return multiset;
		}

		public static int TotalItemCount<Ty>(this IEnumerable<DiscreteEventSeries<Ty>> data){
			return data.Select (item => item.data.Length).Sum ();
		}

	}
}
