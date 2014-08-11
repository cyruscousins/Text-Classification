using System;

using System.Collections.Generic;
using System.Linq;


using Whetstone;

namespace TextCharacteristicLearner
{
	//This interface represents a feature synthesizer that operates on an Event Series.

	public interface IFeatureSynthesizer<Ty>
	{
		//Data Accessors:

		string ClassificationCriterion{get;}

		//Get the names of the features being synthesized.
		string[] GetFeatureSchema();

		//Construction:

		bool NeedsTraining{get;}

		//Train an IFeatureSynthesizer model.
		//This function shall be called before SynthesizeFeatures iff NeedsTraining
		void Train(DiscreteSeriesDatabase<Ty> data);

		//Calculation:

		//Synthesize features for an item.
		//TODO: Enforce contract
		double[] SynthesizeFeatures(DiscreteEventSeries<Ty> item);

	}

	public static class FeatureSynthesizerExtensions{
		public static string SynthesizeLabelFeature<Ty>(this IFeatureSynthesizer<Ty> synth, DiscreteEventSeries<Ty> item){
			return synth.GetFeatureSchema()[synth.SynthesizeFeatures (item).MaxIndex()];
		}
		public static double[] SynthesizeFeaturesSumToOne<Ty>(this IFeatureSynthesizer<Ty> synth, DiscreteEventSeries<Ty> item){
			double[] vals = synth.SynthesizeFeatures(item).NormalizeSumInPlace();
			//It can happen that all are 0, in which case NaN results.
			if(Double.IsNaN (vals[0])){
				//TODO Higher order function for this!
				for(int i = 0; i < vals.Length; i++){
					vals[i] = 1.0 / vals.Length;
				}
			}
			return vals;
		}

		//Score a model.  Value returned on [0, 1], where 1 represents a perfectly accurate model and 0 a completely inaccurate model.
		public static double ScoreModel<Ty> (this IFeatureSynthesizer<Ty> synth, DiscreteSeriesDatabase<Ty> testData)
		{
			return synth.ScoreModel (testData, 1);
		}
		public static double ScoreModel<Ty> (this IFeatureSynthesizer<Ty> synth, DiscreteSeriesDatabase<Ty> testData, int verbosity, string nameCategory = null)
		{
			Dictionary<string, int> classRanks = synth.GetFeatureSchema ().Select ((item, index) => new Tuple<string, int> (item, index)).ToDictionary (a => a.Item1, a => a.Item2);

			//Display schema
			if (verbosity >= 2) {
				Console.WriteLine (synth.GetFeatureSchema ().FoldToString ());
			}

			double score = testData.data.AsParallel()
				.Where (item => classRanks.ContainsKey(item.labels.GetWithDefault (synth.ClassificationCriterion, ""))) //Filter for items for which we have regressors for.
				.Select (i => ScoreModelSingle(synth, classRanks, i, verbosity, nameCategory)).Average (); //Score them and take the average.

			if (verbosity >= 2) {
				Console.WriteLine ("Total Score = " + score);
				Console.WriteLine ("E[random model score] = " + (1.0 / classRanks.Count));
			}

			return score;
		}

		//Helper, score a model on a single element
		private static double ScoreModelSingle<Ty>(this IFeatureSynthesizer<Ty> synth, Dictionary<string, int> classRanks, DiscreteEventSeries<Ty> item, int verbosity, string nameCategory = null){
			int correctClass;
			if (!classRanks.TryGetValue (item.labels [synth.ClassificationCriterion], out correctClass)) {
				if (verbosity >= 1)
					Console.WriteLine ("Classifier does not contain data for " + item.labels [synth.ClassificationCriterion] + ".  Skipping this item.");
				return -1;
			}

			double[] scores = synth.SynthesizeFeaturesSumToOne (item);

			if (verbosity >= 2) {
				string toPrint;
				if(nameCategory != null){
					toPrint = item.labels[nameCategory] + " (" + item.labels [synth.ClassificationCriterion] + ")";
				}
				else{
					toPrint = item.labels [synth.ClassificationCriterion];
				}
				toPrint += ": " + scores.FoldToString () + " (" + scores [correctClass] + ")";
				Console.WriteLine (toPrint);
			}

			return scores [correctClass];
		}

		public static double ScoreModelType<Ty>(IEnumerable<string> categoryLabels, Func<string, IFeatureSynthesizer<Ty>> modelGenerator, DiscreteSeriesDatabase<Ty> trainingData, DiscreteSeriesDatabase<Ty> testData){
			double sumScore = 0;
			int count = 0;

			foreach(string categoryLabel in categoryLabels){
				//Train a model for this category label.
				IFeatureSynthesizer<Ty> model = modelGenerator(categoryLabel);
				model.Train (trainingData);
				sumScore += model.ScoreModel(testData);
			}

			return sumScore / count;
		}
	}
}

