using System;

using System.Collections.Generic;

using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.Compression;

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

		public static void LoadTextDatabase(this DiscreteSeriesDatabase<string> db, TextReader inStream, Func<string, string> textProcessor){
			LoadTextDatabase(db, "", inStream, textProcessor);
		}

		public static void LoadTextDatabase(this DiscreteSeriesDatabase<string> db, string directory, TextReader inStream, Func<string, string> textProcessor, int logLevel = 0) { //TODO make this an extension method for DiscreteSeries<string> ?
			List<DiscreteEventSeries<string>> fileData = db.data;

			//Read the file in
			string s = inStream.ReadToEnd ();
			string[] entries = s.Split (newLine, StringSplitOptions.RemoveEmptyEntries);

			//TODO: Zip archive version of the following.

			//Read each file
			List<DiscreteEventSeries<string>> items = entries.AsParallel().Select(entry => {
				Dictionary<string, string> entryDict = processEntryLine(entry, logLevel);
				return processEntryFromFile(directory, entryDict, logLevel, textProcessor);
			}).Where (entry => entry != null).ToList ();

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

		//This function when called shall produce a dictionary mapping all available criteria to the label provided in the input string.  The format is:
		//FILENAME CRITERION:FILE
		public static Dictionary<string, string> processEntryLine(string entry, int logLevel){
			//Split into info and path
			string[] line = entry.Split (' ');

			string tagsInfo = line[0];

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

			return tags;
		}

		public static DiscreteEventSeries<string> processEntryFromFile(string directory, Dictionary<string, string> tags, int logLevel, Func<string, string> textProcessor){

			//Load the file as a set of words
			string filePath = directory + tags["filename"];

			//Add the file as an entry to the database.
			using (StreamReader sr = File.OpenText(filePath)) {
				return loadEntry (tags, sr, logLevel, textProcessor);
			}
		}
		
		//TODO .NET 4.5 only.
		/*
		public static DiscreteEventSeries<string> processEntryFromZipArchive (ZipArchive archive, Dictionary<string, string> tags, string entry, int logLevel, Func<string, string> textProcessor){

			//Load the file as a set of words
			string filePath = directory + tags["filename"];

			//Add the file as an entry to the database.
			using (StreamReader sr = File.OpenText(filePath)) {
				return loadEntry (tags, sr, logLevel, textProcessor);
			}
		}
		*/

		public static DiscreteEventSeries<string> loadEntry(Dictionary<string, string> tags, StreamReader streamReader, int logLevel, Func<string, string> textProcessor){
			string[] words = DatabaseLoader.loadWordFileRaw (streamReader, textProcessor);
			DiscreteEventSeries<string> file = new DiscreteEventSeries<string>(tags, words);

			if(logLevel >= 2) Console.WriteLine ("Read " + tags.FoldToString (item => item.Key + ":" + item.Value) + ": " + words.Length + " words.");

			return file;
		}


		//Load a word file

		public static string[] loadWordFileRaw(StreamReader stream, Func<string, string> textProcessor){
			String file = textProcessor(stream.ReadToEnd ());
			string[] result = TextProcessor.wordify(file);
			return result;
		}

		public static string[] loadWordFileRaw(string filePath){
			using (StreamReader sr = File.OpenText(filePath)) {
				return loadWordFileRaw (sr, a => a);
			}
		}

		//Text preprocessor functions
		public static string ProcessEnglishText(string s){
			return s.RegexReplace (@"[^a-zA-Z0-9àèìòùÀÈÌÒÙäëïöüÄËÏÖÜ.,;:!?""'`“”/\\ \n\t-]+", ""); //TODO: . in [] is not a special character?
		}
		public static string ProcessSpanishText(string s){
			return s.RegexReplace (@"[^a-zA-Z0-9áéíóúü'ÁÉÍÓÚüÜñÑ.,;:""'`¡!¿?""“”«»/\\ \n\t-]+", "");
		}
	}
}

