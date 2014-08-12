using System;
using System.Collections.Generic;
using System.Linq;

using Whetstone;

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

		public static IEventSeriesProbabalisticClassifier<string> PerceptronCollectionClassifier(string criterion){
			IFeatureSynthesizer<string> synthesizer = new CompoundFeatureSynthesizer<string>(
				criterion,
				new IFeatureSynthesizer<string>[]{
					//string criterion, uint k, uint minKmerCount, uint kmersToTake, double smoothingAmt, bool useUncategorizedForBaseline
					//new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>(criterion, 2, 2, 8, .1, false),
					//new LatinLanguageFeatureSynthesizer(criterion),
					new VarKmerFrequencyFeatureSynthesizer<string>(criterion, 2, 2, 32, 1, false)
				}
			);


			IProbabalisticClassifier classifier = new PerceptronCollection(32.0);
			//IProbabalisticClassifier classifier = new ProbabalisticKNN(5, ProbabalisticKNN.WEIGHT_INVERSE_DISTANCE_SQUARED);

			IEventSeriesProbabalisticClassifier<string> eventSeriesClassifier = new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(synthesizer, classifier);

			return eventSeriesClassifier;
		}

		public static IEventSeriesProbabalisticClassifier<string> TextClassifier(string criterionByWhichToClassify, string[] availableCriteria){
			IFeatureSynthesizer<string> synthesizer = new CompoundFeatureSynthesizer<string>(
				criterionByWhichToClassify,
				
				new IFeatureSynthesizer<string>[]{
					new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>(criterionByWhichToClassify, 2, 2, 8, .1, false),
					new LatinLanguageFeatureSynthesizer(criterionByWhichToClassify),
				}.Concat(availableCriteria.Select (criterion =>
			        new VarKmerFrequencyFeatureSynthesizer<string>(criterion, 2, 2, 32, 1, false))).ToArray ()

			);

			IProbabalisticClassifier classifier = new EnsembleProbabalisticClassifier(
				new IProbabalisticClassifier[]{
					new PerceptronCollection(4.0),
					new ZScoreNormalizer(new ProbabalisticKnn(3, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_ALL_DATA))
				}
			);

			IEventSeriesProbabalisticClassifier<string> eventSeriesClassifier = new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(synthesizer, classifier);
			return eventSeriesClassifier;
		}

		public static IEventSeriesProbabalisticClassifier<string> NewspaperTextClassifier(){
			IFeatureSynthesizer<string> synthesizer = new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 50, 0.1, false);
			IProbabalisticClassifier classifier = new NullProbabalisticClassifier();

			return new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(synthesizer, classifier);
		}





		public static IEnumerable<Tuple<string, IEventSeriesProbabalisticClassifier<string>>> RegionsTestClassifiers(){
			
			IEventSeriesProbabalisticClassifier<string> synthesizerClassifier = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
			        new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					new NullProbabalisticClassifier()
				);

			IEventSeriesProbabalisticClassifier<string> doublePowerSynthesizerClassifier = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 100, 2.0, false),
					new NullProbabalisticClassifier()
				);

			IEventSeriesProbabalisticClassifier<string> perceptronBasedClassifier = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>(
						"region",
						
						new IFeatureSynthesizer<string>[]{
							new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
							new LatinLanguageFeatureSynthesizer("region"),
					        new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					        new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 4, 50, 2.0, false),
						}
					),
					new PerceptronCollection(8.0)
				);
			
			IEventSeriesProbabalisticClassifier<string> evenKnnBasedClassifier = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>(
						"region",
						
						new IFeatureSynthesizer<string>[]{
							new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
							new LatinLanguageFeatureSynthesizer("region"),
					        new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					        new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 4, 50, 2.0, false),
						}
					),
					new ProbabalisticKnn(3, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_ALL_DATA)
				);

			IEventSeriesProbabalisticClassifier<string> allKnnBasedClassifier = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>(
						"region",
						
						new IFeatureSynthesizer<string>[]{
							new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
							new LatinLanguageFeatureSynthesizer("region"),
					        new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					        new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 4, 50, 2.0, false),
						}
					),
					new ProbabalisticKnn(5, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_EVEN_CLASS_SIZES)
				);

			IEventSeriesProbabalisticClassifier<string> normalizedKnnBasedClassifier = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>(
						"region",
						
						new IFeatureSynthesizer<string>[]{
							new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
							new LatinLanguageFeatureSynthesizer("region"),
					        new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					        new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 4, 50, 2.0, false),
						}
					),
					new ZScoreNormalizer(new ProbabalisticKnn(5, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_EVEN_CLASS_SIZES))
				);

			IEventSeriesProbabalisticClassifier<string> ensembleClassifier = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>(
						"region",
						
						new IFeatureSynthesizer<string>[]{
							new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
							new LatinLanguageFeatureSynthesizer("region"),
					        new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					        new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 4, 50, 2.0, false),
						}
					),
					new EnsembleProbabalisticClassifier(
						new IProbabalisticClassifier[]{
							//new ZScoreNormalizer(new ProbabalisticKnn(3, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_EVEN_CLASS_SIZES)),
							//new ZScoreNormalizer(new ProbabalisticKnn(5, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_ALL_DATA)),
							new ProbabalisticKnn(3, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_EVEN_CLASS_SIZES),
							new ProbabalisticKnn(5, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_ALL_DATA),
							new PerceptronCollection(8.0)
						}
					)
				);

			return "Region feature synthesizer based classifier;Double power region feature based classifier;Perceptron based classifier;Even Distribution KNN based classifier;All data KNN based classifier;Normalized KNN based classifier;Ensemble based classifier".Split (';').Zip (
				new[]{
					synthesizerClassifier, 
					doublePowerSynthesizerClassifier, 
					perceptronBasedClassifier, 
					evenKnnBasedClassifier, 
					allKnnBasedClassifier, 
					normalizedKnnBasedClassifier, 
					ensembleClassifier
				});
		}
	}
}

