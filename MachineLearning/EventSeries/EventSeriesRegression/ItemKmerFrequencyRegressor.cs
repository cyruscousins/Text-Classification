using System;

using System.Collections.Generic;
using System.Linq;

using Whetstone;


namespace TextCharacteristicLearner
{
	public class ItemKmerFrequencyRegressor<A> : IEventSeriesScalarRegressor<A>
	{
		ItemFrequencyRegressor<Kmer<A>> regressor;
		uint k;
		public ItemKmerFrequencyRegressor (string name, uint minSignificantCount, uint smoothingAmount, uint featuresToUse, uint k)
		{
			regressor = new ItemFrequencyRegressor<Kmer<A>>(name, minSignificantCount, smoothingAmount, featuresToUse);
			this.k = k;
		}
		
		public ItemKmerFrequencyRegressor (string name, uint minSignificantCount, uint smoothingAmount, uint featuresToUse, uint k, Multiset<Kmer<A>> baselineClass, Multiset<Kmer<A>> thisClass) : this(name, minSignificantCount, smoothingAmount, featuresToUse, k)
		{
			TrainModelRatios (baselineClass, thisClass);
		}

		
		public string label{get {return regressor.name;}}

		
		//TRAINING:

		//One of the Train functions is called to train a raw model, which is trimmed down to size with the finalizeModel() function.

		public void TrainModelSubtractive(Multiset<Kmer<A>> baselineClass, Multiset<Kmer<A>> thisClass){
			regressor.TrainModelSubtractive(baselineClass, thisClass);
		}

		public void TrainModelRatios(Multiset<Kmer<A>> baselineClass, Multiset<Kmer<A>> thisClass){
			regressor.TrainModelRatios(baselineClass, thisClass);
		}

		//Regression.
		public double RegressEventSeries(DiscreteEventSeries<A> series){

			//It's probably faster to convert the series to a multiset before scoring, particularly when many things are repeated.
			Multiset<Kmer<A>> seriesMultiset = series.ToMultisetKmer(k);
			return regressor.RegressEventSeries(seriesMultiset);
		}
	}
}

