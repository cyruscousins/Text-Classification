using System;

using System.Collections.Generic;
using System.Text;
using System.Linq;

using System.IO;

using Whetstone;

using System.Diagnostics;

namespace TextCharacteristicLearner
{
	public class LatexDocument{
		StringBuilder document = new StringBuilder();

		public LatexDocument(string title, string author){
			document.Append (LatexExtensions.LatexIntro(title, author));
		}
		
		public LatexDocument(string title, string author, double margin, double width, double height){
			document.Append (LatexExtensions.LatexIntro(title, author, margin, width, height));
		}

		public void Append(string item){
			document.AppendLine (item);
		}

		public void AppendClose(){
			document.Append ("\\end{document}\n");
		}

		public void Write(string path){
			Write (path, a => a);
		}
		
		public void Write(string path, Func<String, String> process){
			File.WriteAllText (path, process(document.ToString ()));
			//TODO: Invoke latex.
		}
	}

	public static class WriteupGenerator{
		public static void ProduceClassifierComparisonWriteup<Ty> (string documentTitle, string author, double width, double height, string outFile, IEnumerable<Tuple<string, IEventSeriesProbabalisticClassifier<Ty>>> classifiers, string datasetTitle, DiscreteSeriesDatabase<Ty> dataset, string criterionByWhichToClassify, int classificationRounds)
		{
			LatexDocument doc = new LatexDocument (documentTitle, author, .8, width, height);

			doc.Append ("\\section{Input Data Overview}\n\n");
			doc.Append (dataset.DatabaseLatexString (datasetTitle + " Database Analysis", new[]{criterionByWhichToClassify}, (int)width - 2));

			//Train all the classifiers
			foreach (Tuple<string, IEventSeriesProbabalisticClassifier<Ty>> classifier in classifiers) {
				classifier.Item2.Train (dataset);
			}

			Tuple<string, ClassifierAccuracyAnalysis<Ty>>[] analyses = classifiers.AsParallel ().AsOrdered ().Select (classifier => new Tuple<string,ClassifierAccuracyAnalysis<Ty>>(classifier.Item1, new ClassifierAccuracyAnalysis<Ty>(classifier.Item2, dataset, criterionByWhichToClassify, .8, classificationRounds, .05).runAccuracyAnalysis())).ToArray();

			Console.WriteLine ("Generated Database Overview.");

			doc.Append (@"\section{Classifier Comparison Overview}");

			//Overview
			doc.Append (analyses.Length.ToString () + " classifiers were compared.  An overview of the results is presented here.\n");

			doc.Append (LatexExtensions.latexLongTableString(
				"l|;c;c".Split (';'),
				"Classifier Name;Classifier Rank;Overall Accuracy".Split(';'),
				new[]{
					new Tuple<string, double>( @"E$[$Random Selection$]$", analyses[0].Item2.expectedAccuracyRandom),
					new Tuple<string, double>( "Top Class Selection", analyses[0].Item2.topClassSelectionAccuracy)
			}.Concat (analyses.Select (analysis => new Tuple<string, double>(@"\hyperref[sec:classification " + analysis.Item1 + "]{" + analysis.Item1 + "}" , analysis.Item2.overallAccuracy))).OrderByDescending (tup => tup.Item2).Select ((tup, index) => new[]{tup.Item1, (index + 1).ToString (), LatexExtensions.colorPercent(tup.Item2)})
			));


			//TODO: No rank for small lists?



			foreach(Tuple<string, ClassifierAccuracyAnalysis<Ty>> classifierAnalysis in analyses){

				doc.Append ("\\section{Feature Synthesizer and Classifier Overview for " + classifierAnalysis.Item1 + "}\n\n");
				doc.Append (@"\label{sec:classification " + classifierAnalysis.Item1 + "}");
				doc.Append (classifierAnalysis.Item2.classifier.ClassifierLatexString("Author Classifier", (int)((width - 1.6) * 10)));

				//Console.WriteLine ("Generated Classifier Overview.");


				doc.Append ("\\section{Full Classifier Accuracy Report for " + classifierAnalysis.Item1 + "}\n\n");
				doc.Append (@"\label{sec:classifier " + classifierAnalysis.Item1 + "}");
				doc.Append (classifierAnalysis.Item2.latexAccuracyAnalysisString());
				
				//Console.WriteLine ("Generated Classifier Accuracy Report.");

			}


			doc.AppendClose ();
			doc.Write (outFile, s => AsciiOnly(s, false)); //s => s.RegexReplace (@"[^\u0000-\u007F\u0080-\u0099]", string.Empty));
			
		}

