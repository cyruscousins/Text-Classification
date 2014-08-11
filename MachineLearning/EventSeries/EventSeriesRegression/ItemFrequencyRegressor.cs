using System;

using System.Collections.Generic;

using System.Linq;

using Whetstone;


using System.Diagnostics.Contracts;

namespace TextCharacteristicLearner
{
	public class ItemFrequencyRegressor<A> : IEventSeriesScalarRegressor<A>
	{
		
		public string name;

		public string label{get {return name;}}

		//Model Parameters:
		public uint minSignificantCount{get; private set;}
		public uint smoothingAmount{get; private set;}
		public uint featuresToUse{get; private set;}

		//Derived from Model Data (Train function)
		protected Dictionary<A, double> characteristics;
		public uint trainingDataSize;

		//Constructor sets model parameters
		public ItemFrequencyRegressor (string name, uint minSignificantCount, uint smoothingAmount, uint featuresToUse)
		{
			this.name = name;
			this.minSignificantCount = minSignificantCount;
			this.smoothingAmount = smoothingAmount;
			this.featuresToUse = featuresToUse;

			characteristics = new Dictionary<A, double>((int)featuresToUse); //Consider bumping this to FinalizeModel.
		}

		//Constructor with build in training.
		public ItemFrequencyRegressor(string name, uint minSignificantCount, uint smoothingAmount, uint featuresToUse, Multiset<A> baselineClass, Multiset<A> thisClass) : this(name, minSignificantCount, smoothingAmount, featuresToUse){
			TrainModelRatios(baselineClass, thisClass);
		}

		//TRAINING:

		//One of the Train functions is called to train a raw model, which is trimmed down to size with the finalizeModel() function.

		public void TrainModelSubtractive(Multiset<A> baselineClass, Multiset<A> thisClass){
			List<KeyValuePair<A, double>> rawModel = new List<KeyValuePair<A, double>>();

			uint totalCount = 0;
			foreach(A key in thisClass.Keys){
				uint thisCount = thisClass.getCount(key);
				totalCount += thisCount;
				if(thisCount > minSignificantCount){
					double thisFrac = thisClass.GetKeyFrac(key);
					double baseFrac = baselineClass.GetKeyFracLaplace(key, smoothingAmount);
					if(thisFrac > baseFrac){
						rawModel.Add (key, thisFrac - baseFrac);
					}
				}
			}

			finalizeModel (rawModel, totalCount);
		}

		public void TrainModelRatios(Multiset<A> baselineClass, Multiset<A> thisClass){
			List<KeyValuePair<A, double>> rawModel = new List<KeyValuePair<A, double>>();

			uint totalCount = 0;
			foreach(A key in thisClass.Keys){
				uint thisCount = thisClass.getCount(key);
				totalCount += thisCount;
				if(thisCount > minSignificantCount){
					double thisFrac = thisClass.GetKeyFrac(key);
					double baseFrac = baselineClass.GetKeyFracLaplace (key, smoothingAmount);
					if(thisFrac > baseFrac){
						rawModel.Add (key, thisFrac / baseFrac);
					}
				}
			}

			finalizeModel (rawModel, totalCount);
		}

		public void finalizeModel(IEnumerable<KeyValuePair<A, double>> rawModel, uint rawCount){
			double scale = 1 + Math.Log10 (rawCount);
			rawModel.OrderByDescending (pair => pair.Value).Take ((int)featuresToUse).ForEach (a => characteristics.Add(a.Key, a.Value * scale)); //TODO replace with TakeTopNUnordered
			

			//TODO scaling settings.
			//rawModel.OrderByDescending (pair => pair.Value).Take ((int)featuresToUse).ForEach (a => characteristics.Add(a.Key, a.Value)); //TODO replace with TakeTopNUnordered
			trainingDataSize = rawCount;
		}

		//Regression

		public double RegressEventSeries(DiscreteEventSeries<A> series){
			//It's probably faster to convert the series to a multiset before scoring, particularly when many things are repeated.
			//So we do.
			return RegressEventSeries(series.ToMultiset ());
		}

		public double RegressEventSeries(Multiset<A> series){
			double d;
			Contract.Ensures (Contract.ValueAtReturn<double>(out d) > 0);

			double totalValue = 0;
			series.ForEach (kvp => totalValue += characteristics.GetWithDefault (kvp.Key) * kvp.Value);

			return totalValue;
		}


		//////////
		//Printing

		public override string ToString(){
			return ToString (Int32.MaxValue);
		}
		public string ToString(int count){
			//return name + ":" + Keys.OrderByDescending (a => this[a]).Take(count).FoldToString (key => key + ":" + this[key].ToString("e2"));
			return name + ":" +
				"{" + String.Format ("minSignificantCount = {0}, smoothingAmount = {1}, featuresToUse = {2}, trainingDataSize = {3}", minSignificantCount, smoothingAmount, featuresToUse, trainingDataSize) +
				"}" +
				characteristics.OrderByDescending (a => a.Value).Take(count).FoldToString (kvp => kvp.Key + ":" + kvp.Value);
		}

	}

	//TODO: This, and something similar for tuples, belongs in Whetstone.
	public static class KVPExtensions{
		public static void Add<A, B>(this List<KeyValuePair<A, B>> e, A a, B b){
			e.Add (new KeyValuePair<A, B>(a, b));
		}
	}
}

