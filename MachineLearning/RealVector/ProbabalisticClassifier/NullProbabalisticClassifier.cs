using System;

using System.Collections.Generic;

using System.Linq;

using Whetstone;

namespace TextCharacteristicLearner
{
	//More of an adapter than a classifier, this class simply spits out the input.
	//Used primarily to convert an IFeatureSynthesizer's output to that of a Probabalistic classifier to it an IEventSeriesClassifier. 
	public class NullProbabalisticClassifier : IProbabalisticClassifier
	{
		public NullProbabalisticClassifier ()
		{
		}

		string[] classes;
		public string[] GetClasses(){
			return classes;
		}
		public void Train(IEnumerable<LabeledInstance> trainingData){
			classes = trainingData.Select(item => item.label).Distinct ().Order().ToArray();
		}

		public double[] Classify(double[] values){
			//TODO: Make safety assertion, sizes need to be equal.
			return values.NormalizeSumInPlace();
		}
	}
}