		public static void ProduceClassificationReport<Ty>(string documentTitle, string author, double width, double height, string outFile, IEventSeriesProbabalisticClassifier<Ty> classifier, DiscreteSeriesDatabase<Ty> dataset, string datasetTitle, string criterionByWhichToClassify){
			
			LatexDocument doc = new LatexDocument(documentTitle, author, .8, width, height);

			doc.Append ("\\section{Input Data Overview}\n\n");
			doc.Append (dataset.DatabaseLatexString(datasetTitle + " Database Analysis", new[]{criterionByWhichToClassify}, (int)(width - 2)));

			Console.WriteLine ("Generated Database Overview.");

			classifier.Train (dataset);

			doc.Append ("\\section{Feature Synthesizer and Classifier Overview}\n\n");
			doc.Append (classifier.ClassifierLatexString("Author Classifier", 160));

			Console.WriteLine ("Generated Classifier Overview.");

			doc.Append ("\\section{Classification Report}\n\n");
			doc.Append (classifier.ClassificationReportLatexString(dataset, criterionByWhichToClassify));
			
			Console.WriteLine ("Generated Classification Report.");
			
			//classificationReport.Append ("\\section{Classification Report}\n\n");
			//classificationReport.Append (synth.ClassificationReportLatexString(data, "author"));

			//accuracyReport.Append("\\section{Classifier Accuracy Report}\n\n");
			//accuracyReport.Append (synth.ClassifierAccuracyLatexString(data, "author", .8, 8, .05));


			doc.Append ("\\section{Classifier Accuracy Report}\n\n");
			doc.Append ("*\textbf{Section omitted due to abnormally high number of classes.");
			doc.Append (classifier.ClassifierAccuracyLatexString(dataset, criterionByWhichToClassify, .8, 1, .05));
			
			Console.WriteLine ("Generated Classifier Accuracy Report.");


			doc.AppendClose ();
			doc.Write (outFile, s => AsciiOnly(s, false));

		}



		public static string AsciiOnly(string input, bool includeExtendedAscii)
		{
		    int upperLimit = includeExtendedAscii ? 255 : 127;
		    char[] asciiChars = input.Where(c => (int)c <= upperLimit).ToArray();
		    return new string(asciiChars);
		}



	}

	public static class LatexExtensions
	{
		public static string formatString = "0.###";

		public static int maxDisplayClassCount = 50;

		public static string LatexIntro(string title, string author, double margin = .5, double width = 8.5, double height = 11){
			return @"\documentclass[10pt]{article}

\usepackage{amssymb,amsmath}

\usepackage{fullpage}
\usepackage{verbatim}
\usepackage{listings}
\usepackage{enumerate}

\usepackage{array}

\usepackage{rotating}
\usepackage{tablefootnote}

\usepackage{titling}

\usepackage[T1]{fontenc}
\usepackage[utf8]{inputenc}

\usepackage{graphicx}

\usepackage{hyperref}
\usepackage{color}

\usepackage{indentfirst}

\usepackage{longtable}

" + "\\usepackage[margin=" + margin + "in, paperwidth=" + width + "in, paperheight=" + height + "in]{geometry}\n"

 + "\\title{" + title + "}\n\\author{" + author + "}\n\n" +
@"\begin{document}

\maketitle
\tableofcontents
\listoffigures
\pagebreak[4]

\setcounter{MaxMatrixCols}{200}

";
		}

