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
			document.AppendLine (@"\end{document}");
		}

		public void Write(string path){
			Write (path, a => a);
		}
		
		public void Write(string path, Func<String, String> process){
			File.WriteAllText (path, process(document.ToString ()));
			//TODO: Invoke latex.
		}

		public static void WriteLatexDocument(string title, string author, double margin, double width, double height, string body, string path, Func<String, String> process){
			LatexDocument doc = new LatexDocument(title, author, margin, width, height);
			doc.Append (body);
			doc.AppendClose ();
			doc.Write (path, process);
		}
	}
	

	//TODO: Parallelize the various parts of this function.
	public static class WriteupGenerator{

		public static void ProduceClassifierComparisonWriteup<Ty> (string documentTitle, string author, double width, double height, string outDirectory, Tuple<string, IEventSeriesProbabalisticClassifier<Ty>>[] classifiers, string datasetTitle, DiscreteSeriesDatabase<Ty> dataset, string criterionByWhichToClassify, int classificationRounds, string[] analysisCriteria = null, IFeatureSynthesizer<Ty> synthesizer = null)
		{

			Console.WriteLine ("Producing classifier comparison report:");
			
			string thisPath;
			LatexDocument thisDoc;

			{
				string latexProgram = "pdflatex";

				//Create directory and populate with a latex make script.
				Directory.CreateDirectory (outDirectory);
				string[] latexFiles = "database;featuresynthesizer;classifiercomparison;classifiers;fullreport".Split (';'); //TODO: Consider featuresynthesizer.  Should it be optional?  If so it's entry in the makefile should be removed when not present.
				File.WriteAllLines (outDirectory + "make.sh", 
				    new[]{"#! /bin/bash", "", "bufsize = 10000000"}.Concat (
					latexFiles.Select (item => latexProgram + " -draftmode " + item + ".tex" + " &")).Concat (
					new[]{"wait", ""}).Concat (
					latexFiles.Select (item => latexProgram + " " + item + ".tex" + " &")).Concat (
					new[]{"wait", "", "rm -f *.aux *.lof *.log *.out *.toc"}
				)
				);
				//TODO make executable.
				Console.WriteLine ("Wrote make script.");
			}

			dataset = dataset.FilterForCriterion (criterionByWhichToClassify);
			Console.WriteLine ("Filtered dataset for items labeled with criterion \"" + criterionByWhichToClassify + "\"");

			string databaseString = dataset.DatabaseLatexString (datasetTitle + " Database Analysis", (analysisCriteria == null) ? new[]{criterionByWhichToClassify} : analysisCriteria, (int)width - 2);
			Console.WriteLine ("Generated database report string.");
			{
				thisDoc = new LatexDocument ("Database Report", author, .8, width, height);
				thisPath = outDirectory + "database.tex";
				thisDoc.Append (databaseString);
				thisDoc.AppendClose ();
				thisDoc.Write (thisPath, FinalSanitize);
				Console.WriteLine ("Wrote database report to \"" + thisPath + "\".");
			}

			string featureSynthesizerString = ""; //Unnecessary initialization.
			if (synthesizer != null) {
				featureSynthesizerString = LatexExtensions.FeatureAnalysisLatexString (synthesizer, dataset, criterionByWhichToClassify);
				Console.WriteLine ("Generated feature synthesizer report string.");
				{
					thisDoc = new LatexDocument ("Feature Synthesizer Report", author, .8, width, height);
					thisPath = outDirectory + "featuresynthesizer.tex";
					thisDoc.Append (featureSynthesizerString);
					thisDoc.AppendClose ();
					thisDoc.Write (thisPath, FinalSanitize);
					Console.WriteLine ("Wrote feature synthesizer report to \"" + thisPath + "\".");
				}
			}

			//Accuracy analysis:
			//Analysis, classifier string, accuracy analysis string
			Tuple<ClassifierAccuracyAnalysis<Ty>, string, string>[] analyses = classifiers.AsParallel ().WithExecutionMode(System.Linq.ParallelExecutionMode.ForceParallelism).WithDegreeOfParallelism(classifiers.Length)
				.Select (classifier => new ClassifierAccuracyAnalysis<Ty> (
					classifier.Item2, classifier.Item1, dataset, criterionByWhichToClassify, .8, classificationRounds, .05).runAccuracyAnalysis ()
			)
				.Select (analysis => new Tuple<ClassifierAccuracyAnalysis<Ty>, string, string> (
				    analysis, 
					analysis.classifier.ClassifierLatexString (analysis.classifierName, (int)((width - 1.6) * 13.5)),
					analysis.latexAccuracyAnalysisString (@"\subsection", @"\subsubsection"))
			)
				.OrderByDescending (tup => tup.Item1.overallAccuracy)
				.ToArray ();
			Console.WriteLine ("Accuracy Analysis of all classifiers (" + classifiers.Length + ") complete.");

			string classifierSections;
			{
				StringBuilder sb = new StringBuilder ();
			
				//Create a section for each classifier.
				foreach (var classifierAnalysis in analyses) {
					sb.AppendLine (@"\section{Classifier " + classifierAnalysis.Item1.classifierName + "}");
					sb.AppendLine (LatexExtensions.latexLabelString ("sec:classifier " + classifierAnalysis.Item1.classifierName));
					sb.AppendLine (classifierAnalysis.Item2);
					sb.AppendLine (classifierAnalysis.Item3);
				}
				classifierSections = sb.ToString ();
			}
			Console.WriteLine ("Generated classifier accuracy report strings.");
			{
				thisDoc = new LatexDocument ("Accuracy Report by Classifier", author, .8, width, height);
				thisPath = outDirectory + "classifiers.tex";
				thisDoc.Append (featureSynthesizerString);
				thisDoc.AppendClose ();
				thisDoc.Write (thisPath, FinalSanitize);
				Console.WriteLine ("Wrote classifier accuracy report to \"" + thisPath + "\".");
			}

			string classifierComparisonString = LatexExtensions.ClassifierComparisonLatexString (analyses.Select (analysis => analysis.Item1).ToArray ());
			Console.WriteLine ("Generated classifier comparison string.");
			{
				thisDoc = new LatexDocument ("Classifier Comparison Report", author, .8, width, height);
				thisPath = outDirectory + "classifiers.tex";
				thisDoc.Append (classifierComparisonString);
				thisDoc.AppendClose ();
				thisDoc.Write (thisPath, FinalSanitize);
				Console.WriteLine ("Wrote classifier comparison report to \"" + thisPath + "\".");
			}

			//Full Report
			{
				thisDoc = new LatexDocument (documentTitle, author, .8, width, height);
				thisPath = outDirectory + "fullreport.tex";

				thisDoc.Append (@"\part{Problem Analysis}");

				thisDoc.Append (@"\section{Dataset Overview}");
				thisDoc.Append (databaseString);

				if (synthesizer != null) {
					thisDoc.Append (@"\section{Feature Analysis}");
					thisDoc.Append (featureSynthesizerString);
				}

				thisDoc.Append (@"\part{Classifier Analysis}");

				thisDoc.Append (@"\section{Classifier Comparison}");
				thisDoc.Append (classifierComparisonString);

				thisDoc.Append (classifierSections);

				thisDoc.AppendClose ();
			
				thisDoc.Write (thisPath, FinalSanitize); //s => s.RegexReplace (@"[^\u0000-\u007F\u0080-\u0099]", string.Empty));
				Console.WriteLine ("Wrote full report to \"" + thisPath + "\"");
			}

		}

		public static void ProduceClassificationReport<Ty> (string documentTitle, string author, double width, double height, string outDirectory, IEventSeriesProbabalisticClassifier<Ty> classifier, string classifierName, DiscreteSeriesDatabase<Ty> dataset, string datasetTitle, string criterionByWhichToClassify, int accuracyIterations = 4)
		{

			Console.WriteLine ("Producing classification report:");

			string thisPath;
			LatexDocument thisDoc;

			{
				string latexProgram = "pdflatex";

				//Create directory and populate with a latex make script.
				Directory.CreateDirectory (outDirectory);
				string[] latexFiles = "database;classifieroverview;classifications;classifieraccuracy;fullreport".Split (';');
				File.WriteAllLines(outDirectory + "make.sh", 
				    new[]{"#! /bin/bash", "", "bufsize = 10000000", ""}.Concat (
					latexFiles.Select (item => latexProgram + " -draftmode " + item + ".tex" + " &")).Concat (
					new[]{"wait", ""}).Concat (
					latexFiles.Select (item => latexProgram + " " + item + ".tex" + " &")).Concat (
					new[]{"wait", "", "rm -f *.aux *.lof *.log *.out *.toc"}
					)
				);
				//TODO make executable.
				Console.WriteLine ("Wrote make script.");
			}
			
			string databaseString = dataset.DatabaseLatexString (datasetTitle + " Database Analysis", new[]{criterionByWhichToClassify}, (int)(width - 2));
			Console.WriteLine ("Generated Database Report...");
			{
				thisDoc = new LatexDocument ("Database Report", author, .8, width, height);
				thisPath = outDirectory + "database.tex";
				thisDoc.Append (databaseString);
				thisDoc.AppendClose ();
				thisDoc.Write (thisPath, FinalSanitize);
				Console.WriteLine ("Wrote database report to \"" + thisPath + "\".");
			}

			classifier.Train (dataset);
			Console.WriteLine ("Trained Classifier...");

			string classifierString = classifier.ClassifierLatexString ("Author Classifier", (int)((width - 1.6) * 13));
			Console.WriteLine ("Generated Classifier Overview...");
			{
				thisDoc = new LatexDocument ("Database Report", author, .8, width, height);
				thisPath = outDirectory + "classifieroverview.tex";
				thisDoc.Append (classifierString);
				thisDoc.AppendClose ();
				thisDoc.Write (thisPath, FinalSanitize);
				Console.WriteLine ("Wrote classifier overview report to \"" + thisPath + "\".");
			}

			string classificationReportString = classifier.ClassificationReportLatexString (dataset, criterionByWhichToClassify, 0);
			Console.WriteLine ("Generated Document Classification Report...");
			{
				thisDoc = new LatexDocument ("Classification Report", author, .8, width, height);
				thisPath = outDirectory + "classifications.tex";
				thisDoc.Append (classificationReportString);
				thisDoc.AppendClose ();
				thisDoc.Write (thisPath, FinalSanitize);
				Console.WriteLine ("Wrote classification report to \"" + thisPath + "\".");
			}
			//TODO classification text file.

			string classifierAccuracyReportString = classifier.ClassifierAccuracyLatexString (classifierName, dataset, criterionByWhichToClassify, .8, accuracyIterations, .05);
			Console.WriteLine ("Created Classifier Accuracy Report...");
			{
				thisDoc = new LatexDocument ("Classifier Accuracy Report", author, .8, width, height);
				thisPath = outDirectory + "classifieraccuracy.tex";
				thisDoc.Append (classifierAccuracyReportString);
				thisDoc.AppendClose ();
				thisDoc.Write (thisPath, FinalSanitize);
				Console.WriteLine ("Wrote classifier accuracy report to \"" + thisPath + "\".");
			}

			//Full Report
			{
				LatexDocument fullReport = new LatexDocument (documentTitle, author, .8, width, height);

				fullReport.Append ("\\section{Input Data Overview}\n\n");
				fullReport.Append (databaseString);

				fullReport.Append ("\\section{Feature Synthesizer and Classifier Overview}\n\n");
				fullReport.Append (classifierString);

				fullReport.Append ("\\section{Classification Report}\n\n");
				fullReport.Append (classificationReportString);

				fullReport.Append ("\\section{Classifier Accuracy Report}\n\n");
				fullReport.Append (classifierAccuracyReportString);

				fullReport.AppendClose ();
				fullReport.Write (outDirectory + "fullreport.tex", FinalSanitize);

				Console.WriteLine ("Wrote full classifier report.");
			}

			//Run Latex

			//TODO:
			// execute outDirectory + "make.sh"
		}

		public static string FinalSanitize(string s){
			return AsciiOnly(s, false).RegexReplace ("[²½Ã¢©Ââ¦¬¯]", "");
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

\usepackage{multicol}

\usepackage{relsize}

\usepackage[ampersand]{easylist}


" + "\\usepackage[margin=" + margin + "in, paperwidth=" + width + "in, paperheight=" + height + "in]{geometry}\n"

 + "\\title{" + title + "}\n\\author{" + author + "}\n\n" +

@"
\setcounter{MaxMatrixCols}{200}
" + 

@"
\begin{document}

\maketitle
\tableofcontents
%\listoffigures
\pagebreak[4]


";
		}

		public static string DatabaseLatexString<Ty> (this DiscreteSeriesDatabase<Ty> db, string dbName, IEnumerable<string> criteriaToEnumerate, int firstN, string objName = "word")
		{

			//If not provided criteria, use all.
			if (criteriaToEnumerate == null || criteriaToEnumerate.IsEmpty ()) {
				criteriaToEnumerate = db.data.SelectMany (item => item.labels.Keys).Distinct ().Where (item => item != "filename");
			}

			Tuple<string, string[]>[] criterionInformation = db.getLabelCriteria ().Select (criterion => new Tuple<string, string[]> (criterion, db.data.Select (item => item.labels.GetWithDefault (criterion)).Distinct ().Where (val => val != null).Order ().ToArray ())) /* .Where (tup => tup.Item2.Length > 0) */.ToArray ();
			int criteriaCount = criterionInformation.Length;

			StringBuilder result = new StringBuilder ();

			result.AppendLine ("The database contains " + englishCountOfString (objName, db.TotalItemCount ()) + " across " + englishCountOfString ("document", db.data.Count) + ".");
			result.AppendLine ("Documents are labeled along " + englishCountOfString ("criterion", criteriaCount) + ".");

			result.AppendLine (
				criterionInformation.Select (
					info => "Classification criterion " + latexHyperrefString("enum:criterion:" + info.Item1, latexClassString(info.Item1)) + " contains " + info.Item2.Length + " categories.  "
				+ ((info.Item2.Length <= 20) ? ((info.Item2.Length == 1 ? "It is" : "They are") + " " + foldToEnglishList (info.Item2.Select (item => latexHyperrefString("enum:criterion:" + info.Item1 + ":class:" + item, latexClassString(item)))) + ".  ") : "")
			).FoldToString ("", "", "")
			);

			result.AppendLine ("\\subsection{" + dbName + " instance overview}\n");

			DiscreteEventSeries<Ty>[] dbInstances = db.data.OrderBy (a => a.labels ["filename"]).ToArray (); //Sort instances.

			int maxToEnumerate = 10000;
			if (db.data.Count > maxToEnumerate) {
				result.AppendLine (@"\textbf{Database instance overview omitted because over " + maxToEnumerate + " entries exist.}\n");
			} else {
				//result.AppendLine ("\\subsubsection{All Dataset Entries}\n");

				result.AppendLine ("Here all entries in the dataset are presented, along with the first " + englishCountOfString (objName, firstN) + " of the entry and their classes by all available criteria.");

				int cols = 1; //TODO: Calculate this based on string lengths and page width
				if (cols > 1) result.AppendLine (@"\begin{multicols}{" + cols + "}");
				result.AppendLine ("\\begin{enumerate}[1.]");
				result.AppendLine ("\\itemsep0pt"); //TODO: Put this in header?

				foreach (DiscreteEventSeries<Ty> item in dbInstances) {
					result.AppendLine ("\\item " + item.labels ["filename"] + " (" + englishCountOfString (objName, item.data.Length) + ")" 
						+ " $\\in$ " + foldToEnglishList (item.labels.Keys.Where (key => key != "filename").Select (key => key + ":" + item.labels [key])) + ".  ``"
						+ seriesToSafeString (item, firstN) + "''." + latexLabelString ("enum:dsoverview:" + item.labels["filename"])
					);
				}
				result.Append ("\\end{enumerate}\n");
				if (cols > 1)
					result.AppendLine (@"\end{multicols}");
			}
			
			result.AppendLine ("\\subsection{" + dbName + " class overview}\n");

			maxToEnumerate = 20000;
			if (db.data.Count > maxToEnumerate) {
				result.AppendLine (@"\textbf{Class overview omitted because over " + db.data.Count + " instances exist.}\n");
			}
			else{
				//result.Append ("\\subsubsection{" + dbName + " categories}\n");
				
				int cols = 3; //TODO: Calculate this based on string lengths and page width
				if (cols > 1) result.AppendLine (@"\begin{multicols}{" + cols + "}");
				result.Append ("\\begin{enumerate}[1.]\n");

				foreach(string key in criteriaToEnumerate.OrderBy (item => item)){
					result.AppendLine (@"\item " + key + "(" + dbInstances.Where (item => item.labels.ContainsKey (key)).Count() + " labeled entries):");
					result.AppendLine (latexLabelString ("enum:criterion:" + key ));
					result.AppendLine (@"\begin{enumerate}[I.]");
					result.AppendLine (dbInstances.GroupBy (item => item.labels.GetWithDefault(key, "\\texttt{none}")) //Group by category
					    .OrderBy (item => item.Key == "\\texttt{none}" ? 1 : 0).ThenBy (item => item.Key) //Order by name, with none last
						.FoldToString (item => item.Key + " (" + item.Count() + " entries, " + englishCountOfString(objName, item.Select (subitem => subitem.data.Length).Sum()) + ")\n" //Count words per category;
					   		+ "  " + latexLabelString ("enum:criterion:" + key + ":class:" + item.Key) + "\n"
					    	+ item.FoldToString (subitem => latexHyperrefString("enum:dsoverview:" + subitem.labels["filename"], subitem.labels["filename"]) + " (" + subitem.data.Length + " words)", "\\begin{enumerate}[i.]\n  \\item ", "\n\\end{enumerate}\n", "\n  \\item "), "\\item ", "\n" , "\n\\item ")); //Show each item in category.
					result.AppendLine (@"\end{enumerate}");
				}
				result.AppendLine (@"\end{enumerate}");
				if (cols > 1) result.AppendLine (@"\end{multicols}");
			}


			//Insights
			result.AppendLine ("\\subsection{Insights}");

			Tuple<string, Tuple<string, DiscreteEventSeries<Ty>[]>[]>[] dataByCriterionAndClass = criteriaToEnumerate.Select (criterion =>
				new Tuple<string, Tuple<string, DiscreteEventSeries<Ty>[]>[]>(
					criterion, db.data.Where (item => item.labels.ContainsKey(criterion)).GroupBy (item => item.labels[criterion])
					.Select (grp => new Tuple<string, DiscreteEventSeries<Ty>[]>(grp.Key, grp.ToArray ()))
				.OrderBy (tup => tup.Item1)
				.ToArray ()

				)
			).OrderBy (tup => tup.Item1).ToArray();

			//Overview across all criteria:

			result.AppendLine (@"\subsubsection{Aggregate Statistics over all Criteria}");
			result.AppendLine (latexLongTableString (
				"l |".Cons (Enumerable.Range (0, 6).Select (i => "c")), //Format
				@"Criterion Name;Class Count;Min Class Size\tablefootnote{All class sizes refer to instance counts, not sum event counts.  See the following subsection for detailed reports on event counts by class.};Max Class Size;Mean Class Size;Stdev Class Size".Split (';'), //Header
			    dataByCriterionAndClass.Select (row => new[]{
					latexHyperrefString("enum:criterion:" + row.Item1, row.Item1), 
					row.Item2.Length.ToString (), 
					row.Item2.Select (@class => @class.Item2.Length).Min ().ToString (), 
					row.Item2.Select (@class => @class.Item2.Length).Max ().ToString (), 
					row.Item2.Select (@class => @class.Item2.Length).Average ().ToString (formatString), 
					row.Item2.Select (@class => (double)@class.Item2.Length).Stdev().ToString(formatString)
				})
			));

			//Intercriteria class correlation
			//This section is useful for discovering that:
			//Stories written by author A are often about subject B, or even that documents written about subject C were often written during time period D.

			int minClassSize = 3;
			double significantDifference = .1;
			int correlationsToTake = 50;
			//Criterion A, class A, criterion B, class B, double % A that are also in B, double expected %A that are also in B under random hypothesis, class A size, class B size
			IEnumerable<TupleStruct<string, string, string, string, double, double, int, int>> allCorrelations = 
				dataByCriterionAndClass.SelectMany (
					criterion1 => criterion1.Item2.SelectMany(
						class1 => (class1.Item2.Length < minClassSize) ? new EmptyList<TupleStruct<string, string, string, string, double, double, int, int>>() : dataByCriterionAndClass.SelectMany(
							criterion2 => (criterion1.Item1 == criterion2.Item1) ? new EmptyList<TupleStruct<string, string, string, string, double, double, int, int>>() : criterion2.Item2.Select(
								class2 => new TupleStruct<string, string, string, string, double, double, int, int>(
									criterion1.Item1, 
									class1.Item1, 
									criterion2.Item1, 
									class2.Item1,
									class2.Item2.Where (instance => instance.labels.GetWithDefault (criterion1.Item1, (string)null) == class1.Item1).Count () / (double)class2.Item2.Length,
									class1.Item2.Length / (double)criterion1.Item2.Sum (@class => @class.Item2.Length),
									class1.Item2.Length,
									class2.Item2.Length
								)
			))));
			TupleStruct<string, string, string, string, double, double, int, int>[] significantCorrelations = allCorrelations.Where (tup => 
				tup.Item8 > minClassSize //Item 7 is already checked
				&& Math.Abs (tup.Item5 - tup.Item6) > significantDifference //TODO statistical significance.
			).OrderByDescending (tup => Math.Abs (tup.Item5 - tup.Item6)).Take (correlationsToTake).ToArray (); 

			if(significantCorrelations.Length > 0){
				result.AppendLine (@"\subsubsection{Intercriterion Class Correlations}");
				
				int cols = 1; //TODO: Calculate this based on string lengths and page width
				if (cols > 1) result.AppendLine (@"\begin{multicols}{" + cols + "}");
				result.AppendLine (@"\begin{enumerate}[1.]");

				/*
				result.AppendLine (significantCorrelations.FoldToString (
					corr => "Instances of class " + corr.Item1 + ":" + corr.Item2 + "(" + corr.Item7 + ")" + " are found in " + colorPercent(corr.Item5) + " of " + "class " + corr.Item3 + ":" + corr.Item4 + "(" + corr.Item8 + ").  " + "Expected " + colorPercent (corr.Item6) + ".",
					@"\item ", "", "\n\\item "));
				*/

				result.AppendLine (significantCorrelations.FoldToString (
					corr => 
						"Class " + corr.Item1 + ":" + corr.Item2 + "(" + corr.Item7 + ")" + " is " + quantificationAdverbPhrase(Math.Pow (Math.Abs (corr.Item5 - corr.Item6), .9)) +
						" " + ((corr.Item5 < corr.Item6) ? "under" : "over") + "represented in class " + corr.Item3 + ":" + corr.Item4 + "(" + corr.Item8 + ").  " + "(Found " + colorPercent(corr.Item5) + ", expected " + colorPercent (corr.Item6) + ").",
					@"\item ", "", "\n\\item "));

				result.AppendLine (@"\end{enumerate}");
				if (cols > 1) result.AppendLine (@"\end{multicols}");
    	    }

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
					new[]{@"\textbf{Average Class}", meanInstanceCount.ToString (formatString),  (totalWordCount / (double)classCount).ToString (formatString), (totalWordCount / (double)totalInstanceCount).ToString (formatString)}.Cons ( //All row
						Enumerable.Range (0, classCount).Select (classIndex => 
					    	new[]{
								latexHyperrefString("enum:criterion:" + criterionData.Item1 + ":class:" + criterionData.Item2[classIndex].Item1, criterionData.Item2[classIndex].Item1),
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
						result.AppendLine (englishCapitolizeFirst(englishCountOfString("this class is", largeClasses.Length)) + " larger than " + largeLim + " times the mean class size.\n"); //TODO sing/plur
						
						result.AppendLine (@"\begin{itemize}");
						result.AppendLine (largeClasses.Select (item => "Class " + item.Item1 + " contains " + item.Item2 + " instances, which is " + (item.Item2 / meanInstanceCount).ToString (formatString) + " times the average.").FoldToString("\t\\item ", "", "\t\\item "));
						result.AppendLine (@"\end{itemize}");

						result.AppendLine ();
					}

					if(smallClasses.Length > 0){
						result.AppendLine (@"\textbf{Undersized classes}" + "\n");
						result.AppendLine (englishCapitolizeFirst(pluralPhrase("this class is", smallClasses.Length)) + " smaller than " + smallLim + " times the mean class size.\n"); //TODO sing/plur
						
						result.AppendLine (@"\begin{itemize}");
						result.AppendLine (smallClasses.Select (item => "Class " + item.Item1 + " contains " + item.Item2 + " " + (item.Item2 == 1 ? "instance" : "instances")+ ", which is " + (item.Item2 / meanInstanceCount).ToString (formatString) + " times the average.").FoldToString("\t\\item ", "", "\t\\item "));
						result.AppendLine (@"\end{itemize}");

						result.AppendLine ();
					}
				}
			}

			return result.ToString ();
		}

		public static string ClassifierLatexString<Ty>(this IEventSeriesProbabalisticClassifier<Ty> classifier, string classifierName, int lineCols){
			StringBuilder result = new StringBuilder();

			result.Append ("\\subsection{" + "Classifier Parameters and Trained Model Report" + "}\n");
			
			result.Append (latexLabelString ("sec:classifier:model " + classifierName));
			
			//result.AppendLine ("Here relevant information is provided on the classifier used to generate the report.");
			//result.AppendLine ("Generally speaking, the type of the classifier and all parameters are given in the first line, and a complete report of all information learned from training data follows."); // "\\footnote{On a technical note, the reason for the clear difference in representation quality between this section and the remainder of the report is that this section is generated from a string produced by implemetations of the \\texttt{IEventSeriesProbabalisticClassifier<string>} interface, whereas the remaining sections involving the classifier use only the public contract to obtain their data.}");


			//string fsynthstr = WordWrap(classifier.ToString (), lineCols);

			/*
			result.Append ("\\begin{verbatim}\n");
			result.Append (fsynthstr);
			result.Append ("\n\\end{verbatim}\n");
			*/

			/*
			result.Append("MODERN TEXT OUTPUT:\n");
			result.Append ("\\begin{verbatim}\n");
			result.Append (WordWrap (AlgorithmReflectionExtensions.UntrainedModelString(classifier), lineCols));
			result.Append ("\n\\end{verbatim}\n");
			*/
			//result.AppendLine ("LATEX OUTPUT:\n");
			result.AppendLine (AlgorithmReflectionExtensions.UntrainedModelLatexString(classifier));


			//TODO: Accuracy profiling.

			return result.ToString ();

		}

		public static string ClassifierComparisonLatexString<Ty>(ClassifierAccuracyAnalysis<Ty>[] analyses)
		{
			
			//Shared confusion matrix calculation:
			int classifierCount = analyses.Count ();
			int classCount = analyses.First ().datasetSchema.Length;

			double[,] sharedConfusionMatrix = new double[classifierCount, classifierCount];
			for (int i = 0; i < classifierCount; i++) {
				sharedConfusionMatrix [i, i] = Double.NaN;
				for (int j = i + 1; j < classifierCount; j++) {

					double sharedConfusion = 0;
					//Sum the shared confusion of confusion matrices i and j


					for (int ii = 0; ii < classCount; ii++) {
						for (int jj = 0; jj < classCount; jj++) {
							if (ii == jj)
								continue;
							double val = analyses [i].confusionMatrixScores [ii, jj] * analyses [j].confusionMatrixScores [ii, jj];
							if (val > 0)
								sharedConfusion += val.Sqrt ();
						}
					}
					sharedConfusionMatrix [i, j] = sharedConfusionMatrix [j, i] = sharedConfusion;
				}
			}

			//Generate colors for classifiers
			Func<string, string>[] classifierColorers = Enumerable.Range (0, classifierCount).Select ((a) => getRandomColorer (.8)).ToArray ();




			StringBuilder result = new StringBuilder ();
			result.AppendLine (@"\subsection{Classifier Comparison Overview}");

			//Overview
			result.AppendLine ("Over a set of " + analyses [0].labeledData.data.Count + " labeled instances, " + analyses.Length + " classifiers were compared.  In this experiment, a training data to test data ratio of " + analyses [0].trainSplitFrac.ToString (LatexExtensions.formatString) + " (" + ((int)(analyses [0].trainSplitFrac * analyses [0].labeledData.data.Count ())) + " training, " + (analyses [0].labeledData.data.Count - ((int)(analyses [0].trainSplitFrac * analyses [0].labeledData.data.Count ()))) + " test instances) was used.");
			result.AppendLine ("The process was repeated " + LatexExtensions.englishCountOfString ("time", analyses [0].iterations) + ", for a total of " + analyses [0].classificationInstances.Count + " classifications for each classifier.");

			result.AppendLine (analyses.Length.ToString () + " classifiers were compared.  An overview of the results is presented here.\n");

			result.AppendLine (LatexExtensions.latexLongTableString (
				"l|;c;c".Split (';'),
				"Classifier Name;Classifier Rank;Overall Accuracy".Split (';'),
				new[]{
					new Tuple<string, double> (@"\textcolor[gray]{0.2}{E$[$Random Selection$]$}", analyses [0].expectedAccuracyRandom),
					new Tuple<string, double> (@"\textcolor[gray]{0.2}{Top Class Selection}", analyses [0].topClassSelectionAccuracy)
				}.Concat (analyses.Zip (classifierColorers, (analysis, colorer) => new Tuple<string, double> (latexHyperrefString("sec:classifier " + analysis.classifierName, colorer (analysis.classifierName)), analysis.overallAccuracy))).OrderByDescending (tup => tup.Item2).Select ((tup, index) => new[] {
				tup.Item1,
				(index + 1).ToString (),
				LatexExtensions.colorPercent (tup.Item2)
			})
			)
			);


			result.AppendLine (@"\pagebreak[3]");
			//doc.Append (@"\begin{samepage}"); //samepage and multicols don't play nicely.

			result.AppendLine (@"\subsection{Classifier Comparison Shared Confusion Matrix}");
			
			result.AppendLine (@"For classes $A, B,$ with score confusion matrices $\mathbf{A}, \mathbf{B}$, shared confusion is defined as $$\sum_{i = 1}^{|\text{classes}|}\sum_{j \in (1, \hdots, |\text{classes}|) \setminus (i)} \sqrt{\min(0, \textbf{A}_{ij} * \textbf{B}_{ij})}$$");
			result.AppendLine ("");


			//Colorer key:

			result.AppendLine ("Classifier color key:");
			result.AppendLine (@"\begin{multicols}{2}"); //TODO: Calculate number of columns?
			result.AppendLine (@"\begin{itemize}");
			result.AppendLine (analyses.Zip (classifierColorers, (analysis, colorer) => @"  \item " + colorer (analysis.classifierName)).FoldToString ("", "", "\n"));
			result.AppendLine (@"\end{itemize}");
			result.AppendLine (@"\end{multicols}");


			/*
			doc.Append (LatexExtensions.latexLabeledMatrixString(
				"bmatrix", 
				classifiers.Select (classifier => @"\text{" +classifier.Item1 + "}").ToArray (), 
				sharedConfusionMatrix.EnumerateRows ().Select (row => row.Select (cell => LatexExtensions.colorDouble(cell)))));
			*/

			double matrixMax = sharedConfusionMatrix.EnumerateRows ().Flatten1 ().Where (item => !Double.IsNaN (item)).Max (); //TODO: cleaner expression.  Enumerate the matrix in one go?

			result.AppendLine (LatexExtensions.latexTabularLabeledMatrixString (
				analyses.Zip (classifierColorers, (analysis, colorer) => colorer (LatexExtensions.limitLength (analysis.classifierName, 20))).ToArray (), 
				sharedConfusionMatrix.EnumerateRows ().Select (row => row.Select (cell => LatexExtensions.colorDouble (cell, matrixMax))), 90)
			);

			//doc.Append (@"\end{samepage}");

			return result.ToString ();
		}

		//HELPER

		public static double fzeroToOne(double d){
			return (d == 0) ? 1 : d;
		}

		public static string ClassifierAccuracyLatexString<Ty> (this IEventSeriesProbabalisticClassifier<Ty> featureSynth, string classifierName, DiscreteSeriesDatabase<Ty> labeledData, string criterionByWhichToClassify, double trainSplitFrac, int iterations, double bucketSize)
		{
			ClassifierAccuracyAnalysis<Ty> analysis = new ClassifierAccuracyAnalysis<Ty>(featureSynth, classifierName, labeledData, criterionByWhichToClassify, trainSplitFrac, iterations, bucketSize);
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

		public static string FeatureAnalysisLatexString<Ty>(IFeatureSynthesizer<Ty> synth, DiscreteSeriesDatabase<Ty> data, string criterion){

			IEnumerable<DiscreteEventSeries<Ty>> labeledSeries = data.Where (item => item.labels.ContainsKey (criterion)).ToArray ();

			//Train the synth on the input data.
			synth.Train (data); //TODO: This training data is used to create the labeled instances.
			string[] schema = synth.GetFeatureSchema();

			int cutoff = 1000;
			if(schema.Length > cutoff){
				return "\\subsection{Feature Overview}\n\textbf{Omitted, " + schema.Length + " features found.  Cutoff is " + cutoff + "}";
			}
			string[] classes = labeledSeries.Select (item => item.labels[criterion]).Distinct ().ToArray();

			//Run the synth on all data, synthesize features.
			Tuple<string, double[]>[] labeledInstances = labeledSeries.Select (item => new Tuple<string, double[]>(item.labels[criterion], synth.SynthesizeFeatures(item))).ToArray();

			//Calculate statistics over feature data.
			//min, max, mean, stdev
			double[][] featureStats = Enumerable.Range(0, schema.Length).Select(featureIndex =>
				{
					double[] vals = labeledInstances.Select(item => item.Item2[featureIndex]).ToArray();
					double min = vals.Min();
					double max = vals.Max();
					double mean = vals.Average();
					double stdev = vals.Stdev(mean);
					//if(stdev == 0){
					//	stdev = 1;
					//}
					return new[]
					{
						min, max, mean, stdev
					};
				}
			).ToArray ();

			//Normalize the labeled instances
			foreach(Tuple<string, double[]> instance in labeledInstances){
				for(int i = 0; i < schema.Length; i++){
					instance.Item2[i] = (instance.Item2[i] - featureStats[i][2]); //Mean to 0
					if(featureStats[i][3] != 0) instance.Item2[i] /= featureStats[i][3]; //Fix stdev
				}
			}

			//Group features by label.
			IGrouping<string, double[]>[] groupedLabeledInstances = labeledInstances.GroupBy (item => item.Item1, item => item.Item2).ToArray ();
			Dictionary<string, int> classSizes = groupedLabeledInstances.ToDictionary (grp => grp.Key, grp => grp.Count ());

			Tuple<string, Perceptron>[] perceptronsByClass = classes.AsParallel ().Select (perceptronClass => 
			    {
				    TupleStruct<double[], int, double>[] thisPerceptronTrainingData = groupedLabeledInstances.SelectMany (grp =>
						grp.Select (instance => new TupleStruct<double[], int, double>(instance, (grp.Key == perceptronClass) ? 1 : -1, classSizes[grp.Key]))).ToArray ();
					
					Perceptron p = new Perceptron(schema.Length);
					p.Train(thisPerceptronTrainingData);
					return new Tuple<string, Perceptron>(perceptronClass, p);
				}
			).ToArray ();

			//Linear utility Scores
			double[] linearClassificationFeatureUtilities = new double[schema.Length];
			foreach(Tuple<string, Perceptron> p in perceptronsByClass){
				for(int i = 0; i < schema.Length; i++){
					linearClassificationFeatureUtilities[i] += p.Item2.weights[i] * p.Item2.weights[i];
				}
			}

			for(int i = 0; i < schema.Length; i++){
				linearClassificationFeatureUtilities[i] = (linearClassificationFeatureUtilities[i] / classes.Length);
			}
			//Max linear utility score.
			double maxLinearClassificationFeatureUtility = linearClassificationFeatureUtilities.Max ();

			//Top perceptron weight.
			double[] topPerceptronWeights = Enumerable.Range (0, schema.Length).Select (weightIndex => perceptronsByClass.Select(p => Math.Abs (p.Item2.weights[weightIndex])).Max ()).ToArray ();
			double topTopPerceptronWeight = topPerceptronWeights.Max ();

			Tuple<string, double, double>[] featureAnalysisResults = new Tuple<string, double, double>[schema.Length];
			for(int i = 0; i < schema.Length; i++){
				featureAnalysisResults[i] = new Tuple<string, double, double>(schema[i], linearClassificationFeatureUtilities[i], topPerceptronWeights[i]);
			}

			/*
			double[] featureMins = Enumerable.Range (0, schema.Length).Select (data.Select ()).ToArray();
			double[] featureMaxes;
			double[] featureMeans;
			double[] featureStdevs;
			*/

			StringBuilder result = new StringBuilder();

			result.AppendLine (@"\subsection{Feature overview}");
			result.AppendLine (latexLongTableString(
				"l;c;c;c;c".Split (';'),
				"Feature Name;Min;Max;Mean;Stdev".Split (';'),
				Enumerable.Range(0, schema.Length).Select(featureIndex => 
			        (@"\texttt{" + latexEscapeString(schema[featureIndex]) + "}" + latexLabelString ("table:feature:" + criterion + ":" + schema[featureIndex])).Cons(featureStats[featureIndex].Select(item => Double.IsNaN (item) ? colorDouble (Double.NaN) : item.ToString ("G4"))) 
				)
			));

			//TODO: Matrix of features by classes?

			result.AppendLine (@"\subsection{Linear classification feature utility.}");
			result.AppendLine ("This section evaluates the utility of features for use in a linear classifier.  This is accomplished by training a linear classifier (perceptron) to recognize each class individually.  All trainging is performed on normalized training data.");
			result.AppendLine ();
			result.AppendLine (@"Let the i\textsuperscript{th} such perceptron's weight vector be labeled $P_i$.  Feature $f$'s linear feature utility is defined as");
			result.AppendLine (@"$$\text{lincfu}(f) = \sqrt{\frac{\mathlarger\sum_{i = 0}^{|\text{classes}|} \Big(\frac{P_{i_f}}{||P_i||}\Big) ^ 2}{|\text{classes}|}}$$");  //TODO: This normalization might not be complete?  I think this way, one feature per class can have score 1
			result.AppendLine ("Linear classification feature utility is useful for identifying features that are generally useful.  Many features are only useful for detection of specific classes; such features generally have low linear classification feature utility, and are better identified by determining how useful a feature is in the class where it is maximally useful.");
			result.AppendLine ("The concept of maximal subset linear classification feature utility captures this concept, and is defined as the like so:");
			result.AppendLine (@"$$\text{maxsublcfu}(f) = \max\Big(\big\{\text{linfu}(x): x \in \{\mathcal{P}(\text{classes})\} \setminus \emptyset \big\}\Big)$$");
			//as the absolute value of the maximum weight of ")
			result.AppendLine (@"The above definition represents a powerful concept, as it captures the features linear classification feature utility over the subset of classes for which the feature is most useful.  Although of combinatorial complexity when computed naïvely, it can be shown that the above definition is equivalent, for each f, to $$\max\Big(\big\{\frac{|P_{i_f}|}{||P_i||}: i = (1, \hdots, |\text{classes}|)\big\}\Big)$$.");
			/*
			//Horizontal array
			result.AppendLine (latexTabularString (
				"l|".Cons (Enumerable.Range (0, schema.Length).Select (i => "c")),
			    new IEnumerable<string>[]{
					"Feature Name".Cons (schema),
					"Linear Classification Feature Utility".Cons (usefulnessScores.Select(score => colorDouble (score, maxUsefulnessScore)))
				}
			));
			*/

			//Vertical array

			int colsToUse = 4;
			colsToUse = 1; //Seems to cause issues.


			//TODO: Don't do this.
			string temp = formatString;
			formatString = "G3";
			if(colsToUse > 1) result.AppendLine (@"\begin{multicols}{" + colsToUse + "}");
			result.AppendLine (latexLongTableString (
				//"l|;p{1.5in};p{1.5in}".Split (';'),
				//"Feature Name;Linear Classification Feature Utility;Maximal Subset Linear Classification Feature Utility".Split (';'),
				"l|;c;c".Split (';'),
				"Feature Name;lincfu;maxsublcfu".Split (';'),
				featureAnalysisResults.OrderByDescending(item => item.Item2).Select (item => new[]{latexHyperrefString("table:feature:" + criterion + ":" + item.Item1, @"\texttt{" + item.Item1 + "}"), colorDouble(item.Item2, maxLinearClassificationFeatureUtility), colorDouble (item.Item3)})));
			if(colsToUse > 1) result.AppendLine (@"\end{multicols}");

			//Bad!
			formatString = temp;

			//TODO: Solo quality and "drop cost"

			//TODO: Some sort of multivariate significance test.

			return result.ToString ();

		}
		
		public static string ClassificationReportLatexString<Ty> (this IEventSeriesProbabalisticClassifier<Ty> featureSynth, DiscreteSeriesDatabase<Ty> dataToClassify, string criterionByWhichToClassify, double confidenceCutoff)
		{
			dataToClassify = dataToClassify.Filter (item => !item.labels.ContainsKey (criterionByWhichToClassify));

			string[] schema = featureSynth.GetClasses();
			string[] schemaText = schema.Select (item => @"\text{" + limitLength (item, 20) + "}").ToArray ();
			string[] schemaTextRotated = schemaText.Select (item => @"\begin{turn}{70}" + item + @"\end{turn}").ToArray ();

			//filename, prediction class, prediction strength, prediction score vector.
			//TODO: Make this schema line up with the above as closely as possible.

			IEnumerable<Tuple<string, string, double, double[]>> classifications = dataToClassify.data.AsParallel().Select (item => 
				{
					double[] vals = featureSynth.Classify(item);
					int maxIndex = vals.MaxIndex();
					return new Tuple<string, string, double, double[]>(item.labels["filename"], schema[maxIndex], vals[maxIndex], vals);
				}).ToArray ();

			Tuple<IEnumerable<Tuple<string, string, double, double[]>>, IEnumerable<Tuple<string, string, double, double[]>>> confidenceSplit = classifications.Partition (item => item.Item3 >= confidenceCutoff);
			Tuple<string, string, double, double[]>[] significantClassifications = confidenceSplit.Item1.ToArray ();
			Tuple<string, string, double, double[]>[] insignificantClassifications = confidenceSplit.Item2.ToArray ();

			int topNClasses = 5;

			StringBuilder result = new StringBuilder();
			
			result.AppendLine ("Here a report of the classifier's prediction for each unknown input, of which there were " + dataToClassify.data.Count + ", is presented.  For more information on how to interpret these values, check out the Accuracy Report section.");
			result.AppendLine ("Generally speaking, the higher the prediction strength, the higher the probability that the classification is correct.");

			result.AppendLine (@"\subsection{Overview}");
			result.AppendLine ("Here the classification results are presented in a quick summary.  The predicted class is given, along with the prediction weight, and the prediction weights of the next " + (topNClasses - 1) + " guesses.");
			result.AppendLine ("One can generally be more confident when the primary guess has high value and the remaining guesses do not.");
			result.AppendLine ();

			int significantCount = significantClassifications.Length;

			result.AppendLine (latexLongTableString (
				"l|".Cons(Enumerable.Range(0, topNClasses).Select(i => "c")),
				"Instance Name".Cons(Enumerable.Range(0, topNClasses).Select(i => ordinal ((i + 1), true))),
				significantClassifications.OrderByDescending(tuple => tuple.Item3).Select (
					item => limitLength (item.Item1, 25).Cons (item.Item4.Select ((score, index) => new Tuple<double, string>(score, schemaText[index])).OrderByDescending (tup => tup.Item1).Take (topNClasses).Select (final => final.Item2 + ":" + colorPercent (final.Item1)))
				)
			));

			int insignificantCount = insignificantClassifications.Count ();
			if(insignificantCount > 0){
				result.AppendLine ("In addition to the " + englishCountOfString ("classification", significantCount) + " shown above, " + englishCountOfString ("classification was", insignificantCount) + " omitted.");
			}



			/*
			//This section is probably unnecessary.  This information however does belong in the final report.
			
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

			*/

			return result.ToString ();

		}


		////////////
		//Generate useful latex constructs

		//Strings

		//Escapes latex characters.
		public static string latexEscapeString(string s){
			//Escape backslashes that aren't escaping something else, then escape things that need escaping that aren't backslashes.
			return s.Replace (@"\", "").RegexReplace (@"([{}#%_$&])", @"\$1").RegexReplace (@" {2,}", " ");
			//TODO: squared?
			//return s.RegexReplace(@"(([^\\])[][{}#%_$])|(?:\\", @"$1\$2");
		}

		//Gets rid of non labelable characters
		public static string latexLabelableString(string s){
			return s.RegexReplace (@"[][{}#%_\\«»~&$]", @""); //TODO: Maybe some of these are ok.
		}

		public static string latexLabelString(string s){
			return @"\label{" + latexLabelableString (s) + "}";
		}
		
		public static string latexHyperrefString(string label, string text){
			return @"\hyperref[" + latexLabelableString(label) + "]{" + text + "}";
		}
		
		public static string latexClassString(string @class){
			return @"\texttt{" + latexEscapeString(@class) + "}";
		}
		public static string latexClassString(string criterion, string @class){
			return @"\texttt{" + latexEscapeString(criterion) + ":" + latexEscapeString(@class) + "}";
		}


		//Number strings:

		const string nanString = "--";
		internal static string colorString(string s, double d){
			if(d < 0) d = 0;
			else if(d > 1) d = 1;
			if(Double.IsNaN (d)) d = .8;
			double brightness = (1 - d) * 0.85;
			return @"\textcolor[gray]{" + brightness.ToString (formatString) + "}{" + s + "}";
		}

		internal static string colorDouble(double d){
			if(Double.IsNaN(d)) return colorString (nanString, d);
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
			string valStr;
			if(Double.IsNaN (val)){
				valStr = nanString;
			}
			else{
				valStr = val.ToString (formatString);
			}
			return colorString (valStr, val / outof);
		}

		internal static string colorPercent(double d, int places = 2){
			if(Double.IsNaN(d)) return colorString ("-", .8);
			double colorVal = Math.Min (1, Math.Abs (d));
			return colorString ((d * 100).ToString ("F" + places) + @"\%", colorVal);

		}

		//Matrices and tables:


		//MATRIX:
		internal static string latexMatrixString(string envname, bool mathmode, IEnumerable<IEnumerable<string>> data){
			StringBuilder result = new StringBuilder();
			result.AppendLine ((mathmode ? "$$" : "") + @"\begin{" + envname + "}");
			result.AppendLine (data.FoldToString (row => row.FoldToString("\t", "", " & "), "", "", " \\\\\n"));
			result.AppendLine (@"\end{" + envname + "}" + (mathmode ? "$$" : ""));
			return result.ToString ();
		}

		internal static string labeledMatrixCorner = @"\cdot";
		internal static string latexLabeledMatrixString(string envName, IList<string> labels, IEnumerable<IEnumerable<string>> data){
			return latexMatrixString (envName, true, labeledMatrixCorner.Cons (labels).Cons (data.Select ((row, index) => labels[index].Cons (row))));
		}

		//TODO: Choice to include single lines.
		internal static string latexTabularLabeledMatrixString(IList<string> labels, IEnumerable<IEnumerable<string>> data, int angle = 60){

			//Prepend labels onto the first item of data
			data = data.Select ((item, index) => labels[index].Cons (item)); 

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


		internal static string elipsis = @"\ldots"; //"…"; //TODO: Elipsis gets picked up as nonprintable.
		public static string limitLength(string s, int lim){
			if(s.Length > lim){
				return (s.Substring (0, lim - 1) + elipsis).Replace (@"\" + elipsis, elipsis); //TODO: Think about this.
			}
			return s;
		}

		////////////
		//ENCODING:

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


		/////////////////////
		/// LINE PROCESSING:

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

		////////
		//ENGLISH:

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

		internal static string[] quantificationAdverbPhrases = "infinitessimally;ever so slightly;a tiny bit;slightly;noticably;somewhat;quite;very;heavily;quite heavily;extremely;near completely".Split(';');
		internal static string quantificationAdverbPhrase(double d){
			if(d < 0.000001) return "not";
			if(d > 0.99999) return "fully";
			return quantificationAdverbPhrases[(int)(d * quantificationAdverbPhrases.Length)];
		}

		internal static string[] frequencyQuantificationAdverbPhrases = "infinitessimally infrequently;virtually never;intermittently;rarely;occasionally;infrequntly;sometimes;often;quite often;frequently;more often than not;usually;quite frequently;nearly constantly;almost always".Split (';');
		//English language functions
		internal static string frequencyQuantificationAdverbPhrase(double d){
			if(d < .000001){
				return "never";
			}
			if(d > .99999){
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
			return englishNumberString(count, preferNumeric) + " " + pluralPhrase(s, count);
		}

		public static Dictionary<string, string> irregularPlurals = "symposium:symposia;curriculum:curricula;series:series;schema:schemata;formula:formulae;hypothesis:hypothesi;criterion:criteria;datum:data;alumna:alumnae;foot:feet;goose:geese;louse:lice;dormouse:dormice;man:men;mouse:mice;tooth:teeth;woman:women".Split (";:".ToCharArray()).AdjacentPairs().ToDictionary ();

		public static string plural(string s){
			{
				string plur;
				if(irregularPlurals.TryGetValue(s, out plur)){
					return plur;
				}
			}
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

			//Nominative case pluralization
			switch(s){
				case "it":
					return "they";
			}

			//Verb conjugation
			switch(s){
				case "is":
					return "are";
				case "was":
					return "were";
				case "has":
					return "have";
			}

			if(s.EndsWith ("x")){
				return s.Substring(0, s.Length - 1) + "ces";
			}
			if(s.EndsWith ("y")){
				return s.Substring(0, s.Length - 1) + "es";
			}
			if(s.EndsWith ("f")){
				return s.Substring(0, s.Length - 1) + "ves";
			}
			if(s.EndsWith ("fe")){
				return s.Substring(0, s.Length - 2) + "ves";
			}
			if(s.EndsWith ("s") || s.EndsWith ("ch") || s.EndsWith ("sh")){
				return s + "es";
			}

			/*
			if(s.EndsWith ("o")){ //this is sometimes true.
				return s + "es";
			}
			*/
			return s + "s";
		}

		public static string englishCapitolizeFirst(string s){
			return s.Substring (0, 1).ToUpper () + s.Substring (1);
		}


		 
		//////////////
		//COLOR:
		
		public static Random rand = new Random();
		public static string applyRandomLatexColor(string s){
			return getRandomColorer()(s);
		}
		public static Func<string, string> getRandomColorer(double max = .8){
			string colorStr = Enumerable.Range (0, 3).Select (item => (rand.NextDouble () * max).ToString ("F3")).FoldToString ("", "", ",");
			return s => @"\textcolor[rgb]{" + colorStr + "}{" + s + "}";
		}


	}
}

