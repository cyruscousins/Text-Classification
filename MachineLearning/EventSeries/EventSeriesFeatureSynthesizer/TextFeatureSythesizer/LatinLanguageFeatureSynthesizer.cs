using System;

using System.Collections.Generic;
using System.Linq;

using Whetstone;

namespace TextCharacteristicLearner
{
	[AlgorithmNameAttribute("Latin language feature synthesizer")]
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

		string[] featureSchema = "Word Count;Mean Word Length;Stdev Word Length;Mean Sentence Length".Split (';');
		public string[] GetFeatureSchema(){
			return featureSchema;
		}

		public static HashSet<string> stops = new HashSet<string>(){
			".", "!", "?", ";"
		};

		//Synthesize features for an item.
		public double[] SynthesizeFeatures(DiscreteEventSeries<string> item){

			double wordCount = item.data.Length;
			double meanWordLength = item.data.Select (word => word.Length).Average();
			double stdevWordLength = item.data.Select (word => (double)word.Length).Stdev(meanWordLength);
			double meanSentenceLength = item.data.Length / (double)item.data.Where(word => stops.Contains(word)).Count(); //TODO: Stdev sentence length would be nice.

			return new[]{
				wordCount,
				meanWordLength,
				stdevWordLength,
				meanSentenceLength
			};
		}
	}
}

