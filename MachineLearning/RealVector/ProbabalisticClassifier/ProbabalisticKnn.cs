using System;

using System.Collections.Generic;

using System.Linq;

using Whetstone;

namespace TextCharacteristicLearner
{
	//TODO: This is extremely slow.  Optimize and use a better algorithm.
	public class ProbabalisticKNN : IProbabalisticClassifier
	{
		public const int WEIGHT_EVEN = 0, WEIGHT_DISTANCE = 1, WEIGHT_DISTANCE_SQUARED = 2;
		int k, mode;

		private string[] schema;

		private TupleStruct<int, double[]>[] values;

		public ProbabalisticKNN (int k, int mode = WEIGHT_DISTANCE_SQUARED)
		{
			this.k = k;
			this.mode = mode;
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

			values = instances.Select (instance => new TupleStruct<int, double[]>(mapping[instance.label], instance.values)).ToArray ();
		}

		public double[] Classify(double[] instance){
			//Find k nearest neighbors
			IEnumerable<TupleStruct<int, double>> kNearest =  values.Select(item => new TupleStruct<int, double>(item.Item1, instance.DistanceSquared(item.Item2)));

			double[] ret = new double[schema.Length];
			switch(mode){
				case WEIGHT_EVEN:
					foreach(TupleStruct<int, double> neighbor in kNearest){
						ret[neighbor.Item1]+= 1.0 / k;
					}
					break;
				case WEIGHT_DISTANCE:
				{
					double sumDist = 0;
					foreach(TupleStruct<int, double> neighbor in kNearest){
						double dist = neighbor.Item2.Sqrt ();
						sumDist += dist;
						ret[neighbor.Item1] += 1.0;
					}
					ret.MapInPlace(val => 1.0 / sumDist);
					break;
				}
				case WEIGHT_DISTANCE_SQUARED:
				{
					double sumDist = 0;
					foreach(TupleStruct<int, double> neighbor in kNearest){
						double dist = neighbor.Item2;
						sumDist += dist;
						ret[neighbor.Item1]+= 1.0 / k;
					}
					ret.MapInPlace(val => 1.0 / sumDist);
					break;
				}
			}

			return ret;
		}
	}
}

