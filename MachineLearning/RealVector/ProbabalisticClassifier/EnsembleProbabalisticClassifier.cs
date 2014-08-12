using System;

using System.Collections.Generic;

using System.Linq;
using Whetstone;

namespace TextCharacteristicLearner
{
	public class EnsembleProbabalisticClassifier : IProbabalisticClassifier
	{
		IProbabalisticClassifier[] classifiers;

		//TODO, weights, trainings, ...

		public EnsembleProbabalisticClassifier (IProbabalisticClassifier[] classifier)
		{
			this.classifiers = classifier;
		}





		string[] classes;
		public string[] GetClasses(){
			return classes;
		}
		
		public void Train(IEnumerable<LabeledInstance> instances){
			if(!(instances is IList<LabeledInstance>)){
				instances.ToArray (); //Prevent expensive multienumerations by collapsing if necessary.
			}
			classes = instances.Select(instance => instance.label).Distinct().Order ().ToArray ();

			foreach(IProbabalisticClassifier classifier in classifiers){
				classifier.Train (instances);
			}

			//TODO: Weight training.
		}

		public double[] Classify(double[] instance){
			IEnumerable<double[]> results = classifiers.Select(classifier => classifier.Classify(instance));
			return results.VectorMean().ToArray();
		}

		public override string ToString ()
		{
			return "{Ensemble Probabalistic Classifier [Training information unavailable]" + "\n" +
				"Classifiers: \n" + classifiers.FoldToString () +
				"\n}";
		}
	}
}

