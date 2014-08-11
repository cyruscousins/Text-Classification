using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Whetstone;

namespace TextCharacteristicLearner
{
	
	//Represents the set of items that are characteristic for a certain class. 
	public class ClassCharacteristicSetKmer<A> : Dictionary<Kmer<A>, double>
	{
		public string name;
		uint k;
		uint kmerCount;
		
		public ClassCharacteristicSetKmer (string name, uint k){
			this.name = name;
			this.k = k;
		}
		
		public ClassCharacteristicSetKmer (string name, uint k, uint initialCapacity) : base((int)initialCapacity) {
			this.name = name;
			this.k = k;
		}

		public static ClassCharacteristicSetKmer<A> BuildSubtractiveDifference(string name, MultisetKmer<A> baselineClass, MultisetKmer<A> thisClass, uint countCutoff)
		{
			ClassCharacteristicSetKmer<A> newSet = new ClassCharacteristicSetKmer<A>(name, Math.Min (baselineClass.maxK, thisClass.maxK));

			//TODO statistically significant?
			//TODO diffence amount?

			foreach(Kmer<A> key in thisClass.Keys){
				if(thisClass.getCount(key) > countCutoff){
					double thisFrac = thisClass.GetKeyFrac(key);
					double baseFrac = baselineClass.GetKeyFrac (key);
					if(thisFrac > baseFrac){
						newSet.Add (key, thisFrac - baseFrac);
					}
				}
			}

			//TODO select top x?
			//double[] function?

			return newSet;
		}

}