		public static string DatabaseLatexString<Ty> (this DiscreteSeriesDatabase<Ty> db, string dbName, IEnumerable<string> criteriaToEnumerate, int firstN, string objName = "word")
		{

			//If not provided criteria, use all.
			if (criteriaToEnumerate == null || criteriaToEnumerate.IsEmpty ()) {
				criteriaToEnumerate = db.data.SelectMany (item => item.labels.Keys).Distinct ().Where (item => item != "filename");
			}

			Tuple<string, string[]>[] criterionInformation = db.getLabelCriteria ().Select (criterion => new Tuple<string, string[]> (criterion, db.data.Select (item => item.labels.GetWithDefault (criterion)).Distinct ().Where (val => val != null).Order ().ToArray ())).ToArray ();
			int criteriaCount = criterionInformation.Length;

			StringBuilder result = new StringBuilder ();

			result.AppendLine ("The database contains " + englishCountOfString(objName, db.TotalItemCount ()) + " words across " + englishCountOfString("document", db.data.Count) + ".");
			result.AppendLine ("Documents are labeled along " + englishCountOfString("criterion", criteriaCount) + ".");

			result.AppendLine (
				criterionInformation.Select (
					info => "Classification criterion " + @"\texttt{" + info.Item1 + "}" + " contains " + info.Item2.Length + " categories.  "
				+ ((info.Item2.Length <= 20) ? ((info.Item2.Length == 1 ? "It is" : "They are") + " " + foldToEnglishList (info.Item2.Select (item => @"\texttt{" + item + "}")) + ".") : "")
			).FoldToString ("", "", "")
			);

			result.AppendLine ("\\subsection{" + dbName + " overview}\n");

			if (db.data.Count > 10000) {
				result.AppendLine (@"\textbf{Omitted because over 10000 entries exist.}");
			}
			else{
				List<DiscreteEventSeries<Ty>> items = db.data.OrderBy (a => a.labels ["filename"]).ToList (); //Sort.
				result.AppendLine ("\\subsubsection{All Dataset Entries}\n");

				result.AppendLine ("Here all entries in the dataset are presented, along with the first " + englishCountOfString(objName, firstN) + " of the entry and their classes by all available criteria.");

				result.AppendLine ("\\begin{enumerate}[1.]");
				result.AppendLine ("\\itemsep0pt");

				foreach (DiscreteEventSeries<Ty> item in items) {
					result.AppendLine ("\\item " + item.labels ["filename"] + " (" + englishCountOfString(objName, item.data.Length) + ")" 
						+ " $\\in$ " + foldToEnglishList (item.labels.Keys.Where (key => key != "filename").Select (key => key + ":" + item.labels [key])) + ".  ``"
						+ seriesToSafeString (item, firstN) + "''."
					);
				}
				result.Append ("\\end{enumerate}\n");

				if(db.data.Count > 2500){
					result.AppendLine (@"\textbf{Omitted because over 2500 entries exist.}");
				}
				else{
					result.Append ("\\subsubsection{" + dbName + " categories}\n");
					result.Append ("\\begin{enumerate}[1.]\n");

					foreach(string key in criteriaToEnumerate.OrderBy (item => item)){
						result.AppendLine ("\\item " + key + "(" + items.Where (item => item.labels.ContainsKey (key)).Count() + " labeled entries):");
						result.Append ("\\begin{enumerate}[I.]\n");
						result.Append (items.GroupBy (item => item.labels.GetWithDefault(key, "\\texttt{none}")) //Group by category
						    .OrderBy (item => item.Key == "\\texttt{none}" ? 1 : 0).ThenBy (item => item.Key) //Order by name, with none last
							.FoldToString (item => item.Key + " (" + item.Count() + " entries, " + englishCountOfString(objName, item.Select (subitem => subitem.data.Length).Sum()) + ")\n" //Count words per category;
						   		+ item.FoldToString (subitem => subitem.labels["filename"] + " (" + subitem.data.Length + " words)", "\\begin{enumerate}[i.]\n  \\item ", "\\end{enumerate}\n", "\n  \\item "), "\\item ", "\n" , "\n\\item ")); //Show each item in category.
						result.Append ("\\end{enumerate}\n");
					}
					result.AppendLine ("\\end{enumerate}\n");
				}
			}


			//Insights
			result.AppendLine ("\\subsection{Insights}");

			Tuple<string, Tuple<string, DiscreteEventSeries<Ty>[]>[]>[] dataByCriterionAndClass = criteriaToEnumerate.Select (criterion =>
				new Tuple<string, Tuple<string, DiscreteEventSeries<Ty>[]>[]>(
					criterion, db.data.Where (item => item.labels.ContainsKey(criterion)).GroupBy (item => item.labels[criterion])
.Select (grp => 
			         new Tuple<string, DiscreteEventSeries<Ty>[]>(grp.Key, grp.ToArray ()))
				.OrderBy (tup => tup.Item1)
				.ToArray ()

				)
			).OrderBy (tup => tup.Item1).ToArray();

			//Overview across all criteria:

			result.AppendLine (@"\subsubsection{Aggregate Statistics over all Criteria}");
			result.AppendLine (latexLongTableString (
				"l |".Cons (Enumerable.Range (0, 6).Select (i => "c")), //Format
				"Criterion Name;Class Count;Min Class Size;Max Class Size;Mean Class Size;Stdev Class Size".Split (';'), //Header
			    dataByCriterionAndClass.Select (row => new[]{
					row.Item1, 
					row.Item2.Length.ToString (), 
					row.Item2.Select (@class => @class.Item2.Length).Min ().ToString (), 
					row.Item2.Select (@class => @class.Item2.Length).Max ().ToString (), 
					row.Item2.Select (@class => @class.Item2.Length).Average ().ToString (), 
					@"\texttt{Omitted}" //TODO, stdev
				})
			));



			foreach(Tuple<string, Tuple<string, DiscreteEventSeries<Ty>[]>[]> criterionData in dataByCriterionAndClass){
				int classCount = criterionData.Item2.Length;

				int[] instanceCounts = criterionData.Item2.Select(list => list.Item2.Length).ToArray();
				int totalInstanceCount = instanceCounts.Sum ();

				int[] wordCounts = criterionData.Item2.Select(list => list.Item2.Select(item => item.data.Length).Sum()).ToArray ();
				int totalWordCount = wordCounts.Sum ();
				
				double meanInstanceCount = (totalInstanceCount / (double)classCount);
				
				result.AppendLine (@"\subsubsection{Analysis of criterion " + criterionData.Item1 + "}\n");

				result.AppendLine (@"\textbf{Class overview}" + "\n");
				result.AppendLine (latexLongTableString (
					Enumerable.Range (0, 1).Select (val => "l |").Concat (Enumerable.Range (0, 3).Select (val => "c")),
					"Class Name;Class Instance Count;Sum Word Count;Average Word Count".Split (';'),
					new[]{@"\textbf{Average Class}", meanInstanceCount.ToString (formatString),  (totalWordCount / (double)classCount).ToString (), (totalWordCount / (double)totalInstanceCount).ToString (formatString)}.Cons ( //All row
						Enumerable.Range (0, classCount).Select (classIndex => 
					    	new[]{
								criterionData.Item2[classIndex].Item1,
								instanceCounts[classIndex].ToString (),
								wordCounts[classIndex].ToString (),
								(wordCounts[classIndex] / (double) instanceCounts[classIndex]).ToString (formatString)
							}
						)
				)));
				
				//Class imbalance analysis

				double largeLim = 2;
				double smallLim = .5;

				Tuple<string, int>[] largeClasses = criterionData.Item2
					.Select(item => new Tuple<string, int>(item.Item1, item.Item2.Length))
					.Where(item => item.Item2 > largeLim * meanInstanceCount).OrderByDescending(tup => tup.Item2).ToArray ();


				Tuple<string, int>[] smallClasses = criterionData.Item2
					.Select(item => new Tuple<string, int>(item.Item1, item.Item2.Length))
					.Where(item => item.Item2 < smallLim * meanInstanceCount).OrderBy(tup => tup.Item2).ToArray ();

				if(largeClasses.Length + smallClasses.Length > 0){
					if(largeClasses.Length > 0){
						result.AppendLine (@"\textbf{Oversized classes}" + "\n");
						result.AppendLine ("These classes are larger than " + largeLim + " times the mean class size.\n"); //TODO sing/plur
						
						result.AppendLine (@"\begin{itemize}");
						result.AppendLine (largeClasses.Select (item => "Class " + item.Item1 + " contains " + item.Item2 + " instances, which is " + (item.Item2 / meanInstanceCount).ToString (formatString) + " times the average.").FoldToString("\t\\item ", "", "\t\\item "));
						result.AppendLine (@"\end{itemize}");

						result.AppendLine ();
					}

					if(smallClasses.Length > 0){
						result.AppendLine (@"\textbf{Undersized classes}" + "\n");
						result.AppendLine ("These classes are smaller than " + smallLim + " times the mean class size.\n"); //TODO sing/plur
						
						result.AppendLine (@"\begin{itemize}");
						result.AppendLine (smallClasses.Select (item => "Class " + item.Item1 + " contains " + item.Item2 + " " + (item.Item2 == 1 ? "instance" : "instances")+ ", which is " + (item.Item2 / meanInstanceCount).ToString (formatString) + " times the average.").FoldToString("\t\\item ", "", "\t\\item "));
						result.AppendLine (@"\end{itemize}");

						result.AppendLine ();
					}
				}
			}

			return result.ToString ();
		}

