using System;

using System.Text;
using System.Text.RegularExpressions;

using System.Collections.Generic;

using System.Linq;
using Whetstone;

namespace TextCharacteristicLearner
{
	public static class TextProcessor
	{
		public static string[] wordify (string input)
		{
			string normalized = input.ToLower ();

			//string processed = Regex.Replace (normalized, @"\s*([\[\](),.;:¡!¿?""“”«»/\\-])\s*", " $1 ");
			string processed = Regex.Replace (normalized, @"\s*([\[\](),.;:¡!¿?""“”«»/\\_–-])\s*", " ");

			IEnumerable<string> words = Regex.Split(processed, @"\s+");

			words = killStopWords(words);
			words = killNumbers(words);

			IEnumerable<string> allWords = new []{"#START"}.Concat(words).Concat (new []{"#END"});
			return allWords.ToArray ();
		}

		//Hack warning.
		static HashSet<string> stopWords = 
			//"a y e o u el la los las al un una uno unas de en no le les lo yo tu su se de del me te nos" +
			"in ap tecnn cnnmexico cnnméxico c xix today abc n r % s l".Split(' ').Aggregate (new HashSet<string>(), (sum, val) => {sum.Add(val); return sum;}); //No , operator :'(
		public static IEnumerable<string> killStopWords(IEnumerable<string> input){
			return input.Filter(a => !stopWords.Contains(a));
		}
		public static IEnumerable<string> killNumbers(IEnumerable<string> input){
			return input.Select (a => Regex.Replace (a, "^\\d+$", "#NUMBER"));
			//return input.Filter(a => !Regex.IsMatch (a, "\\d+"));
		}


		//INFLECTOR:

		//https://github.com/davidarkemp/Inflector/blob/master/Inflector/Inflector.cs
	}
}

