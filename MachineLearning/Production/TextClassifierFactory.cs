using System;

namespace TextCharacteristicLearner
{
	public static class TextClassifierFactory
	{
		public static IEventSeriesProbabalisticClassifier<string> EnglishTextClassifier(string criterion){
			IFeatureSynthesizer<string> synthesizer = new CompoundFeatureSynthesizer<string>(
				criterion,
				new IFeatureSynthesizer<string> []{
					new VarKmerFrequencyFeatureSynthesizer<string>(criterion, 3, 2, 50, 0.1, false),
					new TextFeatureSynthesizer(criterion)
					//TODO: Classification accross additional criteria.
				}
			);

			//TODO probabalistic Decision Tree.


			return null;
			//return new IEventSeriesProbabalisticClassifier(synthesizer)
		}

		public static IEventSeriesProbabalisticClassifier<string> perceptronCollectionClassifier(string criterion){
			IFeatureSynthesizer<string> synthesizer = new CompoundFeatureSynthesizer<string>(
				criterion,
				new IFeatureSynthesizer<string>[]{
					//string criterion, uint k, uint minKmerCount, uint kmersToTake, double smoothingAmt, bool useUncategorizedForBaseline
					new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>(criterion, 2, 2, 64, .1, false),
					new LatinLanguageFeatureSynthesizer(criterion)
				}
			);

			IProbabalisticClassifier classifier = new PerceptronCollection(3.0);

			IEventSeriesProbabalisticClassifier<string> eventSeriesClassifier = new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(synthesizer, classifier);
			return eventSeriesClassifier;
		}

		public static IEventSeriesProbabalisticClassifier<string> NewspaperTextClassifier(){
			IFeatureSynthesizer<string> synthesizer = new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 50, 0.1, false);
			IProbabalisticClassifier classifier = new NullProbabalisticClassifier();

			return new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(synthesizer, classifier);
		}
	}
}