		public static string ClassifierLatexString<Ty>(this IEventSeriesProbabalisticClassifier<Ty> featureSynth, string classifierName, int textWrap){
			StringBuilder result = new StringBuilder();

			result.Append ("\\subsection{" + classifierName + "}\n");
			
			result.AppendLine ("Here relevant information is provided on the classifier used to generate the report.");
			result.AppendLine ("Generally speaking, the type of the classifier and all parameters are given in the first line, and a complete report of all information learned from training data follows.\\footnote{On a technical note, the reason for the clear difference in representation quality between this section and the remainder of the report is that this section is generated from a string produced by implemetations of the \\texttt{IEventSeriesProbabalisticClassifier<string>} interface, whereas the remaining sections involving the classifier use only the public contract to obtain their data.}");


			string fsynthstr = WordWrap(featureSynth.ToString (), textWrap);

			//TODO: Escape?
			//Package inputenc Error: Unicode char \u8:² not set up for use with LaTeX.

			//TODO: Make this less bad.
			result.Append ("\\begin{verbatim}\n");
			result.Append (fsynthstr);
			result.Append ("\n\\end{verbatim}\n");

			//TODO: Accuracy profiling.

			return result.ToString ();

		}

		//HELPER

		public static double fzeroToOne(double d){
			return (d == 0) ? 1 : d;
		}

