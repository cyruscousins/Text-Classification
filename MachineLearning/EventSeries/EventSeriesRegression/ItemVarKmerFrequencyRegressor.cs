using System;

using System.Collections.Generic;
using System.Linq;
using Whetstone;

namespace TextCharacteristicLearner
{
	public class ItemVarKmerFrequencyRegressor<A> : IEventSeriesScalarRegressor<A>
	{
		ItemFrequencyRegressor<Kmer<A>> regressor;
		uint maxK;

		public ItemVarKmerFrequencyRegressor (string name, uint minSignificantCount, uint smoothingAmount, uint featuresToUse, uint k)
		{
			regressor = new ItemFrequencyRegressor<Kmer<A>>(name, minSignificantCount, smoothingAmount, featuresToUse);
			this.maxK = k;
		}

		
		public ItemVarKmerFrequencyRegressor (string name, uint minSignificantCount, uint smoothingAmount, uint featuresToUse, uint k, MultisetKmer<A> baselineClass, MultisetKmer<A> thisClass) : this(name, minSignificantCount, smoothingAmount, featuresToUse, k)
		{
			TrainModelRatios (baselineClass, thisClass);
		}

		public string label{get {return regressor.name;}}

		
		//TRAINING:

		//One of the Train functions is called to train a raw model, which is trimmed down to size with the finalizeModel() function.

		public void TrainModelSubtractive(MultisetKmer<A> baselineClass, MultisetKmer<A> thisClass){
			//TODO
			List<KeyValuePair<Kmer<A>, double>> rawModel = new List<KeyValuePair<Kmer<A>, double>>();

			uint totalCount = 0;
			foreach(Kmer<A> key in thisClass.Keys){
				uint thisCount = thisClass.getCount(key);
				totalCount += thisCount;
				if(thisCount > regressor.minSignificantCount){
					double thisFrac = thisClass.GetKeyFrac(key);
					double baseFrac = baselineClass.GetKeyFracLaplace(key, regressor.smoothingAmount);
					if(thisFrac > baseFrac){
						rawModel.Add (key, thisFrac - baseFrac);
					}
				}
			}

			regressor.finalizeModel (rawModel, totalCount);
		}

		public void TrainModelRatios(MultisetKmer<A> baselineClass, MultisetKmer<A> thisClass){
			List<KeyValuePair<Kmer<A>, double>> rawModel = new List<KeyValuePair<Kmer<A>, double>>();

			uint totalCount = 0;
			foreach(Kmer<A> key in thisClass.Keys){
				uint thisCount = thisClass.getCount(key);
				totalCount += thisCount;
				if(thisCount > regressor.minSignificantCount){
					double thisFrac = thisClass.GetKeyFrac(key);
					double baseFrac = baselineClass.GetKeyFracLaplace (key, regressor.smoothingAmount);
					if(thisFrac > baseFrac){
						rawModel.Add (key, thisFrac / baseFrac);
					}
				}
			}

			regressor.finalizeModel (rawModel, totalCount);
		}

		//Regression.
		public double RegressEventSeries(DiscreteEventSeries<A> series){
			double score = 0;
			for(uint k = 1; k <= maxK; k++){ //For locality!
				Multiset<Kmer<A>> ms = series.ToMultisetKmer (k);
				score += regressor.RegressEventSeries(ms);
			}
			return score;
		}

		public override string ToString ()
		{
			return regressor.ToString ();
		}
	}

}

