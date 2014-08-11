using System;

using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Whetstone;

using System.Linq;
using System.Linq.Parallel;

using System.Diagnostics;


namespace TextCharacteristicLearner
{
	public static class DatabaseLoader
	{
		static string[] newLine = new string[]{"\n", "\r\n"};
		static string whiteSpaceRegex = @"\s+";

		//Load a database file from a stream.

		public static void LoadTextDatabase(this DiscreteSeriesDatabase<string> db, TextReader inStream){
			LoadTextDatabase(db, "", inStream);
		}

		public static void LoadTextDatabase(this DiscreteSeriesDatabase<string> db, string directory, TextReader inStream, int logLevel = 0) { //TODO make this an extension method for DiscreteSeries<string> ?
			List<DiscreteEventSeries<string>> fileData = db.data;

			//Read the file in
			string s = inStream.ReadToEnd ();
			string[] entries = s.Split (newLine, StringSplitOptions.RemoveEmptyEntries);

			//Read each file
			List<DiscreteEventSeries<string>> items = entries.AsParallel().Select(entry => processEntry(directory, entry, logLevel)).Where (entry => entry != null).ToList ();

			fileData.AddRange (items);

			//Print some information about what has been read.
			if(logLevel >= 1){
				Console.WriteLine ("Loaded " + items.Count + " / " + entries.Length + " discrete event series.  " + items.TotalItemCount() + " total words added.");

				if(logLevel >= 3){
					IEnumerable<string> categoryKeys = items.SelectMany (item => item.labels.Keys).Distinct().Where (item => item != "filename");
					foreach(string key in categoryKeys){
						Console.WriteLine ("Classification Criterion: " + key);
						Console.WriteLine (items.GroupBy (item => item.labels.GetWithDefault(key, "[none]")) //Group by category
						                   .FoldToString (item => item.Key + " (" + item.Select (subitem => subitem.data.Length).Sum() + " words): " //Count words per category
						               		+ item.FoldToString (subitem => subitem.labels["filename"] + " (" + subitem.data.Length + " words)"), "", "" , "\n")); //Show each item in category.
					}
				}

			}
		}

		public static DiscreteEventSeries<string> processEntry(string directory, string entry, int logLevel){
			//Console.WriteLine ("Processing: " + entry);

			//Split into info and path
			string[] line = entry.Split (' ');

			string tagsInfo = line[0];
			string filePath = directory + line[1];

			string[] tagsInfoSplit = tagsInfo.Split (";:".ToCharArray());

			if(tagsInfoSplit.Length % 2 == 1){
				Trace.TraceError("Error processing tags of database entry: \"" + entry + "\".");
				return null;
			}

			//Break the tags into a dictionary.
			Dictionary<string, string> tags = new Dictionary<string, string>();

			tagsInfoSplit.ForEachAdjacentPair (tags.Add);

			//Add the filepath to the dictionary too.
			tags.Add ("filename", line[1]);

			//Load the file as a set of words

			//Add the file as an entry to the database.
			using (StreamReader sr = File.OpenText(filePath)) {
				string[] words = DatabaseLoader.loadWordFileRaw (sr);
				DiscreteEventSeries<string> file = new DiscreteEventSeries<string>(tags, words);

				if(logLevel >= 2) Console.WriteLine ("Read " + tags.FoldToString (item => item.Key + ":" + item.Value) + ": " + words.Length + " words.");

				return file;
			}
		}


		//Load a word file

		public static string[] loadWordFileRaw(StreamReader stream){
			String file = stream.ReadToEnd ();
			string[] result = TextProcessor.wordify(file);
			return result;
		}
		
		public static string[] loadWordFileRaw(string filePath){
			using (StreamReader sr = File.OpenText(filePath)) {
				return loadWordFileRaw (sr);
			}
		}
	}
}

