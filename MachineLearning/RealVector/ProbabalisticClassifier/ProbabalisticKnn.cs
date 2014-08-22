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
	[AlgorithmNameAttribute("Probabalistic K Nearest Neighbors Classifier")]
	public class ProbabalisticKnn : IProbabalisticClassifier
	{
		[AlgorithmParameterAttribute("k", 0)]
		public int k;

		[AlgorithmParameterAttribute("training mode", 1)]
		public KnnTrainingMode trainingMode;

		[AlgorithmParameterAttribute("classification mode", 2)]
		public KnnClassificationMode classifyMode;


		private TupleStruct<int, double[]>[] values;

		[AlgorithmTrainingAttribute("samples", 0)]
		public IEnumerable<string> samples(){
			return values.Select (val => schema[val.Item1] + ": " + val.Item2.FoldToString(item => item.ToString ("G4")));
		}
		
		private string[] schema;

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