		//COLOR & TEXT

		internal static string colorString(string s, double d){
			if(d < 0) d = 0;
			else if(d > 1) d = 1;
			double brightness = (1 - d) * 0.85;
			return @"\textcolor[gray]{" + brightness.ToString (formatString) + "}{" + s + "}";
		}

		internal static string colorDouble(double d){
			if(Double.IsNaN(d)) return colorString ("-", .8);
			else if (Double.IsPositiveInfinity(d))  return @"$\infty$";
			else if (Double.IsNegativeInfinity (d)) return @"$-\infty$";
			return colorString(d.ToString (formatString), d);
		}

		internal static string colorDouble(double val, double outof){
			//Gets caught down the line as a NaN.
			/*
 			if(outof == 0){
				return "0.000";
			}
			*/
			return colorString (val.ToString (formatString), val / outof);
		}

		internal static string colorPercent(double d, int places = 2){
			if(Double.IsNaN(d)) return colorString ("-", .8);
			double colorVal = Math.Min (1, Math.Abs (d));
			return colorString ((d * 100).ToString ("F" + places) + @"\%", colorVal);

		}

		public static string ClassifierAccuracyLatexString<Ty> (this IEventSeriesProbabalisticClassifier<Ty> featureSynth, DiscreteSeriesDatabase<Ty> labeledData, string criterionByWhichToClassify, double trainSplitFrac, int iterations, double bucketSize)
		{
			ClassifierAccuracyAnalysis<Ty> analysis = new ClassifierAccuracyAnalysis<Ty>(featureSynth, labeledData, criterionByWhichToClassify, trainSplitFrac, iterations, bucketSize);
			analysis.runAccuracyAnalysis();

			return analysis.latexAccuracyAnalysisString();
		}

		
		public static string FeatureSynthesizerLatexString<Ty> (this IFeatureSynthesizer<Ty> featureSynth, DiscreteSeriesDatabase<Ty> data)
		{
			//TODO: This

			//Show what features are generated for some documents.

			string[] schema = featureSynth.GetFeatureSchema();

			IEnumerable<double[]> generations = data.Select (item => featureSynth.SynthesizeFeatures(item));

			StringBuilder result = new StringBuilder();
			
			result.AppendLine (@"\subsection{Feature Synthesizer output}");


			if(schema.Length < 50){

			}
			else{

			}

			result.AppendLine ("Feature Generation Schema:\n");
			//result.AppendLine (@"\begin{verbatim}");
			result.AppendLine (schema.Select (item => item.RegexReplace ("([{}])", @"\$1")).FoldToString (@"\{", @"\}", ", ") + "\n");
			//result.AppendLine (@"\end{verbatim}");
			result.AppendLine("Synthesized Features:\n");

			//result.AppendLine (@"\begin{verbatim}");
			result.AppendLine (generations.Zip (data, (item, info) => info.labels["filename"] + ":" + info.labels["region"] + item.Select(val => val.ToString (formatString)).FoldToString()).FoldToString ("", "", "\n\n"));
			//result.AppendLine (@"\end{verbatim}");

			//TODO: Statistics on these values.












			return result.ToString ();


		}
		
