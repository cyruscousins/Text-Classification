using System;

using System.Collections.Generic;
using System.Collections;

using System.Linq;
using Whetstone;

namespace TextCharacteristicLearner
{
	//Should Multiset be an interface?

	public class MultisetKmer<Tyvar> : IEnumerable<KeyValuePair<Kmer<Tyvar>, uint>> /* , IDictionary<Kmer<Type>, int> */ {
		public uint maxK;
		Multiset<Kmer<Tyvar>>[] data; //data[i] contains kmers of k = i.
		uint[] sizes;

		public uint Size(uint k){
			return sizes[k];
		}

		public uint Size(){
			return sizes.Sum ();
		}

		public MultisetKmer(uint k){
			this.maxK = k;
			sizes = new uint[k + 1];
			data = sizes.Map (a => new Multiset<Kmer<Tyvar>>()).ToArray ();
		}

		public void AddKmer(Kmer<Tyvar> toAdd){
			data[toAdd.data.Count].Add (toAdd);
			sizes[toAdd.data.Count]++;
		}

		public void AddKmer(Kmer<Tyvar> toAdd, uint count){
			data[toAdd.data.Count].Add (toAdd, count);
			sizes[toAdd.data.Count] += count;
		}

		public void AddKmers(IEnumerable<Tyvar> toAdd, uint k){
			Tyvar[] toAddArr = toAdd.ToArray (); //TODO: ensure that this leaves an existing array alone.

			/*
			if(toAddArr.Length < k){
				return;
			}
			*/

			for(uint i = 0; i < toAddArr.Length - k; i++){
				AddKmer (new Kmer<Tyvar>(toAddArr, i, k));
			}
		}

		public void AddKmers(IEnumerable<Tyvar> toAdd){
			toAdd = toAdd.ToArray ();
			for(uint i = 1; i <= maxK; i++){
				AddKmers (toAdd, i);
			}
		}

		public uint getCount(Kmer<Tyvar> item){
			return data[item.data.Count].getCount (item);
		}

		public void ConsumeEventSeriesKmer(IEnumerable<Tyvar> s){
			AddKmers (s);
		}
		public double GetKeyFrac(Kmer<Tyvar> v){
			//Console.WriteLine ("f(" + v + ") = " + (double)getCount (v) + " / " + (double)sizes[v.data.Count]);
			return (double)getCount (v) / (double)sizes[v.data.Count];
		}

		public double GetKeyFracLaplace(Kmer<Tyvar> val, double smoothingAmt){
			return ((double)getCount (val) + smoothingAmt) / ((double)sizes[val.data.Count] + smoothingAmt); //TODO is this laplacian smoothing?
		}
		public double GetKeyFracLaplace(Kmer<Tyvar> val){
			return GetKeyFracLaplace (val, 1);
		}

		public IEnumerator<KeyValuePair<Kmer<Tyvar>, uint>> GetEnumerator(){
			return data.Flatten1().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator(){
			return GetEnumerator();
		}

		//TODO: Implement IDictionary
		public IEnumerable<Kmer<Tyvar>> Keys{ get { return data.Map (a => a.Keys).Flatten1(); } } 
		
	}

	public struct Kmer<A> : IEnumerable<A>{

		//TODO: The implementation of this class is a mess, primarily because ArraySegment is completely broken in the version of C# this code targets.
		public ArraySlice_t<A> data;

		//IENUMERABLE:

		
		//TODO: Update to a runtime where this class is already IEnumerable.
		public IEnumerator<A> GetEnumerator(){
			return data.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator(){ //TODO it is totally broken that this needs to be implemented.  Why?
			return GetEnumerator ();
		}

		public A this[int i]{
			get { return data[i]; }
			set { data[i] = value; }
		}

		//CONSTRUCTOR:

		public Kmer(ArraySlice_t<A> data){
			this.data = data;
		}

		public Kmer(A[] a, uint start, uint size){
			data = new ArraySlice_t<A>(a, start, size);
		}



		//HASHING:

		private static uint RotateLeft(uint value, int count)
		{
		    return (value << count) | (value >> (sizeof(uint) - count));
		}
		private static uint RotateRight(uint value, int count)
		{
		    return (value >> count) | (value << (sizeof(uint) - count));
		}

		//TODO cache this.
		public override int GetHashCode ()
		{
			//TODO: This breaks on a 0mer.  Must make those illegal.

			int hash = data[0].GetHashCode();
			uint s = data.StartIndex + 1;
			uint e = data.StartIndex + data.Count;
			for(uint i = s; i < e; i++){
				//Console.WriteLine ("s: " + s + ", i: " + i + ", e: " + e);
				hash = ((int)RotateLeft((uint)hash, 1)) ^ data.Array[i].GetHashCode();
			}

			return hash;

			//return (int)this.Aggregate((uint)0, (sum, val) => RotateLeft(sum, 1) ^ (uint)val.GetHashCode () );
		}

		public override bool Equals (Object o)
		{
			if (o is Kmer<A>) {
				Kmer<A> otherKmer = (Kmer<A>) o;
				if(otherKmer.data.Count == this.data.Count){
					return this.Zip (otherKmer, (a, b) => a.Equals (b)).All ( a => a ); //TODO is this the right equality function to use?  Do we need to handle nulls?  Can the all be expressed more succinctly?
				}
			}
			return false;
		}

		public override string ToString(){
			return this.FoldToString(a => a.ToString ());
		}
	}
}

