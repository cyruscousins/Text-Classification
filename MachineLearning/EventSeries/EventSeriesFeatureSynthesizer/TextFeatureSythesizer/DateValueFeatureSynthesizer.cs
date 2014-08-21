using System;
using System.Linq;

namespace TextCharacteristicLearner
{
	//TODO: Deprecate this class?

	public class DateValueFeatureSynthesizer : IFeatureSynthesizer<string>
	{
		public DateValueFeatureSynthesizer (string criterion)
		{
			ClassificationCriterion = criterion;
		}


		public string ClassificationCriterion{get; private set;} //TODO: null?

		public bool NeedsTraining{get{return false;} }

		public void Train(DiscreteSeriesDatabase<string> data){
			throw new Exception("Cannot train a DateValueFeatureSynthesizer.");
		}

		public string[] GetFeatureSchema(){
			return new[]{"date (from \"" + ClassificationCriterion + "\")"};
		}

		//Synthesize features for an item.
		public double[] SynthesizeFeatures(DiscreteEventSeries<string> item){
			//"Word Count;Mean Sentence Length;Orthographical Error Rate;Formality;Textspeak"
			string date;
			if(item.labels.TryGetValue(ClassificationCriterion, out date)){
				try{
					int[] split = date.Split('-').Select(term => Int32.Parse (term)).ToArray();
					return new[]{new DateTime(split[0], split[1], split[2]).Ticks / (10000000.0 * 60 * 60 * 24)}; //ticks are 100 ns.  This converts to days.
				}
				catch(Exception e){
					//TODO: respond to this error.
				}
			}
			return new[]{0.0}; //TODO: NaN?  Other no information representation?
		}
	}
}

