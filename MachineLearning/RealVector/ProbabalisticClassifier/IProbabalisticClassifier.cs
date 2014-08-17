using System;

using System.Collections.Generic;

namespace TextCharacteristicLearner
{
	public struct LabeledInstance{
		public string label;
		public double[] values;

		public LabeledInstance(string label, double[] values){
			this.label = label;
			this.values = values;
		}
	}

	public interface IProbabalisticClassifier
	{
		string[] GetClasses();
		void Train(IEnumerable<LabeledInstance> trainingData); //TODO: Prepend string[] classes to arguments, refactor.

		double[] Classify(double[] values);
	}

	public static class ProbabalisticClassifierExtensions{
		static double[] Classify(this IProbabalisticClassifier classifier, LabeledInstance instance){
			return classifier.Classify (instance.values);
		}
	}
}

