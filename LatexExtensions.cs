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
			document.Append (item);
		}

		public void AppendClose(){
			document.Append ("\\end{document}\n");
		}

		public void Write(string path){
			File.WriteAllText (path, document.ToString ());
		}
		
		public void Write(string path, Func<String, String> process){
			File.WriteAllText (path, process(document.ToString ()));
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

		public static string ClassifierLatexString<Ty>(this IFeatureSynthesizer<Ty> featureSynth, string classifierName, int textWrap){
			StringBuilder result = new StringBuilder();

			result.Append ("\\subsection{" + classifierName + "}\n");

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

		private static string colorString(string s, double d){
			if(d < 0) d = 0;
			else if(d > 1) d = 1;
			double brightness = (1 - d) * 0.85;
			return @"\textcolor[gray]{" + brightness.ToString (formatString) + "}{" + s + "}";
		}

		private static string colorDouble(double d){
			if(Double.IsNaN(d)) return colorString ("-", .8);
			else if (Double.IsPositiveInfinity(d))  return @"$\infty$";
			else if (Double.IsNegativeInfinity (d)) return @"$-\infty$";
			return colorString(d.ToString (formatString), d);
		}

		private static string colorDouble(double val, double outof){
			//Gets caught down the line as a NaN.
			/*
 			if(outof == 0){
				return "0.000";
			}
			*/
			return colorString (val.ToString (formatString), val / outof);
		}

		private static string colorPercent(double d, int places = 2){
			if(Double.IsNaN(d)) return colorString ("-", .8);
			double colorVal = Math.Min (1, Math.Abs (d));
			return colorString ((d * 100).ToString ("F" + places) + @"\%", colorVal);

		}

		/*
		private static string colorPercent(double val, double outof){
			if(outof == 0){
				return @"0.00\%";
			}
			else return colorString ((val * 100).ToString ("F2") + @"\%", val / outof);
		}
		*/
		
		//Name, true class, predicted class, scores, winning score;
		public static Tuple<string, string, string, double[], double> classificationInfo<Ty>(IFeatureSynthesizer<Ty> featureSynth, string[] classifierSchema, Dictionary<string, int> trueSchemaMapping, DiscreteEventSeries<Ty> data, string nameCriterion, string criterionByWhichToClassify){

			//scores in the synthesizer scorespace
			double[] synthScores = featureSynth.SynthesizeFeaturesSumToOne (data);

			int maxIndex = synthScores.MaxIndex();
			string predictedClass = classifierSchema[maxIndex];
			double maxScore = synthScores[maxIndex];

			//convert scores to the true space.
			double[] trueScores = new double[trueSchemaMapping.Count];
			for(int j = 0; j < classifierSchema.Length; j++){
				trueScores[trueSchemaMapping[classifierSchema[j]]] = synthScores[j];
			}

			return new Tuple<string, string, string, double[], double> (data.labels[nameCriterion], data.labels[criterionByWhichToClassify], predictedClass, trueScores, maxScore);
		}

		public static string ClassifierAccuracyLatexString<Ty> (this IFeatureSynthesizer<Ty> featureSynth, DiscreteSeriesDatabase<Ty> labeledData, string criterionByWhichToClassify, double trainSplitFrac, int iterations, double bucketSize)
		{

			string nameCriterion = "filename"; //TODO: Input parameter or constant for this.

			//Filter out items not labeled for this criterion.

			labeledData = labeledData.FilterForCriterion (criterionByWhichToClassify);

			string[] datasetSchema = labeledData.getLabelClasses (criterionByWhichToClassify).Order ().ToArray ();

			Dictionary<string, int> schemaMapping = datasetSchema.IndexLookupDictionary ();

			int bucketCount = (int)(1.0 / bucketSize);

			int classCount = datasetSchema.Length;

			int classificationInstancesCount = 0;

			//Raw data classifications:
			//Name, true class, predicted class, scores, winning score;
			List<Tuple<string, string, string, double[], double>> classificationInstances = new List<Tuple<string, string, string, double[], double>> ();

			//Run and make classifiers.
			for (int i = 0; i < iterations; i++) {
				Console.WriteLine ("Classifier Accuracy: Initiating round " + (i + 1) + " / " + iterations);
				
				Tuple<DiscreteSeriesDatabase<Ty>, DiscreteSeriesDatabase<Ty>> split = labeledData.SplitDatabase (trainSplitFrac); //TODO: Vary this?
				DiscreteSeriesDatabase<Ty> training = split.Item1;
				DiscreteSeriesDatabase<Ty> test = split.Item2;

				featureSynth.Train (training);

				string[] classifierSchema = featureSynth.GetFeatureSchema ();

				classificationInstances.AddRange (test.data.AsParallel ().Select (item => classificationInfo (featureSynth, classifierSchema, schemaMapping, item, nameCriterion, criterionByWhichToClassify)));
				
				classificationInstancesCount += test.data.Count;
			}


			//Confusion Matrices.
			int[,] confusionMatrixCounts = new int[classCount, classCount];
			double[,] confusionMatrixScores = new double[classCount, classCount];

			//Confusion matrix allocation
			int[][,] countsConfusionMatricesByConfidence = new int[bucketCount][,];
			double[][,] scoresConfusionMatricesByConfidence = new double[bucketCount][,];

			for (int i = 0; i < bucketCount; i++) {
				countsConfusionMatricesByConfidence [i] = new int[classCount, classCount];
				scoresConfusionMatricesByConfidence [i] = new double[classCount, classCount];
			}

			//Confusion Matrix population
			foreach (var classification in classificationInstances) {
				//Counts
				confusionMatrixCounts [schemaMapping [classification.Item2], schemaMapping [classification.Item3]] ++;
				countsConfusionMatricesByConfidence [(int)Math.Floor (classification.Item5 * bucketCount * .999999)] [schemaMapping [classification.Item2], schemaMapping [classification.Item3]] ++;

				//Scores
				for (int j = 0; j < classCount; j++) {
					confusionMatrixScores [schemaMapping [classification.Item2], j] += classification.Item4 [j];
					scoresConfusionMatricesByConfidence [(int)Math.Floor (classification.Item5 * bucketCount * .999999)] [schemaMapping [classification.Item2], j] += classification.Item4 [j];
				}
			}


			//Aggregates

			double[] countColumnSums = Enumerable.Range (0, classCount).Select (i => (double)confusionMatrixCounts.SumColumn (i)).ToArray ();
			double[] countRowSums = Enumerable.Range (0, classCount).Select (i => (double)confusionMatrixCounts.SumRow (i)).ToArray ();
			double[] scoreColumnSums = Enumerable.Range (0, classCount).Select (i => confusionMatrixScores.SumColumn (i)).ToArray ();
			double[] scoreRowSums = Enumerable.Range (0, classCount).Select (i => confusionMatrixScores.SumRow (i)).ToArray ();

			double[] classCountAccuracies = Enumerable.Range (0, classCount).Select (c => confusionMatrixCounts[c,c] / (double)countRowSums[c]).ToArray ();
			double overallAccuracy = Enumerable.Range (0, classCount).Select (i => (double)confusionMatrixCounts[i,i]).Sum () / classificationInstancesCount;
			double expectedAccuracyRandom = (1.0 / classCount);

			//Safety check.
			{
				double countSum = countColumnSums.Sum ();
				double scoreSum = scoreColumnSums.Sum ();

				//These should all be the same ass instancesClassifiedCount, to within numerics errors.
				Trace.Assert(Math.Abs (countSum - classificationInstancesCount) < .00001);
				Trace.Assert(Math.Abs (scoreSum - classificationInstancesCount) < .00001);
			}

			double instancesClassifiedDouble = classificationInstancesCount;

			//Class, Confidence Bucket
			double[,] accuracyByTrueClassAndConfidence = new double[classCount + 1, bucketCount];
			double[,] accuracyByPredictedClassAndConfidence = new double[classCount + 1, bucketCount];
			
			for (int i = 0; i < bucketCount; i++) {
				for (int j = 0; j < classCount; j++) {
					accuracyByTrueClassAndConfidence [j + 1, i] = (double)countsConfusionMatricesByConfidence [i] [j, j] / (double)countsConfusionMatricesByConfidence [i].SumRow (j);
					accuracyByPredictedClassAndConfidence [j + 1, i] = (double)countsConfusionMatricesByConfidence [i] [j, j] / (double)countsConfusionMatricesByConfidence [i].SumColumn (j);
				}

				//TODO: Never use a try catch block, and punish those who do.
				try {
					accuracyByTrueClassAndConfidence [0, i] = Enumerable.Range (1, classCount).Select (j => accuracyByTrueClassAndConfidence [j, i]).Where (val => !Double.IsNaN (val)).Average ();
				} catch {
					accuracyByTrueClassAndConfidence [0, i] = Double.NaN;
				}
				try{
					accuracyByPredictedClassAndConfidence [0, i] = Enumerable.Range (1, classCount).Select (j => accuracyByTrueClassAndConfidence [j, i]).Where (val => !Double.IsNaN (val)).Average ();
				} catch {
					accuracyByPredictedClassAndConfidence [0, i] = Double.NaN;
				}
			}

			//For use in math mode elements.
			string[] datasetSchemaText = datasetSchema.Select (item => @"\text{" + limitLength (item, 15) + "}").ToArray ();
			string[] datasetSchemaTextRotated = datasetSchemaText.Select (item => @"\begin{turn}{70} " + item + @" \end{turn}").ToArray ();

			//TODO: Limiting length could cause duplication




			StringBuilder result = new StringBuilder();

			//RAW RESULTS

			result.AppendLine (@"\subsection{Classifier Accuracy Estimation Report}");
			result.AppendLine ("From a set of " + labeledData.data.Count + " labeled instances, classifiers were build with a training data to test data ratio of " + trainSplitFrac.ToString(formatString) + " (" + ((int) (trainSplitFrac * labeledData.data.Count())) + " training, " + (labeledData.data.Count - ((int) (trainSplitFrac * labeledData.data.Count()))) + " test instances).");
			result.AppendLine ("The process was repeated " + iterations + " times, for a total of " + classificationInstances.Count + " classifications.");
			result.AppendLine ("The raw results appear here below\\footnote{It may be that an instance appears multiple times in the table below; this is expected behavior, as the training and test datasets are repeatedly randomly selected.  This behavior is not undesirable, as, assuming the highly probable case where the repeated item is repeated classified by a classifier trained on different data in both each instance, each classification instance represents a novel point and is still valuable.}.\n");


			//Matrix form:
			/*
			result.AppendLine (latexMatrixString("bmatrix", true, ("Instance Name;True Class;Predicted Class".Split (';').Concat (datasetSchema.Select (item => item )).Select (item => @"\text{" + item + "}")).Cons (
				classificationInstances.Select (
					item => new string[]{item.Item1, item.Item2, item.Item3}.Select (str => @"\text{" + limitLength (str, 16) + "}").Concat<string>(item.Item4.Select (val => colorDouble(val)))
				)
			)));
			*/

			//Tabular form:
			/*
			result.AppendLine (latexTabularString ("l;c;c".Split (';').Concat (datasetSchema.Select(item => "c")),
				classificationInstances.Select (
					item => new string[]{item.Item1, item.Item2, item.Item3}.Select (str => @"\text{" + limitLength (str, 16) + "}").Concat<string>(item.Item4.Select (val => colorDouble(val)))
				)
			));
			*/

			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Full report omitted because classes exceed limit " + maxDisplayClassCount + "}*.");

				int topnClasses = 5;
				int topnItems = 1000;

				result.AppendLine (latexLongTableString (
					"l|l|".Cons(Enumerable.Range(0, topnClasses).Select(i => "c")),
					"Instance Name;True Class".Split (';').Concat(Enumerable.Range(0, topnClasses).Select(i => ordinal ((i + 1), true))),
					classificationInstances.OrderByDescending(tuple => tuple.Item5). /* Take (topnItems). */ Select (
					item => new[]{limitLength (item.Item1, 25), limitLength (item.Item2, 25)}.Concat (item.Item4.Select ((score, index) => new Tuple<double, string, string>(score, datasetSchema[index], datasetSchemaText[index])).OrderByDescending (tup => tup.Item1).Take (topnClasses).Select ((final, index) => ((final.Item2 == item.Item2) ? final.Item3 : @"\textcolor[rgb]{" + (.9 * 2 / (2 + index)) + "," + (.1 * 2 / (2 + index)) + "," + (.1 * 2 / (2 + index)) + "}{ " + final.Item3 + "}") + @":" + colorPercent (final.Item1)))
					)
				));
				if (classificationInstances.Count > topnItems){
					result.AppendLine (@"*\textbf{Only first " + topnItems + " shown.}"); //TODO also show the last couple?
				}
			}
			else{
				//Longtable form
				result.AppendLine (latexLongTableString (
					"l;c;c".Split (';').Concat (datasetSchemaText.Select(item => "c")),
					"Instance Name;True Class;Predicted Class".Split (';').Concat (datasetSchemaText),
					classificationInstances.OrderByDescending (tuple => tuple.Item5).Select (
					item => new string[]{limitLength (item.Item1, 16), item.Item2, (item.Item3 == item.Item2) ? item.Item3 : (@"\textcolor[rgb]{.9,.1,.1}{" + item.Item3 + "}")}.Concat<string>(item.Item4.Select (val => colorDouble(val)))
					)
				));
			}



			//ACCURACY PROFILE

			result.AppendLine (@"\subsection{True vs. Predicted Class Distributions (Classifier Accuracy)}");

			result.AppendLine ("Although it is one of the most basic metrics for classifier accuracy, comparing the true and predicted class distribution is an excellent way to detect a classifier biased toward or against a particular class.");
			result.AppendLine ("Confusion matrices and other techniques become necessary when the bias becomes more subtle, as in the case where instances of one particular class are often confused for instances of another.");

			
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{True Distribution}:" + "\n");			
				
				result.AppendLine (latexMatrixString ("bmatrix", true, new[]{
					(@"\text{Total}").Cons (datasetSchemaTextRotated), 
					instancesClassifiedDouble.ToString (formatString).Cons (countRowSums.Select (item => colorDouble(item, instancesClassifiedDouble))),
					("-").Cons    (countRowSums.Select (item => colorPercent(item / instancesClassifiedDouble)))

				}));
				
				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{Predicted Distribution}:" + "\n");

				result.AppendLine (latexMatrixString ("bmatrix", true, new[]{
					(@"\text{Total}").Cons (datasetSchemaTextRotated), 
					instancesClassifiedDouble.ToString (formatString).Cons(countColumnSums.Select (item => colorDouble(item, instancesClassifiedDouble))),
					("-").Cons    (countColumnSums.Select (item => colorPercent(item / instancesClassifiedDouble)))
				}));

				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{Error in Distribution (Predicted - True)}:" + "\n");

				result.AppendLine (latexMatrixString ("bmatrix", true, new[]{
					(datasetSchemaTextRotated).Select (str => @"\text{" + str + "}"), 
					(countRowSums.Zip(countColumnSums, (rowSum, columnSum) => (columnSum - rowSum).ToString(formatString))),
					(countRowSums.Zip (countColumnSums, (rowSum, columnSum) => colorPercent((columnSum - rowSum) / instancesClassifiedDouble)))
				}));
			}

			//SCORE PROFILE
			
			result.AppendLine (@"\subsection{True vs. Predicted Class Distributions (Algorithm Score)}");

			//TODO: Writeup for this.  ALgorthim weight score instead of accuracy.
			
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				
				result.AppendLine (@"\par\bigskip");
				result.AppendLine(@"\textbf{True Distribution}:" + "\n");

				result.AppendLine (latexMatrixString ("bmatrix", true, new[]{
					(@"\text{Total}").Cons (datasetSchemaTextRotated), 
					instancesClassifiedDouble.Cons (scoreRowSums).Select (item => colorDouble(item, instancesClassifiedDouble)),
					instancesClassifiedDouble.Cons (scoreRowSums).Select (item => colorDouble(item / instancesClassifiedDouble))
				}));
				
				result.AppendLine (@"\par\bigskip");
				result.AppendLine(@"\textbf{Predicted Distribution}:" + "\n");

				result.AppendLine (latexMatrixString ("bmatrix", true, new[]{
					(@"\text{Total}").Cons (datasetSchemaTextRotated), 
					instancesClassifiedDouble.Cons (scoreColumnSums).Select (item => colorDouble(item, instancesClassifiedDouble)),
					instancesClassifiedDouble.Cons (scoreColumnSums).Select (item => colorDouble(item / instancesClassifiedDouble))
				}));

				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{Error in Distribution (Predicted - True)}:" + "\n");

				result.AppendLine (latexMatrixString ("bmatrix", true, new[]{
					datasetSchemaTextRotated, 
					(scoreRowSums).Zip (scoreColumnSums, (rowSum, columnSum) => (columnSum - rowSum).ToString(formatString)),
					(scoreRowSums).Zip (scoreColumnSums, (rowSum, columnSum) => colorPercent((columnSum - rowSum) / instancesClassifiedDouble))
				}));

			}




			result.AppendLine(@"\subsection{Accuracy Confusion Matrix}");
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				result.AppendLine (@"\textbf{Note}: In all confusion matrices, columns map to predicted class and rows map to true class.");
				result.AppendLine ("All percents refer to the breakdown of the predicted class for the row's true class (thus rows sum to 100\\%).");

				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{Classification Counts Confusion Matrix}:" + "\n");

				result.AppendLine (latexTabularLabeledMatrixString(datasetSchemaText, 
					Enumerable.Range (0, classCount).Select (i => Enumerable.Range (0, classCount).Select (r => confusionMatrixCounts[i,r].ToString ()))));
				
				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{Percents Confusion Matrix}:" + "\n");
				
				result.AppendLine (latexTabularLabeledMatrixString(datasetSchemaText, 
					Enumerable.Range (0, classCount).Select (row => Enumerable.Range (0, classCount).Select (column => colorPercent(confusionMatrixCounts[row,column] / countRowSums[row])))));
			}



			result.AppendLine(@"\subsection{Algorithm Prediction Weight Score Confusion Matrix}" + "\n");
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				result.AppendLine (@"\textbf{Raw Scores Confusion Matrix}:" + "\n");
				//Matrix, not so nice
				/*
				result.AppendLine (latexLabeledMatrixString("bmatrix", datasetSchemaText, 
					Enumerable.Range (0, classCount).Select (row => 
				    	Enumerable.Range (0, classCount).Select (column => confusionMatrixScores[row, column].ToString (formatString)))));
				*/
				//Improved (tabular, rotated labels)
				result.AppendLine (latexTabularLabeledMatrixString(datasetSchemaText,
					Enumerable.Range (0, classCount).Select (row => 
				    	Enumerable.Range (0, classCount).Select (column => confusionMatrixScores[row, column].ToString (formatString)))));



				result.AppendLine (@"\textbf{Percentages Confusion Matrix}:" + "\n");
				//Matrix
				/*
				result.AppendLine (latexLabeledMatrixString("bmatrix", datasetSchemaText, 
					Enumerable.Range (0, classCount).Select (row => 
				    	Enumerable.Range (0, classCount).Select (column => colorPercent(confusionMatrixScores[row, column] / fzeroToOne (scoreRowSums[row]))))));
				*/
				//Tabular
				result.AppendLine (latexTabularLabeledMatrixString(datasetSchemaText,
					Enumerable.Range (0, classCount).Select (row => 
				    	Enumerable.Range (0, classCount).Select (column => colorPercent(confusionMatrixScores[row, column] / fzeroToOne (scoreRowSums[row]), 1)))));
			}



			//Confidence Accuracy Tradeoff

			result.AppendLine (@"\subsection{Accuracy Analysis}" + "\n");

			result.AppendLine ("Here the collected data on the relationship between algorithm prediction strength value and accuracy are presented.");
			result.AppendLine ("The value output by the algorithm for prediction strength is only guaranteed to be monotonic with respect to the strength of the classification, by consulting this table, the accuracy of the classifier for various prediction strengths can be interpreted.");
			result.AppendLine ("Overall accuracies are presented first in unidimensional format, where neither true nor predicted class is considered (see below for their treatment).");

			result.AppendLine (@"\par\bigskip");
			result.AppendLine ("\n\n\\textbf{Accuracy by Prediction Strength}\n");
			result.AppendLine (latexMatrixString ("bmatrix", true, new[]{
				new[]{@"\text{E[Random]}", @"\mathbf{[0, 1]}"}.Concat (Enumerable.Range (0, bucketCount).Select (b => @"[" + (b * bucketSize).ToString("0.##") + ", " + ((b + 1) * bucketSize).ToString("0.##") + @"]")), //TODO: Pull this out to a function, make intervals properly.
				new[]{expectedAccuracyRandom, overallAccuracy}.Concat(Enumerable.Range (0, bucketCount).Select (b => accuracyByPredictedClassAndConfidence[0, b])).Select(item => colorPercent (item))
			}));

			//TODO: This should not be a row (row 0) of the matrix.  This duplicates information and unnecessarily complicates the contract...
			//TODO: Should show fractions instead of doubles?

			result.AppendLine (@"\par\bigskip");

			result.AppendLine ("Now a breakdown of prediction strength vs accuracy for all possible true and predicted classes is presented.");
			result.AppendLine ("Results are given both in terms of predicted class accuracy by prediction strength.");
			result.AppendLine ("Predicted class accuracy is perhaps a more valuable metric, as it allows a prediction to be assigned an accuracy value, but true class accuracy is also valuable in that it reveals the probability of classifying an element of a certain class correctly.");
			result.AppendLine ("Note that the first column represents all classes summed, and is thus identical to the array above." + "\n");
				
			
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				result.AppendLine (@"\par\bigskip");
				result.AppendLine ("\\textbf{Predicted Class Accuracy Analysis Matrix}:\n");
				
				result.AppendLine (latexMatrixString("bmatrix", true,
				    new[]{
						new []{@"\cdot", @"\begin{turn}{70} \textbf{All Classes} \end{turn}"}.Concat (datasetSchemaTextRotated), //Header row
						@"\mathbf{[0, 1]}".Cons((@"\mathbf{" + colorPercent (overallAccuracy) + "}").Cons(Enumerable.Range (0, classCount).Select (c => colorPercent(confusionMatrixCounts[c,c] / (double)countColumnSums[c])))), //[0, 1] row //TODO: sumscoreaccuracies
						new[]{"\t\\\\\n"} //Blank row
					}.Concat (
						Enumerable.Range (0, bucketCount).Select (
							b => (@"[" + (b * bucketSize).ToString("0.##") + ", " + ((b + 1) * bucketSize).ToString("0.##") + "]").Cons(
							Enumerable.Range (0, classCount + 1).Select (c => colorPercent(accuracyByPredictedClassAndConfidence[c, b]))))
					)
				));
			}

			
			result.AppendLine (@"\par\bigskip");
			result.AppendLine ("\\textbf{True Class Accuracy Analysis Matrix}:\n");

			
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				result.AppendLine (latexMatrixString("bmatrix", true,
				                                     
				    new[]{
						new []{@"\cdot", @"\begin{turn}{70} \textbf{All Classes} \end{turn}"}.Concat (datasetSchemaTextRotated), //Header row
						@"\mathbf{[0, 1]}".Cons((@"\mathbf{" + colorPercent (overallAccuracy) + "}").Cons(Enumerable.Range (0, classCount).Select (c => colorPercent(classCountAccuracies[c])))), //[0, 1] row
						new[]{"\t\\\\\n"} //Blank row
					}.Concat (
						Enumerable.Range (0, bucketCount).Select (
							b => (@"[" + (b * bucketSize).ToString("0.##") + ", " + ((b + 1) * bucketSize).ToString("0.##") + "]").Cons(
							Enumerable.Range (0, classCount + 1).Select (c => colorPercent(accuracyByTrueClassAndConfidence[c, b]))))
					)
				));
			}

			//May be good to have a space filling charts of correct vs incorrect classifications by confidence.

			//TODO: Insights section.

			
			result.AppendLine (@"\subsection{Insights}");
			result.AppendLine ("This subsection presents insights that can be gleaned from the above data.  It is particularly useful for large datasets, where it is difficult to interpret results presented in enormous matrices.");

			result.AppendLine(@"\par\bigskip");
			result.AppendLine (@"\textbf{Classifier Skew}");
			result.AppendLine (@"\begin{itemize}");
			result.AppendLine (Enumerable.Range (0, classCount).Select(
								i => new Tuple<string, double>(datasetSchema[i], (countColumnSums[i] - countRowSums[i]) / instancesClassifiedDouble)).Where (item => Math.Abs (item.Item2) > .02).OrderByDescending(item => Math.Abs (item.Item2)).Take(20).Select(
									tup => "The classifier is " + quantificationAdverbPhrase(Math.Abs (tup.Item2)) + " skewed " + ((tup.Item2 > 0) ? "toward" : "against") + " class \"" + tup.Item1 + "\" by " + colorPercent(Math.Abs (tup.Item2)) + ".").FoldToString("\t\\item ", "", "\t\\item "));
			result.AppendLine (@"\end{itemize}");

			result.AppendLine(@"\par\bigskip");
			result.AppendLine (@"\textbf{Interclass Confusion Bias Detection}");
			result.AppendLine (@"\begin{itemize}");
			result.AppendLine (Enumerable.Range (0, classCount).SelectMany(row => Enumerable.Range (0, classCount).Select (col => new Tuple<string, string, double>(datasetSchema[row], datasetSchema[col], confusionMatrixCounts[row, col] / (double) countRowSums[row]))).Where (tup => tup.Item1 != tup.Item2).Where(tup => tup.Item3 >= .02).OrderByDescending(tup => tup.Item3).Take(30) //Select the top n highest values not on the diagonal (greatest mistakes)
									.Select(tup => "Instances of class \"" + tup.Item1 + "\" are " + frequencyQuantificationAdverbPhrase(tup.Item3) + " mistaken for those of class \"" + tup.Item2 + "\" (This occurs in " + colorPercent (tup.Item3) + " of test instances).").FoldToString ("\t\\item ", "", "\t\\item ")); 
									//TODO, somehow ignore small classes being mistaken as larger ones, could be chance.  Better yet, add statistical significance of this bias.
			result.AppendLine (@"\end{itemize}");

			result.AppendLine(@"\par\bigskip");
			result.AppendLine (@"\textbf{Poor Class Performance Detection}");
			result.AppendLine (@"\begin{itemize}");
			result.AppendLine (Enumerable.Range (0, classCount).Select(
								i => new Tuple<string, double>(datasetSchema[i], classCountAccuracies[i])).Where (item => Math.Abs (item.Item2) < .5).OrderBy(item => Math.Abs (item.Item2)).Take(15).Select(
								tup => "Instances of class \"" + tup.Item1 + "\" are " + frequencyQuantificationAdverbPhrase(1 - tup.Item2) + " misclassified (" + colorPercent(Math.Abs (tup.Item2)) + " accuracy).  "
									+ ((tup.Item2 < expectedAccuracyRandom) ? (@"\textit{This performance is worse than that of random selection} (" + colorPercent (expectedAccuracyRandom) + ").") : ""))
									.FoldToString("\t\\item ", "", "\t\\item "));
			result.AppendLine (@"\end{itemize}");

			//TODO: % with + or -

			return result.ToString ();
		}

		
		public static string ClassificationReportLatexString<Ty> (this IFeatureSynthesizer<Ty> featureSynth, DiscreteSeriesDatabase<Ty> dataToClassify, string criterionByWhichToClassify)
		{
			dataToClassify = dataToClassify.Filter (item => !item.labels.ContainsKey (criterionByWhichToClassify));

			string[] schema = featureSynth.GetFeatureSchema();
			string[] schemaText = schema.Select (item => @"\text{" + limitLength (item, 20) + "}").ToArray ();
			string[] schemaTextRotated = schemaText.Select (item => @"\begin{turn}{70}" + item + @"\end{turn}").ToArray ();

			//filename, prediction class, prediction strength, prediction score vector.
			//TODO: Make this schema line up with the above as closely as possible.

			//TODO: AsParallel?
			IEnumerable<Tuple<string, string, double, double[]>> classifications = dataToClassify.data. /* AsParallel(). */ Select (item => 
				{
					double[] vals = featureSynth.SynthesizeFeaturesSumToOne(item);
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
		private static string latexMatrixString(string envname, bool mathmode, IEnumerable<IEnumerable<string>> data){
			StringBuilder result = new StringBuilder();
			result.AppendLine ((mathmode ? "$$" : "") + @"\begin{" + envname + "}");
			result.AppendLine (data.FoldToString (row => row.FoldToString("\t", "", " & "), "", "", " \\\\\n"));
			result.AppendLine (@"\end{" + envname + "}" + (mathmode ? "$$" : ""));
			return result.ToString ();
		}

		private static string labeledMatrixCorner = "\\cdot";
		private static string latexLabeledMatrixString(string envName, IList<string> labels, IEnumerable<IEnumerable<string>> data){
			return latexMatrixString (envName, true, labeledMatrixCorner.Cons (labels).Cons (data.Select ((row, index) => labels[index].Cons (row))));
		}

		/*
		private static string latexLabeledMatrixSplitIfNecessary(string envName, IList<string> labels, IEnumerable<IEnumerable<string>> data){
			string[] labels 
		}
		*/

		//TODO: Choice to include single lines.
		//TODO: Choice to rotate the labels
		private static string latexTabularLabeledMatrixString(IList<string> labels, IEnumerable<IEnumerable<string>> data){

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
		private static string tableEnvName = "tabular";
		private static string latexTabularString(IEnumerable<string> modes, IEnumerable<IEnumerable<string>> data){
			StringBuilder result = new StringBuilder();
			result.AppendLine (@"\begin{" + tableEnvName + "}{" + modes.FoldToString ("|", "|", " | ") + "}");
			result.AppendLine (data.FoldToString(row => row.FoldToString ("\t", " \\\\\n", " & "), "\t\\hline\n", "\t\\hline\n", "\t\\hline\n"));
			result.AppendLine (@"\end{" + tableEnvName + "}");
			return result.ToString ();
		}
		private static string latexTabularString(int count, IEnumerable<IEnumerable<string>> data){
			return latexTabularString(Enumerable.Range (0, count).Select (item => "c"), data);
		}


		//LONGTABLE
		private static string longTableEnvName = "longtable";
		private static string latexLongTableString(IEnumerable<string> modes, IEnumerable<string> header, IEnumerable<IEnumerable<string>> data){
			StringBuilder result = new StringBuilder();
			result.AppendLine (@"\begin{" + longTableEnvName + "}{" + modes.FoldToString ("|", "|", " | ") + "}");
			result.AppendLine (@"\hline");
			result.AppendLine (header.FoldToString ("\t", " \\\\", " & "));
			result.AppendLine (@"\hline\hline\endhead");
			result.AppendLine (data.FoldToString(row => row.FoldToString ("\t", " \\\\\n", " & "), "\t\\hline\n", "\t\\hline\n", "\t\\hline\n"));
			result.AppendLine (@"\end{" + longTableEnvName + "}");
			return result.ToString ();
		}


		private static string elipsis = "..."; //"…"; //TODO: Elipsis gets picked up as nonprintable.
		public static string limitLength(string s, int lim){
			if(s.Length > lim){
				return (s.Substring (0, lim - 1) + elipsis).Replace (@"\" + elipsis, elipsis); //TODO: Think about this.
			}
			return s;
		}

		//Make the top n words into a safe latex string.
		private static string seriesToSafeString<Ty>(DiscreteEventSeries<Ty> series, int n){
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
		private static int BreakLine(string text, int pos, int max)
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

		private static string[] quantificationAdverbPhrases = "infinitessimally;ever so slightly;a tiny bit;slightly;noticably;somewhat;quite;very;heavily;quite heavily;extremely;near completely".Split(';');
		private static string quantificationAdverbPhrase(double d){
			if(d < 0.0001) return "not";
			if(d > 0.999) return "fully";
			return quantificationAdverbPhrases[(int)(d * quantificationAdverbPhrases.Length)];
		}

		private static string[] frequencyQuantificationAdverbPhrases = "infinitessimally infrequently;virtually never;intermittently;rarely;occasionally;infrequntly;sometimes;often;quite often;frequently;more often than not;usually;quite frequently;nearly constantly;almost always".Split (';');
		//English language functions
		private static string frequencyQuantificationAdverbPhrase(double d){
			if(d < .0001){
				return "never";
			}
			if(d > .9999){
				return "always";
			}
			return frequencyQuantificationAdverbPhrases[(int)(d * quantificationAdverbPhrases.Length)];
		}

		private static string ordinal (int num, bool preferNumeric)
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

