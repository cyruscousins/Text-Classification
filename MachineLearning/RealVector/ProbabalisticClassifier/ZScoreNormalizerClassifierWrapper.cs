using System;

using System.Collections.Generic;
using System.Linq;
using Whetstone;

namespace TextCharacteristicLearner
{
	//While certainly not a classifier on its own, the ZScoreNormalizer serves as an adapter, being connected immediately to a classifier, and thus fulfils the contracts.
	[AlgorithmNameAttribute("z-score normalizer")]
	public class ZScoreNormalizerClassifierWrapper : IProbabalisticClassifier
	{
		[AlgorithmParameterAttribute("classifier", 0)]
		public IProbabalisticClassifier classifier;
		
		[AlgorithmTrainingAttribute("standard deviations vector", 1)]
		public double[] stdevs;
		
		[AlgorithmTrainingAttribute("means vector", 1)]
		public double[] means;

		public ZScoreNormalizerClassifierWrapper (IProbabalisticClassifier classifier)
		{
			this.classifier = classifier;
		}

		public void Train(IEnumerable<LabeledInstance> data){
			double[,] transpose = data.Select(instance => instance.values).Transpose();

			int count = transpose.GetUpperBound (1) + 1;

			double[] means = transpose.EnumerateRows().Select (row => row.Average()).ToArray();
			double[] stdevs = transpose.EnumerateRows().Select ((row, index) => row.Stdev(means[index])).ToArray ();

			stdevs.MapInPlace (item => item = (item == 0 || Double.IsNaN(item)) ? 1 : item);

			this.stdevs = stdevs;
			this.means = means;

			classifier.Train (data);
		}

		public string[] GetClasses(){
			return classifier.GetClasses ();
		}

		public double[] Classify(double[] vals){
			//:'( Copying memory.

			return classifier.Classify (applyNormalization(vals));
		}

		public double[] applyNormalization(double[] vals){
			double[] transformed = new double[vals.Length];
			for(int i = 0; i < vals.Length; i++){
				transformed[i] = (vals[i] - means[i]) / stdevs[i];
			}
			return transformed;
		}

		public double[] applyInverseNormalization(double[] vals){
			double[] transformed = new double[vals.Length];
			for(int i = 0; i < vals.Length; i++){
				transformed[i] = (vals[i] * stdevs[i]) + means[i];
			}
			return transformed;
		}

		public override string ToString(){
			return "{Z Score Normalizer\n" +
				"means: " + means.FoldToString() + "\n" +
				"standard deviations: " + stdevs.FoldToString () + "\n" +
				"Inner Classifier: " + classifier.ToString () +
				"}";
		}


	}
}

