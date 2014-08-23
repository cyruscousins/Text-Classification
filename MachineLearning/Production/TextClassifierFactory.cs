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


			IProbabalisticClassifier classifier = new PerceptronCloud(32.0);
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
					new PerceptronCloud(4.0),
					new ZScoreNormalizerClassifierWrapper(new ProbabalisticKnn(3, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_ALL_DATA))
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



		public static IEnumerable<Tuple<string, IEventSeriesProbabalisticClassifier<string>>> RegionsPerceptronTestClassifiers(){
			double factor = 8.0;
			return EnumeratePerceptrons(factor).Select (item => new Tuple<string, IEventSeriesProbabalisticClassifier<string>>(
				item.Item1, 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					item.Item2
				)
			)).Concat (
				new []{
					new Tuple<string, IEventSeriesProbabalisticClassifier<string>>("Perceptron with Extended Information, negatives, even weights.",
						new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
							new CompoundFeatureSynthesizer<string>(
								"region",
								new IFeatureSynthesizer<string>[]{
									new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
									new LatinLanguageFeatureSynthesizer("region"),
									new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
									new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 3, 50, 2.0, false)
								}
							),
							new PerceptronCloud(factor, PerceptronTrainingMode.TRAIN_EVEN_WEIGHTS, PerceptronClassificationMode.USE_NEGATIVES)
						)
				    )
				});
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
					new PerceptronCloud(16.0, PerceptronTrainingMode.TRAIN_EVEN_WEIGHTS, PerceptronClassificationMode.USE_NEGATIVES)
				);
			
			IEventSeriesProbabalisticClassifier<string> evenKnnBasedClassifier = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>(
						"region",
						
						new IFeatureSynthesizer<string>[]{
							//new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
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
							//new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
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
							//new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
							new LatinLanguageFeatureSynthesizer("region"),
					        new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					        new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 4, 50, 2.0, false),
						}
					),
					new ZScoreNormalizerClassifierWrapper(new ProbabalisticKnn(5, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_EVEN_CLASS_SIZES))
				);

			IEventSeriesProbabalisticClassifier<string> normalizedKnnBasedClassifier2 = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>(
						"region",
						
						new IFeatureSynthesizer<string>[]{
							//new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
							new LatinLanguageFeatureSynthesizer("region"),
					        new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					        new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 4, 50, 2.0, false),
						}
					),
					new ZScoreNormalizerClassifierWrapper(new ProbabalisticKnn(5, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_ALL_DATA))
				);

			IEventSeriesProbabalisticClassifier<string> normalizedKnnBasedClassifier3 = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>(
						"region",
						
						new IFeatureSynthesizer<string>[]{
							//new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
							//new LatinLanguageFeatureSynthesizer("region"),
					        new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					        new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 4, 50, 2.0, false),
						}
					),
					new ZScoreNormalizerClassifierWrapper(new ProbabalisticKnn(5, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_EVEN_CLASS_SIZES))
				);

			IEventSeriesProbabalisticClassifier<string> normalizedKnnBasedClassifier4 = 
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>(
						"region",
						
						new IFeatureSynthesizer<string>[]{
							//new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 8, .1, false),
							//new LatinLanguageFeatureSynthesizer("region"),
					        new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					        new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 4, 50, 2.0, false),
						}
					),
					new ZScoreNormalizerClassifierWrapper(new ProbabalisticKnn(5, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_ALL_DATA))
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
							new ProbabalisticKnn(5, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE, KnnTrainingMode.TRAIN_ALL_DATA),
							new PerceptronCloud(4.0),
							new PerceptronCloud(4.0, classificationMode: PerceptronClassificationMode.USE_NEGATIVES),
							new PerceptronCloud(4.0, classificationMode: PerceptronClassificationMode.USE_SCORES),
							new PerceptronCloud(4.0, classificationMode: PerceptronClassificationMode.USE_SCORES | PerceptronClassificationMode.USE_NEGATIVES)
						}
					)
				);

			return new[]{
					"region feature synthesizer based classifier",
					"double power region feature based classifier",
					"perceptron based classifier",
					"even distribution KNN based classifier",
					"all data KNN based classifier",
					"normalized KNN based classifier even",
					"normalized KNN based classifier all",
					"normalized KNN based classifier even (fewer features)",
					"normalized KNN based classifier all (fewer features)",
					"ensemble based classifier"
				}.Zip (
				new[]{
					synthesizerClassifier, 
					doublePowerSynthesizerClassifier, 
					perceptronBasedClassifier, 
					evenKnnBasedClassifier, 
					allKnnBasedClassifier, 
					normalizedKnnBasedClassifier, 
					normalizedKnnBasedClassifier2, 
					normalizedKnnBasedClassifier3, 
					normalizedKnnBasedClassifier4, 
					ensembleClassifier
				});
		}

		public static IEnumerable<Tuple<string, IEventSeriesProbabalisticClassifier<string>>> NewsTestClassifiers(){
			IEventSeriesProbabalisticClassifier<string>[] classifiers = new []{
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 50, 0.1, false),
					new NullProbabalisticClassifier()
				),
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 40, 0.1, false),
					new NullProbabalisticClassifier()
				),
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 60, 0.1, false),
					new NullProbabalisticClassifier()
				),
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 70, 0.1, false),
					new NullProbabalisticClassifier()
				),
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 50, 0.2, false),
					new NullProbabalisticClassifier()
				),
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("author", 4, 2, 50, 0.1, false),
					new NullProbabalisticClassifier()
				),
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 1, 50, 0.1, false),
					new NullProbabalisticClassifier()
				),
			};
			string[] names = new string[]{
				"baseline",
				"fewer features",
				"more features",
				"many more features",
				"more smoothing",
				"k=4",
				"no cutoff"
			};

			return names.Zip(classifiers);

		}

		public static IEnumerable<Tuple<string, IProbabalisticClassifier>> EnumeratePerceptrons(double oversamplingFactor){
			foreach(PerceptronTrainingMode trainingMode in PerceptronTrainingMode.GetValues(typeof(PerceptronTrainingMode))){
				foreach(PerceptronClassificationMode classificationMode in PerceptronClassificationMode.GetValues(typeof(PerceptronClassificationMode))){
					foreach(bool normalize in new[]{false}){
//					foreach(bool normalize in new[]{true, false}){
						foreach(double dist in new[]{0, 1, 2}){
							yield return new Tuple<string, IProbabalisticClassifier>(
								("Perceptron Cloud: t: " + trainingMode.ToString() + ", c: " + classificationMode.ToString() + ", " + "dist: " + dist + (normalize ? " (normalized out)" : "")).Replace ("_", " "),
								new PerceptronCloud(oversamplingFactor, trainingMode, classificationMode, dist, normalize));
						}
					}
				}
			}
		}
		
		public static IEnumerable<Tuple<string, IEventSeriesProbabalisticClassifier<string>>> NewsTestAdvancedClassifiers(){
			IEventSeriesProbabalisticClassifier<string>[] classifiers = new []{
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 50, 0.1, false),
					new NullProbabalisticClassifier()
				),
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 50, 0.1, false),
					new PerceptronCloud(8.0, PerceptronTrainingMode.TRAIN_ALL_DATA, PerceptronClassificationMode.USE_NEGATIVES | PerceptronClassificationMode.USE_SCORES)
				),
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>("author", new IFeatureSynthesizer<string>[]{
						new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 50, 0.1, false),
						new VarKmerFrequencyFeatureSynthesizer<string>("location", 3, 2, 50, 0.1, false),
						new DateValueFeatureSynthesizer("date"),
						new LatinLanguageFeatureSynthesizer("author")
					}),
					new PerceptronCloud(8.0, PerceptronTrainingMode.TRAIN_ALL_DATA, PerceptronClassificationMode.USE_NEGATIVES | PerceptronClassificationMode.USE_SCORES)
				),
				new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
					new CompoundFeatureSynthesizer<string>("author", new IFeatureSynthesizer<string>[]{
						new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 50, 0.1, false),
						new VarKmerFrequencyFeatureSynthesizer<string>("location", 3, 2, 50, 0.1, false),
						new DateValueFeatureSynthesizer("date"),
						new LatinLanguageFeatureSynthesizer("author")
					}),
					new EnsembleProbabalisticClassifier(
						new IProbabalisticClassifier[]{
							//new ZScoreNormalizer(new ProbabalisticKnn(3, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_EVEN_CLASS_SIZES)),
							//new ZScoreNormalizer(new ProbabalisticKnn(5, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_ALL_DATA)),
							//TODO:
							//new ProbabalisticKnn(3, KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode.TRAIN_EVEN_CLASS_SIZES),
							
							new PerceptronCloud(4.0, PerceptronTrainingMode.TRAIN_ALL_DATA, PerceptronClassificationMode.USE_NEGATIVES | PerceptronClassificationMode.USE_SCORES),
							new PerceptronCloud(4.0, PerceptronTrainingMode.TRAIN_EVEN_SIZE, PerceptronClassificationMode.NOFLAGS)
						}
					)
				),
			};
			string[] names = new string[]{
				"raw synth output",
				@"synth to perceptron cloud",
				"features to perceptron cloud",
				"ensemble"
			};

			return names.Zip(classifiers);

		}
	}
}

