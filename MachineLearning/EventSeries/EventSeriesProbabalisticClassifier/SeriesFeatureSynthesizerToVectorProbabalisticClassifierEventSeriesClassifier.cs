using System;

using System.Collections.Generic;

using System.Linq;
using Whetstone;

namespace TextCharacteristicLearner
{

	//Sorry about the name.
	//This class does exactly what one might expect.  As an event series classifier, its job is to produce probabalistic classifications for generic event series.
	//It does this by running the series through a feature synthesizer to generate a feature vector, then running a proabalistic classifier on the feature vector.
	[AlgorithmNameAttribute("Series Feature Synthesizer to Vector Probabalistic Classifier")]
	public class SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<Ty> : IEventSeriesProbabalisticClassifier<Ty>
	{
		[AlgorithmParameterAttribute("Feature Synthesizer", 0)]
		public IFeatureSynthesizer<Ty> synthesizer;
		[AlgorithmParameterAttribute("Probabalistic Classifier", 1)]
		public IProbabalisticClassifier classifier;

		public SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier (IFeatureSynthesizer<Ty> synthesizer, IProbabalisticClassifier classifier)
		{
			this.synthesizer = synthesizer;
			this.classifier = classifier;
		}

		public string[] GetClasses(){
			return classifier.GetClasses();
		}
		public void Train(DiscreteSeriesDatabase<Ty> series){
			//TODO: Sharing this data like this may be detrimental.
			//TODO: Boolean for whether the synthesizer needs to be trained.
			synthesizer.Train (series);
			classifier.Train (series//.AsParallel().AsOrdered() //Parallel causes a bug where not all of the items are always reached.
				.Where (item => item.labels.ContainsKey (synthesizer.ClassificationCriterion))
			    .Select (item => new LabeledInstance(item.labels[synthesizer.ClassificationCriterion], synthesizer.SynthesizeFeatures(item))).ToArray() //But this seems to fix it?
			);

			//There is a bug, this code tests for a failure.


			if(classifier is NullProbabalisticClassifier){
				string[] synthFeatures = synthesizer.GetFeatureSchema();
				string[] classifierClasses = classifier.GetClasses();

				if(synthFeatures.Length != classifierClasses.Length  || !synthFeatures.Zip (classifierClasses, (s, c) => (s == c)).Conjunction() ){
					Console.WriteLine ("A catastrophic error has occured in the Null Probabalistic Classifier.  Feature schema:");
					Console.WriteLine (synthFeatures.FoldToString ());
					Console.WriteLine ("But classifier (NullProbabalisticClassifier):");
					Console.WriteLine (classifierClasses.FoldToString ());
					Console.WriteLine ("Training Names:");
					string[] trainingNames = series.Where (item => item.labels.ContainsKey (synthesizer.ClassificationCriterion)).Select(item => item.labels[synthesizer.ClassificationCriterion]).Distinct().Order().ToArray();
					Console.WriteLine (trainingNames.FoldToString());
					Console.WriteLine ("synthesizer, classifier, training: " + synthFeatures.Length + ", " + classifierClasses.Length + ", " + trainingNames.Length);
					Console.WriteLine ("All Training Data:");
					Console.WriteLine (series.FoldToString (item => item.labels.GetWithDefault (synthesizer.ClassificationCriterion, "[none]")));
					Console.Write ("");
				}
			}

		}
		
		public double[] Classify(DiscreteEventSeries<Ty> series){
			return classifier.Classify (synthesizer.SynthesizeFeatures(series));
		}

		public override string ToString(){
			return "{SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier: [Training data configuration not yet available]\n" +
				"Synthesizer = " + synthesizer.ToString () + "," +
				"Classifier = " + classifier.ToString () +
					"\n}";
		}
	}
}