		public static string ClassificationReportLatexString<Ty> (this IEventSeriesProbabalisticClassifier<Ty> featureSynth, DiscreteSeriesDatabase<Ty> dataToClassify, string criterionByWhichToClassify)
		{
			dataToClassify = dataToClassify.Filter (item => !item.labels.ContainsKey (criterionByWhichToClassify));

			string[] schema = featureSynth.GetClasses();
			string[] schemaText = schema.Select (item => @"\text{" + limitLength (item, 20) + "}").ToArray ();
			string[] schemaTextRotated = schemaText.Select (item => @"\begin{turn}{70}" + item + @"\end{turn}").ToArray ();

			//filename, prediction class, prediction strength, prediction score vector.
			//TODO: Make this schema line up with the above as closely as possible.

			//TODO: AsParallel?
			IEnumerable<Tuple<string, string, double, double[]>> classifications = dataToClassify.data. /* AsParallel(). */ Select (item => 
				{
					double[] vals = featureSynth.Classify(item);
					int maxIndex = vals.MaxIndex();
					return new Tuple<string, string, double, double[]>(item.labels["filename"], schema[maxIndex], vals[maxIndex], vals);
				});

			int topNClasses = 5;

			StringBuilder result = new StringBuilder();
			
			result.AppendLine ("Here a report of the classifier's prediction for each unknown input, of which there were " + dataToClassify.data.Count + ", is presented.  For more information on how to interpret these values, check out the Accuracy Report section.");
			result.AppendLine ("Generally speaking, the higher the prediction strength, the higher the probability that the classification is correct.");

			result.AppendLine (@"\subsection{Overview}");
			result.AppendLine ("Here the classification results are presented in a quick summary.  The predicted class is given, along with the prediction weight, and the prediction weights of the next " + (topNClasses - 1) + " guesses.");
			result.AppendLine ("One can generally be more confident when the primary guess has high value and the remaining guesses do not.");
			result.AppendLine ();

			classifications = classifications.ToArray ();

			result.AppendLine (latexLongTableString (
				"l|".Cons(Enumerable.Range(0, topNClasses).Select(i => "c")),
				"Instance Name".Cons(Enumerable.Range(0, topNClasses).Select(i => ordinal ((i + 1), true))),
				classifications.OrderByDescending(tuple => tuple.Item3).Select (
				item => limitLength (item.Item1, 25).Cons (item.Item4.Select ((score, index) => new Tuple<double, string>(score, schemaText[index])).OrderByDescending (tup => tup.Item1).Take (topNClasses).Select (final => final.Item2 + ":" + colorPercent (final.Item1)))
				)
			));

			//Extremely basic overview:
			/*
			result.AppendLine (latexLongTableString (
				"l;c;c".Split (';'),
				"Instance Name;Predicted Class;Prediction Strength".Split (';'),
				classifications.OrderByDescending(tuple => tuple.Item3).Select (
					item => new string[]{limitLength (item.Item1, 16), item.Item2, @"\textbf{" + colorPercent(item.Item3) + "}" }
				)
			));
			*/

			//TODO: It may be good to get this top score into the other table as well.

			result.AppendLine (@"\subsection{Full Report}");


			int lim = 50;
			if(schemaText.Length > lim){
				result.AppendLine (@"*\textbf{Omitting full report, there are " + schemaText.Length + " classes, cannot display more than " + lim + " classes.}*");
			}
			else{

				//Shorten this for the full report.
				schemaText = schema.Select (item => @"\text{" + limitLength (item, 10) + "}").ToArray ();

				result.AppendLine (latexLongTableString (
					"l;c;c".Split (';').Concat (schemaText.Select(item => "c")),
					"Instance Name;Predicted Class;Prediction Strength".Split (';').Concat (schemaText),
					classifications.OrderByDescending(tuple => tuple.Item3).Take(200).Select ( //TODO: Get rid of this Take.  Why are we hitting a memory wall?
					item => new string[]{limitLength (item.Item1, 16), item.Item2, @"\textbf{" + colorPercent(item.Item3) + "}" }.Concat<string>(item.Item4.Select (val => colorPercent(val)))
					)
				));
			}

			return result.ToString ();

		}


		//MATRIX:
		internal static string latexMatrixString(string envname, bool mathmode, IEnumerable<IEnumerable<string>> data){
			StringBuilder result = new StringBuilder();
			result.AppendLine ((mathmode ? "$$" : "") + @"\begin{" + envname + "}");
			result.AppendLine (data.FoldToString (row => row.FoldToString("\t", "", " & "), "", "", " \\\\\n"));
			result.AppendLine (@"\end{" + envname + "}" + (mathmode ? "$$" : ""));
			return result.ToString ();
		}

		internal static string labeledMatrixCorner = "\\cdot";
		internal static string latexLabeledMatrixString(string envName, IList<string> labels, IEnumerable<IEnumerable<string>> data){
			return latexMatrixString (envName, true, labeledMatrixCorner.Cons (labels).Cons (data.Select ((row, index) => labels[index].Cons (row))));
		}

		/*
		internal static string latexLabeledMatrixSplitIfNecessary(string envName, IList<string> labels, IEnumerable<IEnumerable<string>> data){
			string[] labels 
		}
		*/

		//TODO: Choice to include single lines.
		//TODO: Choice to rotate the labels
		internal static string latexTabularLabeledMatrixString(IList<string> labels, IEnumerable<IEnumerable<string>> data){

			//Prepend labels onto the first item of data
			data = data.Select ((item, index) => labels[index].Cons (item)); 

			int angle = 60;
			bool lines = false;

			StringBuilder result = new StringBuilder();
			result.AppendLine (@"\begin{" + tableEnvName + "}{" + "" + (labels.Select (item => "c").FoldToString ("| l || ", "|", lines ? " | " : " ")) + "}");
			result.AppendLine (@"\hline");
			result.AppendLine (labels.Select (label => @"\begin{turn}{" + angle + "} " + label + @" \end{turn}").FoldToString ("\t\\null & ", " \\\\", " & "));
			result.AppendLine (data.FoldToString(row => row.FoldToString ("\t", " \\\\\n", " & "), "\\hline\\hline\n\t", "\t\\hline", lines ? "\t\\hline\n" : ""));
			result.AppendLine (@"\end{" + tableEnvName + "}");
			return result.ToString ();
		}


