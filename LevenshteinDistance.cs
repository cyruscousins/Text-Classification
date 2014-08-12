using System;

namespace TextCharacteristicLearner
{


	//Adapted from http://www.dotnetperls.com/levenshtein

	/// <summary>
	/// Contains approximate string matching
	/// </summary>
	static class LevenshteinDistance
	{
		public static int[,] DistanceMatrix(string[] inStrings){
			int[,] distances = new int[inStrings.Length, inStrings.Length];
			for(int i = 0; i < inStrings.Length; i++){
				for(int j = 0; j < inStrings.Length; j++){
					if(i == j) continue;
					distances[i,j] = Compute (inStrings[i], inStrings[j]);
				}
			}
			return distances;
		}

		public static double[,] ErrorRateMatrix(string[] inStrings){
			int[,] distances = DistanceMatrix(inStrings);
			double[,] errorRates = new double[inStrings.Length, inStrings.Length];
			for(int i = 0; i < inStrings.Length; i++){
				for(int j = 0; j < inStrings.Length; j++){
					if(i == j) continue;
					errorRates[i,j] = distances[i,j] / ((inStrings[i].Length + inStrings[j].Length) / 2.0);
				}
			}
			return errorRates;
		}

	    /// <summary>
	    /// Compute the distance between two strings.
	    /// </summary>
	    public static int Compute(string s, string t)
	    {
		int n = s.Length;
		int m = t.Length;
		int[,] d = new int[n + 1, m + 1];

		// Step 1
		if (n == 0)
		{
		    return m;
		}

		if (m == 0)
		{
		    return n;
		}

		// Step 2
		//Note: There's something wrong with the person who wrote this.
		for (int i = 0; i <= n; d[i, 0] = i++)
		{
		}

		for (int j = 0; j <= m; d[0, j] = j++)
		{
		}

		// Step 3
		for (int i = 1; i <= n; i++)
		{
		    //Step 4
		    for (int j = 1; j <= m; j++)
		    {
			// Step 5
			int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

			// Step 6
			d[i, j] = Math.Min(
			    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
			    d[i - 1, j - 1] + cost);
		    }
		}
		// Step 7
		return d[n, m];
	    }
	}
}

