using System;

using System.Collections.Generic;

using System.Linq;

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
			classifier.Train (series.AsParallel().AsOrdered()
				.Where (item => item.labels.ContainsKey (synthesizer.ClassificationCriterion))
			    .Select (item => new LabeledInstance(item.labels[synthesizer.ClassificationCriterion], synthesizer.SynthesizeFeatures(item)))
			);
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