		//TABLE:
		internal static string tableEnvName = "tabular";
		internal static string latexTabularString(IEnumerable<string> modes, IEnumerable<IEnumerable<string>> data){
			StringBuilder result = new StringBuilder();
			result.AppendLine (@"\begin{" + tableEnvName + "}{" + modes.FoldToString ("|", "|", " | ") + "}");
			result.AppendLine (data.FoldToString(row => row.FoldToString ("\t", " \\\\\n", " & "), "\t\\hline\n", "\t\\hline\n", "\t\\hline\n"));
			result.AppendLine (@"\end{" + tableEnvName + "}");
			return result.ToString ();
		}
		internal static string latexTabularString(int count, IEnumerable<IEnumerable<string>> data){
			return latexTabularString(Enumerable.Range (0, count).Select (item => "c"), data);
		}


		//LONGTABLE
		internal static string longTableEnvName = "longtable";
		internal static string latexLongTableString(IEnumerable<string> modes, IEnumerable<string> header, IEnumerable<IEnumerable<string>> data){
			StringBuilder result = new StringBuilder();
			result.AppendLine (@"\begin{" + longTableEnvName + "}{" + modes.FoldToString ("|", "|", " | ") + "}");
			result.AppendLine (@"\hline");
			result.AppendLine (header.FoldToString ("\t", " \\\\", " & "));
			result.AppendLine (@"\hline\hline\endhead");
			result.AppendLine (data.FoldToString(row => row.FoldToString ("\t", " \\\\\n", " & "), "\t\\hline\n", "\t\\hline\n", "\t\\hline\n"));
			result.AppendLine (@"\end{" + longTableEnvName + "}");
			return result.ToString ();
		}


