using System;

using System.Collections.Generic;
using System.Linq;

namespace TextCharacteristicLearner
{
	public class LatinLanguageFeatureSynthesizer : IFeatureSynthesizer<string>
	{
		public LatinLanguageFeatureSynthesizer (string criterion)
		{
			ClassificationCriterion = criterion;
		}


		public string ClassificationCriterion{get; private set;} //TODO: null?

		public bool NeedsTraining{get{return false;} }

		public void Train(DiscreteSeriesDatabase<string> data){
			throw new Exception("Cannot train a LatinLanguageFeatureSynthesizer.");
		}

		string[] featureSchema = "Word Count;Mean Word Length;Mean Sentence Length".Split (';');
		public string[] GetFeatureSchema(){
			return featureSchema;
		}

		public static HashSet<string> stops = new HashSet<string>(){
			".", "!", "?", ";"
		};

		//Synthesize features for an item.
		public double[] SynthesizeFeatures(DiscreteEventSeries<string> item){
			//"Word Count;Mean Sentence Length;Orthographical Error Rate;Formality;Textspeak"

			return new[]{
				item.data.Length,
				item.data.Select (word => word.Length).Sum () / (double)item.data.Length,
				item.data.Where(word => stops.Contains(word)).Count() / (double)item.data.Length,
			};
		}
	}
}

