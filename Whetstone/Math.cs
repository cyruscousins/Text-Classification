using System;

using System.Collections.Generic;

using System.Linq;


namespace Whetstone
{
	public static class WhetstoneMath
	{
		//Basic math operations
		public static double Sqrt(this double a){
			return Math.Sqrt (a);
		}
	}

	public static class WhetstoneArrayMath{

		//Assumes a, b are equidimensional.
		public static double DistanceSquared (this double[] array1, double[] array2)
		{
			return array1.Zip(array2, (a, b) => (a - b) * (a - b)).Sum();
		}

		//Assumes a, b are equidimensional.
		public static double Distance(this double[] a, double[] b){
			return a.DistanceSquared (b).Sqrt();
		}

		public static double CrossProduct(this double[] array1, double[] array2){
			//Clean implementation:
			//return array1.Zip (array2, (a, b) => a * b).Sum ();

			//Fast implementation
			double sum = 0;
			for(int i = 0; i < array1.Length; i++){
				sum += array1[i] * array2[i];
			}
			return sum;
		}

		
		//C# discriminates against the uint type.
		public static uint Sum(this IEnumerable<uint> items){
			return items.Aggregate ((uint)0, (sum, val) => sum + val);
		}

		public static double[] VectorMean(this IEnumerable<IList<double>> vectors){
			double[] result = new double[vectors.First ().Count];

			foreach(IEnumerable<double> array in vectors){
				int i = 0; //TODO: This is a mess.
				foreach(double d in array){
					result[i++] += d;
				}
			}

			result.MapInPlace(val => val * (1.0 / result.Length));
			return result;
		}

		//This function returns NaN on an empty set. of values.
		public static double Stdev(this IEnumerable<double> vals, double mean){
			double ret = 0;
			int count = 0;
			foreach(double val in vals){
				ret += (val - mean) * (val - mean);
				count++;
			}
			return ret.Sqrt () / (count - 1);
		}

		public static double Stdev(this IList<double> vals, double mean){
			double ret = 0;
			foreach(double val in vals){
				ret += (val - mean) * (val - mean);
			}
			return ret.Sqrt () / vals.Count;
		}

		public static double Stdev(this IEnumerable<double> vals){
			return Stdev (vals, vals.Average());
		}

		public static double Stdev(this IList<double> vals){
			return Stdev (vals, vals.Average ());
		}
	}

	public static class WhetstoneListMath{
		//A spot of Boolean algebra

		//TODO: It may be that these should be renamed All and Any to fit with Linq precedent.
		public static bool Conjunction(this IEnumerable<bool> list){
			//Note that empty conjunction is true.
			
			//Could express as an aggregation, but shortcircuiting is desirable.
			foreach(bool b in list){
				if(!b) return false;
			}
			return true;
		}

		public static bool Disjunction(this IEnumerable<bool> list){
			//Note that empty disjunction is false.

			//Could express as an aggregation, but shortcircuiting is desirable.
			foreach(bool b in list){
				if(b) return true;
			}
			return false;
		}
	}
}

