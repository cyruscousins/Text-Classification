using System;

using System.Collections.Generic;
using System.Collections;

using System.Linq;
using Whetstone;

namespace TextCharacteristicLearner
{
	//Should Multiset be an interface?

	public class MultisetKmer<Tyvar> : IEnumerable<KeyValuePair<Kmer<Tyvar>, int>> /* , IDictionary<Kmer<Type>, int> */ {
		public int maxK;
		Multiset<Kmer<Tyvar>>[] data; //data[i] contains kmers of k = i.
		int[] sizes;

		public int Size(int k){
			return sizes[k];
		}

		public int Size(){
			return sizes.Sum ();
		}

		public MultisetKmer(int k){
			this.maxK = k;
			sizes = new int[k + 1];
			data = sizes.Map (a => new Multiset<Kmer<Tyvar>>()).ToArray ();
		}

		public void AddKmer(Kmer<Tyvar> toAdd){
			data[toAdd.Count].Add (toAdd);
			sizes[toAdd.Count]++;
		}

		public void AddKmer(Kmer<Tyvar> toAdd, int count){
			data[toAdd.Count].Add (toAdd, count);
			sizes[toAdd.Count] += count;
		}

		public void AddKmers(IEnumerable<Tyvar> toAdd, int k){
			Tyvar[] toAddArr = toAdd.ToArray (); //TODO: ensure that this leaves an existing array alone.

			/*
			if(toAddArr.Length < k){
				return;
			}
			*/

			for(int i = 0; i < toAddArr.Length - k; i++){
				AddKmer (new Kmer<Tyvar>(toAddArr, i, k));
			}
		}

		public void AddKmers(IEnumerable<Tyvar> toAdd){
			toAdd = toAdd.ToArray ();
			for(int i = 1; i <= maxK; i++){
				AddKmers (toAdd, i);
			}
		}

		public int getCount(Kmer<Tyvar> item){
			return data[item.Count].getCount (item);
		}

		public void ConsumeEventSeriesKmer(IEnumerable<Tyvar> s){
			AddKmers (s);
		}
		public double GetKeyFrac(Kmer<Tyvar> v){
			//Console.WriteLine ("f(" + v + ") = " + (double)getCount (v) + " / " + (double)sizes[v.Count]);
			return (double)getCount (v) / (double)sizes[v.Count];
		}

		public double GetKeyFracLaplace(Kmer<Tyvar> val, double smoothingAmt){
			return ((double)getCount (val) + smoothingAmt) / ((double)sizes[val.Count] + smoothingAmt); //TODO is this laplacian smoothing?
		}
		public double GetKeyFracLaplace(Kmer<Tyvar> val){
			return GetKeyFracLaplace (val, 1);
		}

		public IEnumerator<KeyValuePair<Kmer<Tyvar>, int>> GetEnumerator(){
			return data.Flatten1().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator(){
			return GetEnumerator();
		}

		//TODO: Implement IDictionary
		public IEnumerable<Kmer<Tyvar>> Keys{ get { return data.Map (a => a.Keys).Flatten1(); } } 
		
	}
	/*
	
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

		public Kmer(A[] a, int start, int size){
			data = new ArraySlice_t<A>(a, start, size);
		}



		//HASHING:

		private static int RotateLeft(int value, int count)
		{
		    return (value << count) | (value >> (sizeof(int) - count));
		}
		private static int RotateRight(int value, int count)
		{
		    return (value >> count) | (value << (sizeof(int) - count));
		}

		//TODO cache this.
		public override int GetHashCode ()
		{
			//TODO: This breaks on a 0mer.  Must make those illegal.

			int hash = data[0].GetHashCode();
			int s = data.StartIndex + 1;
			int e = data.StartIndex + data.Count;
			for(int i = s; i < e; i++){
				//Console.WriteLine ("s: " + s + ", i: " + i + ", e: " + e);
				hash = ((int)RotateLeft((int)hash, 1)) ^ data.Array[i].GetHashCode();
			}

			return hash;

			//return (int)this.Aggregate((int)0, (sum, val) => RotateLeft(sum, 1) ^ (int)val.GetHashCode () );
		}

		public override bool Equals (Object o)
		{
			if (o is Kmer<A>) {
				Kmer<A> otherKmer = (Kmer<A>) o;
				if(otherKmer.Count == this.Count){
					return this.Zip (otherKmer, (a, b) => a.Equals (b)).Conjunction (); //TODO: Is this the right equality operator?  Nulls?
				}
			}
			return false;
		}

		public override string ToString(){
			return this.FoldToString(a => a.ToString ());
		}
	}
	 */ 

	//TODO: Rename to HashedKmer, ressurect Kmer.  Make immutable
	public struct Kmer<A> : IEnumerable<A>{

		//TODO: The implementation of this class is a mess, primarily because ArraySegment is completely broken in the version of C# this code targets.
		private ArraySlice_t<A> data;
		int hash;

		//Accessors:
		public int Count {
			get{return data.Count;}
		}


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
			hash = hashCode(data);
		}

		public Kmer(A[] a, int start, int size) : this(new ArraySlice_t<A>(a, start, size)){ }
		
		public void RehashKmer(){
			hash = hashCode(data);
		}

		//HASHING:

		private static int RotateLeft(int value, int count)
		{
		    return (value << count) | (value >> (sizeof(int) - count));
		}
		private static int RotateRight(int value, int count)
		{
		    return (value >> count) | (value << (sizeof(int) - count));
		}

		public override int GetHashCode ()
		{
			return hash;
		}
		private static int hashCode<H>(ArraySlice_t<H> data){
			//TODO: This breaks on a 0mer.  Must make those illegal.

			int hash = data[0].GetHashCode();
			int s = data.StartIndex + 1;
			int e = data.StartIndex + data.Count;
			for(int i = s; i < e; i++){
				//Console.WriteLine ("s: " + s + ", i: " + i + ", e: " + e);
				hash = ((int)RotateLeft((int)hash, 1)) ^ data.Array[i].GetHashCode();
			}

			return hash;

			//return (int)this.Aggregate((int)0, (sum, val) => RotateLeft(sum, 1) ^ (int)val.GetHashCode () );
		}

		public override bool Equals (Object o)
		{
			if (o is Kmer<A>) {
				Kmer<A> otherKmer = (Kmer<A>) o;
				if(otherKmer.Count == this.Count){
					return this.Zip (otherKmer, (a, b) => a.Equals (b)).Conjunction (); //TODO: Is this the right equality operator?  Nulls?
				}
			}
			return false;
		}

		public override string ToString(){
			return this.FoldToString(a => a.ToString ());
		}
	}
}