		internal static string elipsis = "..."; //"…"; //TODO: Elipsis gets picked up as nonprintable.
		public static string limitLength(string s, int lim){
			if(s.Length > lim){
				return (s.Substring (0, lim - 1) + elipsis).Replace (@"\" + elipsis, elipsis); //TODO: Think about this.
			}
			return s;
		}

		//Make the top n words into a safe latex string.
		internal static string seriesToSafeString<Ty>(DiscreteEventSeries<Ty> series, int n){
			return series.Where (item => item.ToString ().Length > 0 && item.ToString ()[0] != '#').Take (n).Select (str => AsciiOnly(str.ToString (), false).RegexReplace("([#_$&])", "\\$1")).FoldToString ("", "", " ");
		}
		
		public static string AsciiOnly(string input, bool includeExtendedAscii)
		{
		    int upperLimit = includeExtendedAscii ? 255 : 127;
		    char[] asciiChars = input.Where(c => (int)c <= upperLimit).ToArray();
		    return new string(asciiChars);
		}

		//http://www.codeproject.com/Articles/51488/Implementing-Word-Wrap-in-C
		
		/// <summary>
		/// Word wraps the given text to fit within the specified width.
		/// </summary>
		/// <param name="text">Text to be word wrapped</param>
		/// <param name="width">Width, in characters, to which the text
		/// should be word wrapped</param>
		/// <returns>The modified text</returns>
		public static string WordWrap(string text, int width)
		{
		    int pos, next;
		    StringBuilder sb = new StringBuilder();

		    // Lucidity check
		    if (width < 1)
		        return text;

		    // Parse each line of text
		    for (pos = 0; pos < text.Length; pos = next)
		    {
		        // Find end of line
		        int eol = text.IndexOf(Environment.NewLine, pos);
		        if (eol == -1)
		            next = eol = text.Length;
		        else
		            next = eol + Environment.NewLine.Length;

		        // Copy this line of text, breaking into smaller lines as needed
		        if (eol > pos)
		        {
		            do
		            {
		                int len = eol - pos;
		                if (len > width)
		                    len = BreakLine(text, pos, width);
		                sb.Append(text, pos, len);
		                sb.Append(Environment.NewLine);

		                // Trim whitespace following break
		                pos += len;
		                while (pos < eol && Char.IsWhiteSpace(text[pos]))
		                    pos++;
		            } while (eol > pos);
		        }
		        else sb.Append(Environment.NewLine); // Empty line
		    }
		    return sb.ToString();
		}

		public static string foldToEnglishList(IEnumerable<string> list){
			IEnumerator<string> enumerator = list.GetEnumerator();
			if(enumerator.MoveNext ()){
				
				StringBuilder result = new StringBuilder();

				result.Append (enumerator.Current); //First

				if(!enumerator.MoveNext ()){
					return result.ToString (); // If there's only one element, return it.
				}

				string thisItem = enumerator.Current;
				while(enumerator.MoveNext()) {
					result.Append (", ");
					result.Append (thisItem);
					thisItem = enumerator.Current;
				}

				result.Append (", and ");
				result.Append (thisItem);

				return result.ToString();
			}
			else{
				return "";
			}
		}
		/// <summary>
		/// Locates position to break the given line so as to avoid
		/// breaking words.
		/// </summary>
		/// <param name="text">String that contains line of text</param>
		/// <param name="pos">Index where line of text starts</param>
		/// <param name="max">Maximum line length</param>
		/// <returns>The modified line length</returns>
		internal static int BreakLine(string text, int pos, int max)
		{
		    // Find last whitespace in line
		    int i = max;
		    while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
		        i--;

		    // If no whitespace found, break at maximum length
		    if (i < 0)
		        return max;

		    // Find start of whitespace
		    while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
		        i--;

		    // Return length of text before whitespace
		    return i + 1;
		}

		internal static string[] quantificationAdverbPhrases = "infinitessimally;ever so slightly;a tiny bit;slightly;noticably;somewhat;quite;very;heavily;quite heavily;extremely;near completely".Split(';');
		internal static string quantificationAdverbPhrase(double d){
			if(d < 0.0001) return "not";
			if(d > 0.999) return "fully";
			return quantificationAdverbPhrases[(int)(d * quantificationAdverbPhrases.Length)];
		}

		internal static string[] frequencyQuantificationAdverbPhrases = "infinitessimally infrequently;virtually never;intermittently;rarely;occasionally;infrequntly;sometimes;often;quite often;frequently;more often than not;usually;quite frequently;nearly constantly;almost always".Split (';');
		//English language functions
		internal static string frequencyQuantificationAdverbPhrase(double d){
			if(d < .0001){
				return "never";
			}
			if(d > .9999){
				return "always";
			}
			return frequencyQuantificationAdverbPhrases[(int)(d * quantificationAdverbPhrases.Length)];
		}

		internal static string ordinal (int num, bool preferNumeric)
		{
			if (!preferNumeric) {
				switch (num) {
					case 0:
						return "zeroeth";
					case 1:
						return "first";
					case 2:
						return "second";
					case 3:
						return "third";
					case 4:
						return "fourth";
					case 5:
						return "fifth";
					case 6:
						return "sixth";
					case 7:
						return "seventh";
					case 8:
						return "eighth";
					case 9:
						return "ninth";
					case 10:
						return "tenth";
				}
			}

			//Special cases
			switch(num){
				case 11:
				case 12:
				case 13:
					return num + @"\textsuperscript{th}";
			}
			//Normal cases
			switch(num % 10){
				case 1:
					return num + @"\textsuperscript{st}";
				case 2:
					return num + @"\textsuperscript{nd}";
				case 3:
					return num + @"\textsuperscript{rd}";
				default:
					return num + @"\textsuperscript{th}";
			}
		}

		public static string englishNumberString(int number, bool preferNumeric = false){
			if (!preferNumeric) {
				switch (number) {
					case 0:
						return "zero";
					case 1:
						return "one";
					case 2:
						return "two";
					case 3:
						return "three";
					case 4:
						return "four";
					case 5:
						return "five";
					case 6:
						return "six";
					case 7:
						return "seven";
					case 8:
						return "eight";
					case 9:
						return "nine";
					case 10:
						return "ten";
				}
			}
			return number.ToString ();
		}

		public static string pluralPhrase(string s, int count){
			if(count == 1) return s;
			return s.Split (' ').Select (word => plural (word)).FoldToString ("", "", " ");
		}

		public static string englishCountOfString(string s, int count, bool preferNumeric = false){
			return englishNumberString(count, preferNumeric) + " " + ((count == 1) ? s : plural(s));
		}

		public static string plural(string s){
			//Adjectives
			switch(s){
				case "this":
					return "these";
				case "that":
					return "those";
				case "a":
				case "an":
					return "some";
				case "the":
					return "the";
			}

			//Nouns
			switch(s){
				case "formula":
					return "formulae";
				case "hypothesis":
					return "hypothesi";
				case "criterion":
					return "criteria";
				case "datum":
					return "data";
				case "series":
					return "series";
				case "schema":
					return "schemata";

				case "it":
					return "they";
			}

			//Verbs
			switch(s){
				case "is":
					return "are";
				case "has":
					return "have";
			}

			if(s.EndsWith ("x")){
				return s.Substring(0, s.Length - 1) + "ces";
			}
			if(s.EndsWith ("y")){
				return s.Substring(0, s.Length - 1) + "es";
			}
			if(s.EndsWith ("o")){ //I think this is usually true.
				return s + "es";
			}
			return s + "s";
		}

	}
}

