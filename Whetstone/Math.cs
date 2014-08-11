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
			return array1.Zip (array2, (a, b) => a * b).Sum ();
		}

		
		//C# discriminates against the uint type.
		public static uint Sum(this IEnumerable<uint> items){
			return items.Aggregate ((uint)0, (sum, val) => sum + val);
		}
	}

	public static class WhetstoneListMath{
		//A spot of Boolean algebra

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

