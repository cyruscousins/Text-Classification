using System;

using System.Collections.Generic;

using System.Linq;

using Whetstone;

namespace TextCharacteristicLearner
{
	public enum KnnClassificationMode{
		WEIGHT_EVEN,
		WEIGHT_INVERSE_DISTANCE,
		WEIGHT_INVERSE_DISTANCE_SQUARED
	}

	public enum KnnTrainingMode{
		TRAIN_ALL_DATA,
		TRAIN_EVEN_CLASS_SIZES
	}

	//TODO: This is extremely slow.  Optimize and use smarter code..
	public class ProbabalisticKnn : IProbabalisticClassifier
	{
		int k;

		KnnTrainingMode trainingMode;
		KnnClassificationMode classifyMode;

		private string[] schema;

		private TupleStruct<int, double[]>[] values;

		public ProbabalisticKnn (int k, KnnClassificationMode classificationMode = KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED, KnnTrainingMode trainingMode = KnnTrainingMode.TRAIN_EVEN_CLASS_SIZES)
		{
			this.k = k;
			this.classifyMode = classificationMode;
			this.trainingMode = trainingMode;
		}

		public string[] GetClasses(){
			return schema;
		}
		
		public void Train(IEnumerable<LabeledInstance> instances){
			
			if(!(instances is List<LabeledInstance>)){
				instances.ToArray (); //Prevent expensive multienumerations by collapsing if necessary.
			}

			schema = instances.Select(instance => instance.label).Distinct().Order ().ToArray ();
			Dictionary<string, int> mapping = schema.IndexLookupDictionary();

			switch(trainingMode){
				case KnnTrainingMode.TRAIN_ALL_DATA:
					values = instances.Select (instance => new TupleStruct<int, double[]>(mapping[instance.label], instance.values)).ToArray ();
					break;
				case KnnTrainingMode.TRAIN_EVEN_CLASS_SIZES:
					int size = instances.GroupBy (instance => instance.label).Select (grp => grp.Count ()).Min ();
					//TODO: This is slow, unnecessary double enumeration.
					values = instances.GroupBy (instance => instance.label).SelectMany (grp => grp.Take (size).Select (item => new TupleStruct<int, double[]>(mapping[item.label], item.values))).ToArray ();
					break;
			}

		}

		//TODO contract on classify output.
		public double[] Classify(double[] instance){
			//Find k nearest neighbors
			IEnumerable<TupleStruct<int, double>> kNearest =  values.Select(item => new TupleStruct<int, double>(item.Item1, instance.DistanceSquared(item.Item2)));

			double[] ret = new double[schema.Length];
			switch(classifyMode){
				case KnnClassificationMode.WEIGHT_EVEN:
					foreach(TupleStruct<int, double> neighbor in kNearest){
						ret[neighbor.Item1]+= 1.0 / k;
					}
					break;
				case KnnClassificationMode.WEIGHT_INVERSE_DISTANCE:
				{
					double sumWeight = 0;
					foreach(TupleStruct<int, double> neighbor in kNearest){
						double weight = 1.0 / neighbor.Item2.Sqrt ();
						sumWeight += weight;
						ret[neighbor.Item1] += weight;
					}
					ret.MapInPlace(val => val * (1.0 / sumWeight));
					break;
				}
				case KnnClassificationMode.WEIGHT_INVERSE_DISTANCE_SQUARED:
				{
					double sumWeight = 0;
					foreach(TupleStruct<int, double> neighbor in kNearest){
						double weight = 1.0 / neighbor.Item2;
						sumWeight += weight;
						ret[neighbor.Item1] += weight;
					}
					ret.MapInPlace(val => val * (1.0 / sumWeight));
					break;
				}
			}

			//Console.WriteLine ("Result: " + ret.FoldToString ());
			return ret;
		}

		public override string ToString ()
		{
			return "{Probabalistic KNN " + "k = " + k + ",  mode = " + classifyMode + "\n" + 
				values.Length + " values." + //TODO: Distribution of values.  We want to see the breakdown of types represented
				"}";
		}
	}
}

