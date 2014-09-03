using System;

using System.Collections.Generic;

using System.Linq;
using System.Linq.Parallel;

using System.Diagnostics;

using System.Text;

using Whetstone;

namespace TextCharacteristicLearner
{
	public class ClassifierAccuracyAnalysis<Ty>
	{
		//INPUT:
		public IEventSeriesProbabalisticClassifier<Ty> classifier;
		public string classifierName;
		public DiscreteSeriesDatabase<Ty> labeledData;
		public string criterionByWhichToClassify;
		public double trainSplitFrac;
		public int iterations;
		public double bucketSize;

		//OUTPUT:

		//Text
		public string[] datasetSchema;
		public Dictionary<string, int> schemaMapping;
		public string[] datasetSchemaText;
		public string[] datasetSchemaTextRotated;

		//Classifications
		//Name, true class, predicted class, scores, winning score;
		public List<Tuple<string, string, string, double[], double>> classificationInstances;

		//Confusion matrices
		public int[,] confusionMatrixCounts; // [a,b] : How often a is classified as b 
		public double[,] confusionMatrixScores;

		//Confusion matrices by confidence
		public int[][,] countsConfusionMatricesByConfidence;
		public double[][,] scoresConfusionMatricesByConfidence;

		//Counts
		double[] countColumnSums;
		double[] countRowSums;
		double[] scoreColumnSums;
		double[] scoreRowSums;

		double[] classCountAccuracies;

		public double overallAccuracy;
		public double expectedAccuracyRandom;
		public double topClassSelectionAccuracy;

		double[,] accuracyByTrueClassAndConfidence;
		double[,] accuracyByPredictedClassAndConfidence;

		//Overfitting
		//Name, true class, predicted class, scores, winning score;
		public List<Tuple<string, string, string, double[], double>> trainingDataClassificationInstances;


		//Simple:
		
		int bucketCount;
		int classCount;




		//Should be parameters

		int maxDisplayClassCount = 50;
		bool testOverfitting = true;
		double overfittingTestFrac = .1;


		public ClassifierAccuracyAnalysis (IEventSeriesProbabalisticClassifier<Ty> classifier, string classifierName,DiscreteSeriesDatabase<Ty> labeledData, string criterionByWhichToClassify, double trainSplitFrac, int iterations, double bucketSize)
		{
			this.classifier = classifier;
			this.classifierName = classifierName;
			this.labeledData = labeledData;
			this.criterionByWhichToClassify = criterionByWhichToClassify;
			this.trainSplitFrac = trainSplitFrac;
			this.iterations = iterations;
			this.bucketSize = bucketSize;
		}
			
		public ClassifierAccuracyAnalysis<Ty> runAccuracyAnalysis ()
		{

			string nameCriterion = "filename"; //TODO: Input parameter or constant for this.

			//Filter out items not labeled for this criterion.
			labeledData = labeledData.FilterForCriterion (criterionByWhichToClassify);

			//
			datasetSchema = labeledData.getLabelClasses (criterionByWhichToClassify).Order ().ToArray ();

			//Create mapping from strings to indices
			schemaMapping = datasetSchema.IndexLookupDictionary ();

			bucketCount = (int)(1.0 / bucketSize);
			classCount = datasetSchema.Length;

			//Raw data classifications:
			//Name, true class, predicted class, scores, winning score;
			classificationInstances = new List<Tuple<string, string, string, double[], double>> ();
			if(testOverfitting){
				trainingDataClassificationInstances = new List<Tuple<string, string, string, double[], double>> ();
			}

			string classifierName = "\"" + AlgorithmReflectionExtensions.GetAlgorithmName(classifier) + "\"";

			Console.WriteLine ("Running classifier " + classifierName + " on " + labeledData.data.Count + " items.");

			//TODO: Not a bad idea to duplicate the classifiers to increase parallelism.

			//Run and make classifiers.
			for (int i = 0; i < iterations; i++) {
				Console.WriteLine ("Classifier Accuracy: Initiating round " + (i + 1) + " / " + iterations + " for " + classifierName + ".");
				
				Tuple<DiscreteSeriesDatabase<Ty>, DiscreteSeriesDatabase<Ty>> split = labeledData.SplitDatabase (trainSplitFrac); //TODO: Vary this?
				DiscreteSeriesDatabase<Ty> training = split.Item1;
				DiscreteSeriesDatabase<Ty> test = split.Item2;

				classifier.Train (training);

				string[] classifierSchema = classifier.GetClasses ();

				classificationInstances.AddRange (test.data.AsParallel ().WithExecutionMode(ParallelExecutionMode.ForceParallelism).Select (item => classificationInfo (classifier, classifierSchema, schemaMapping, item, nameCriterion, criterionByWhichToClassify)));
				if(testOverfitting){
					trainingDataClassificationInstances.AddRange (training.data.Take ((int)(overfittingTestFrac * training.data.Count)).AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).Select (item => classificationInfo (classifier, classifierSchema, schemaMapping, item, nameCriterion, criterionByWhichToClassify)));
				}
			}


			//Confusion Matrices.
			confusionMatrixCounts = new int[classCount, classCount]; // [a,b] : How often a is classified as b 
			confusionMatrixScores = new double[classCount, classCount];

			//Confusion matrix allocation
			countsConfusionMatricesByConfidence = new int[bucketCount][,];
			scoresConfusionMatricesByConfidence = new double[bucketCount][,];

			for (int i = 0; i < bucketCount; i++) {
				countsConfusionMatricesByConfidence [i] = new int[classCount, classCount];
				scoresConfusionMatricesByConfidence [i] = new double[classCount, classCount];
			}

			//Confusion Matrix population
			foreach (var classification in classificationInstances) {
				int confidenceBucket = Math.Min ((int)Math.Floor (classification.Item5 * bucketCount), bucketCount - 1); //On a score of 1 or greater, clip to the top bucket.  Highest confidence is always positive because confidences sum to 1.
				//Counts
				confusionMatrixCounts [schemaMapping [classification.Item2], schemaMapping [classification.Item3]] ++;
				countsConfusionMatricesByConfidence [confidenceBucket] [schemaMapping [classification.Item2], schemaMapping [classification.Item3]] ++;

				//Scores
				for (int j = 0; j < classCount; j++) {
					confusionMatrixScores [schemaMapping [classification.Item2], j] += classification.Item4 [j];
					scoresConfusionMatricesByConfidence [confidenceBucket] [schemaMapping [classification.Item2], j] += classification.Item4 [j];
				}
			}


			//Aggregates

			countColumnSums = Enumerable.Range (0, classCount).Select (i => (double)confusionMatrixCounts.SumColumn (i)).ToArray ();
			countRowSums = Enumerable.Range (0, classCount).Select (i => (double)confusionMatrixCounts.SumRow (i)).ToArray ();
			scoreColumnSums = Enumerable.Range (0, classCount).Select (i => confusionMatrixScores.SumColumn (i)).ToArray ();
			scoreRowSums = Enumerable.Range (0, classCount).Select (i => confusionMatrixScores.SumRow (i)).ToArray ();

			classCountAccuracies = Enumerable.Range (0, classCount).Select (c => confusionMatrixCounts [c, c] / (double)countRowSums [c]).ToArray ();
			overallAccuracy = Enumerable.Range (0, classCount).Select (i => (double)confusionMatrixCounts [i, i]).Sum () / classificationInstances.Count;
			expectedAccuracyRandom = (1.0 / classCount);
			topClassSelectionAccuracy = labeledData.GroupBy (item => item.labels[criterionByWhichToClassify]).Select (grp => grp.Count ()).Max () / (double)labeledData.data.Count;

			//Safety check.
			{
				double countSum = countColumnSums.Sum ();
				double scoreSum = scoreColumnSums.Sum ();

				//These should all be the same ass instancesClassifiedCount, to within numerics errors.
				Trace.Assert (Math.Abs (countSum - classificationInstances.Count) < .00001);
				Trace.Assert (Math.Abs (scoreSum - classificationInstances.Count) < .00001);
			}

			//Class, Confidence Bucket
			accuracyByTrueClassAndConfidence = new double[classCount + 1, bucketCount];
			accuracyByPredictedClassAndConfidence = new double[classCount + 1, bucketCount];
			
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
				try {
					accuracyByPredictedClassAndConfidence [0, i] = Enumerable.Range (1, classCount).Select (j => accuracyByTrueClassAndConfidence [j, i]).Where (val => !Double.IsNaN (val)).Average ();
				} catch {
					accuracyByPredictedClassAndConfidence [0, i] = Double.NaN;
				}
			}

			//For use in math mode elements and matrices.
			datasetSchemaText = datasetSchema.Select (item => @"\text{" + LatexExtensions.limitLength (item, 15) + "}").ToArray ();
			datasetSchemaTextRotated = datasetSchemaText.Select (item => @"\begin{turn}{70} " + item + @" \end{turn}").ToArray ();

			//TODO: Limiting length could cause duplication


			return this;
		}
				
		//Name, true class, predicted class, scores, winning score;
		public static Tuple<string, string, string, double[], double> classificationInfo (IEventSeriesProbabalisticClassifier<Ty> classifier, string[] classifierSchema, Dictionary<string, int> trueSchemaMapping, DiscreteEventSeries<Ty> data, string nameCriterion, string criterionByWhichToClassify)
		{

			//scores in the synthesizer scorespace
			double[] synthScores = classifier.Classify (data);

			int maxIndex = synthScores.MaxIndex ();

			/*
			classifierSchema = classifier.GetClasses();
			if (maxIndex >= classifierSchema.Length) {
				Console.WriteLine ("Schema not long enough.  synthlen, max, schema = " + synthScores.Length + ", " + maxIndex + ", " + classifierSchema.Length);
				Console.WriteLine ("Classifier Info:");
				Console.WriteLine (classifier.ToString ());
				Console.WriteLine ("Synth Features:");
				Console.WriteLine (((SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>)classifier).synthesizer.GetFeatureSchema().FoldToString ());
				Console.WriteLine ("Classifier Features:");
				Console.WriteLine (((SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>)classifier).classifier.GetClasses().FoldToString ());
				return null;
			}
			*/

			string predictedClass = classifierSchema[maxIndex];
			double maxScore = synthScores[maxIndex];

			//convert scores to the true space.
			double[] trueScores = new double[trueSchemaMapping.Count];
			for(int j = 0; j < classifierSchema.Length; j++){
				trueScores[trueSchemaMapping[classifierSchema[j]]] = synthScores[j];
			}

			return new Tuple<string, string, string, double[], double> (data.labels[nameCriterion], data.labels[criterionByWhichToClassify], predictedClass, trueScores, maxScore);
		}

		public string latexAccuracyAnalysisString(string subsection = @"\subsection", string subsubsection = @"\subsubsection"){

			double instancesClassifiedDouble = classificationInstances.Count;
			double accuracyImprovement = overallAccuracy - expectedAccuracyRandom;

			StringBuilder result = new StringBuilder();

			//RAW RESULTS

			result.AppendLine (subsection + "{Classifier Accuracy Estimation Report}");
			result.AppendLine (@"\label{sec:classifier:accuracy " + classifierName + "}");
			result.AppendLine ("From a set of " + labeledData.data.Count + " labeled instances, classifiers were build with a training data to test data ratio of " + trainSplitFrac.ToString(LatexExtensions.formatString) + " (" + ((int) (trainSplitFrac * labeledData.data.Count())) + " training, " + (labeledData.data.Count - ((int) (trainSplitFrac * labeledData.data.Count()))) + " test instances).");
			result.AppendLine ("The process was repeated " + iterations + " times, for a total of " + classificationInstances.Count + " classifications.");
			result.AppendLine ("In these trials, the overall accuracy rate was found to be " + overallAccuracy.ToString (LatexExtensions.formatString) + ", which is " + Math.Abs (accuracyImprovement).ToString (LatexExtensions.formatString) + " " + ((accuracyImprovement > 0) ? "better" : "worse") + " than random choice (" + expectedAccuracyRandom.ToString (LatexExtensions.formatString) + ").");

			result.AppendLine ("The raw results appear here below\\footnote{It may be that an instance appears multiple times in the table below; this is expected behavior, as the training and test datasets are repeatedly randomly selected.  This behavior is not undesirable, as, assuming the highly probable case where the repeated item is repeated classified by a classifier trained on different data in both each instance, each classification instance represents a novel point and is still valuable.}.\n");


			//Matrix form:
			/*
			result.AppendLine (LatexExtensions.latexMatrixString("bmatrix", true, ("Instance Name;True Class;Predicted Class".Split (';').Concat (datasetSchema.Select (item => item )).Select (item => @"\text{" + item + "}")).Cons (
				classificationInstances.Select (
					item => new string[]{item.Item1, item.Item2, item.Item3}.Select (str => @"\text{" + LatexExtensions.limitLength (str, 16) + "}").Concat<string>(item.Item4.Select (val => LatexExtensions.colorDouble(val)))
				)
			)));
			*/

			//Tabular form:
			/*
			result.AppendLine (latexTabularString ("l;c;c".Split (';').Concat (datasetSchema.Select(item => "c")),
				classificationInstances.Select (
					item => new string[]{item.Item1, item.Item2, item.Item3}.Select (str => @"\text{" + LatexExtensions.limitLength (str, 16) + "}").Concat<string>(item.Item4.Select (val => LatexExtensions.colorDouble(val)))
				)
			));
			*/

			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Full report omitted because classes exceed limit " + maxDisplayClassCount + "}*.");

				int topnClasses = 5;
				int topnItems = 1000;

				result.AppendLine (LatexExtensions.latexLongTableString (
					"l|l|".Cons(Enumerable.Range(0, topnClasses).Select(i => "c")),
					"Instance Name;True Class".Split (';').Concat(Enumerable.Range(0, topnClasses).Select(i => LatexExtensions.ordinal ((i + 1), true))),
					classificationInstances.OrderByDescending(tuple => tuple.Item5).ThenByDescending (tuple => tuple.Item1).Take (topnItems).Select (
					item => new[]{LatexExtensions.limitLength (item.Item1, 25), LatexExtensions.limitLength (item.Item2, 25)}.Concat (item.Item4.Select ((score, index) => new Tuple<double, string, string>(score, datasetSchema[index], datasetSchemaText[index])).OrderByDescending (tup => tup.Item1).Take (topnClasses).Select ((final, index) => ((final.Item2 == item.Item2) ? final.Item3 : @"\textcolor[rgb]{" + (.9 * 3 / (3 + index)) + "," + (.1 * 3 / (3 + index)) + "," + (.1 * 3 / (3 + index)) + "}{ " + final.Item3 + "}") + @":" + LatexExtensions.colorPercent (final.Item1)))
					)
				));
				if (classificationInstances.Count > topnItems){
					result.AppendLine (@"*\textbf{Only first " + topnItems + " shown.}"); //TODO also show the last couple?
				}
			}
			else{
				//Longtable form
				result.AppendLine (LatexExtensions.latexLongTableString (
					"l;c;c".Split (';').Concat (datasetSchemaText.Select(item => "c")),
					"Instance Name;True Class;Predicted Class".Split (';').Concat (datasetSchemaText),
					classificationInstances.OrderByDescending (tuple => tuple.Item5).ThenByDescending (tuple => tuple.Item1).Select (
					item => new string[]{LatexExtensions.limitLength (item.Item1, 16), item.Item2, (item.Item3 == item.Item2) ? item.Item3 : (@"\textcolor[rgb]{.9,.1,.1}{" + item.Item3 + "}")}.Concat<string>(item.Item4.Select (val => LatexExtensions.colorDouble(val)))
					)
				));
			}



			//ACCURACY PROFILE

			result.AppendLine (subsubsection + "{True vs. Predicted Class Distributions (Classifier Accuracy)}");

			result.AppendLine ("Although it is one of the most basic metrics for classifier accuracy, comparing the true and predicted class distribution is an excellent way to detect a classifier biased toward or against a particular class.");
			result.AppendLine ("Confusion matrices and other techniques become necessary when the bias becomes more subtle, as in the case where instances of one particular class are often confused for instances of another.");

			
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{True Distribution}:" + "\n");			
				
				result.AppendLine (LatexExtensions.latexMatrixString ("bmatrix", true, new[]{
					(@"\text{Total}").Cons (datasetSchemaTextRotated), 
					instancesClassifiedDouble.ToString(LatexExtensions.formatString).Cons (countRowSums.Select (item => LatexExtensions.colorDouble(item, instancesClassifiedDouble))),
					("-").Cons    (countRowSums.Select (item => LatexExtensions.colorPercent(item / instancesClassifiedDouble)))

				}));
				
				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{Predicted Distribution}:" + "\n");

				result.AppendLine (LatexExtensions.latexMatrixString ("bmatrix", true, new[]{
					(@"\text{Total}").Cons (datasetSchemaTextRotated), 
					instancesClassifiedDouble.ToString(LatexExtensions.formatString).Cons(countColumnSums.Select (item => LatexExtensions.colorDouble(item, instancesClassifiedDouble))),
					("-").Cons    (countColumnSums.Select (item => LatexExtensions.colorPercent(item / instancesClassifiedDouble)))
				}));

				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{Error in Distribution (Predicted - True)}:" + "\n");

				result.AppendLine (LatexExtensions.latexMatrixString ("bmatrix", true, new[]{
					(datasetSchemaTextRotated).Select (str => @"\text{" + str + "}"), 
					(countRowSums.Zip(countColumnSums, (rowSum, columnSum) => (columnSum - rowSum).ToString(LatexExtensions.formatString))),
					(countRowSums.Zip (countColumnSums, (rowSum, columnSum) => LatexExtensions.colorPercent((columnSum - rowSum) / instancesClassifiedDouble)))
				}));
			}

			//SCORE PROFILE
			
			result.AppendLine (subsubsection + "{True vs. Predicted Class Distributions (Algorithm Score)}");

			//TODO: Writeup for this.  ALgorthim weight score instead of accuracy.
			
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				
				result.AppendLine (@"\par\bigskip");
				result.AppendLine(@"\textbf{True Distribution}:" + "\n");

				result.AppendLine (LatexExtensions.latexMatrixString ("bmatrix", true, new[]{
					(@"\text{Total}").Cons (datasetSchemaTextRotated), 
					instancesClassifiedDouble.Cons (scoreRowSums).Select (item => LatexExtensions.colorDouble(item, instancesClassifiedDouble)),
					instancesClassifiedDouble.Cons (scoreRowSums).Select (item => LatexExtensions.colorDouble(item / instancesClassifiedDouble))
				}));
				
				result.AppendLine (@"\par\bigskip");
				result.AppendLine(@"\textbf{Predicted Distribution}:" + "\n");

				result.AppendLine (LatexExtensions.latexMatrixString ("bmatrix", true, new[]{
					(@"\text{Total}").Cons (datasetSchemaTextRotated), 
					instancesClassifiedDouble.Cons (scoreColumnSums).Select (item => LatexExtensions.colorDouble(item, instancesClassifiedDouble)),
					instancesClassifiedDouble.Cons (scoreColumnSums).Select (item => LatexExtensions.colorDouble(item / instancesClassifiedDouble))
				}));

				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{Error in Distribution (Predicted - True)}:" + "\n");

				result.AppendLine (LatexExtensions.latexMatrixString ("bmatrix", true, new[]{
					datasetSchemaTextRotated, 
					(scoreRowSums).Zip (scoreColumnSums, (rowSum, columnSum) => (columnSum - rowSum).ToString(LatexExtensions.formatString)),
					(scoreRowSums).Zip (scoreColumnSums, (rowSum, columnSum) => LatexExtensions.colorPercent((columnSum - rowSum) / instancesClassifiedDouble))
				}));

			}




			result.AppendLine(subsubsection + "{Accuracy Confusion Matrix}");
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				result.AppendLine (@"\textbf{Note}: In all confusion matrices, columns map to predicted class and rows map to true class.");
				result.AppendLine ("All percents refer to the breakdown of the predicted class for the row's true class (thus rows sum to 100\\%).");

				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{Classification Counts Confusion Matrix}:" + "\n");

				result.AppendLine (LatexExtensions.latexTabularLabeledMatrixString(datasetSchemaText, 
					Enumerable.Range (0, classCount).Select (i => Enumerable.Range (0, classCount).Select (r => confusionMatrixCounts[i,r].ToString ()))));
				
				result.AppendLine (@"\par\bigskip");
				result.AppendLine (@"\textbf{Percents Confusion Matrix}:" + "\n");
				
				result.AppendLine (LatexExtensions.latexTabularLabeledMatrixString(datasetSchemaText, 
					Enumerable.Range (0, classCount).Select (row => Enumerable.Range (0, classCount).Select (column => LatexExtensions.colorPercent(confusionMatrixCounts[row,column] / countRowSums[row])))));
			}



			result.AppendLine(subsubsection + "{Algorithm Prediction Weight Score Confusion Matrix}" + "\n");
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				result.AppendLine (@"\textbf{Raw Scores Confusion Matrix}:" + "\n");
				//Matrix, not so nice
				/*
				result.AppendLine (latexLabeledMatrixString("bmatrix", datasetSchemaText, 
					Enumerable.Range (0, classCount).Select (row => 
				    	Enumerable.Range (0, classCount).Select (column => confusionMatrixScores[row, column].ToString(LatexExtensions.formatString)))));
				*/
				//Improved (tabular, rotated labels)
				result.AppendLine (LatexExtensions.latexTabularLabeledMatrixString(datasetSchemaText,
					Enumerable.Range (0, classCount).Select (row => 
				    	Enumerable.Range (0, classCount).Select (column => confusionMatrixScores[row, column].ToString(LatexExtensions.formatString)))));



				result.AppendLine (@"\textbf{Percentages Confusion Matrix}:" + "\n");
				//Matrix
				/*
				result.AppendLine (latexLabeledMatrixString("bmatrix", datasetSchemaText, 
					Enumerable.Range (0, classCount).Select (row => 
				    	Enumerable.Range (0, classCount).Select (column => LatexExtensions.colorPercent(confusionMatrixScores[row, column] / LatexExtensions.fzeroToOne (scoreRowSums[row]))))));
				*/
				//Tabular
				result.AppendLine (LatexExtensions.latexTabularLabeledMatrixString(datasetSchemaText,
					Enumerable.Range (0, classCount).Select (row => 
				    	Enumerable.Range (0, classCount).Select (column => LatexExtensions.colorPercent(confusionMatrixScores[row, column] / LatexExtensions.fzeroToOne (scoreRowSums[row]), 1)))));
			}

			// Derivation of the minimum bucket index:
			// minimum selectable confidence = 1.0 / class count
			// bucketsize = 1.0 / bucket count
			// index of minimum bucket = floor(minimum selectable confidence / bucket size) = floor(bucket count / class count
			int minPredictionBucketIndex = (int)Math.Floor (bucketCount / (double)classCount);
			int bucketsToEnumerate = bucketCount - minPredictionBucketIndex;


			//Confidence Accuracy Tradeoff

			result.AppendLine (subsubsection + "{Accuracy Analysis}" + "\n");

			result.AppendLine ("Here the collected data on the relationship between algorithm prediction strength value and accuracy are presented.");
			result.AppendLine ("The value output by the algorithm for prediction strength is only guaranteed to be monotonic with respect to the strength of the classification, by consulting this table, the accuracy of the classifier for various prediction strengths can be interpreted.");
			result.AppendLine ("Overall accuracies are presented first in unidimensional format, where neither true nor predicted class is considered (see below for their treatment).");

			result.AppendLine (@"\par\bigskip");
			result.AppendLine ("\n\n\\textbf{Accuracy by Prediction Strength}\n");
			result.AppendLine (LatexExtensions.latexMatrixString ("bmatrix", true, new[]{
				new[]{@"\text{E[Random]}", @"\mathbf{[0, 1]}"}.Concat (Enumerable.Range (minPredictionBucketIndex, bucketsToEnumerate).Select (b => @"[" + (b * bucketSize).ToString("0.##") + ", " + ((b + 1) * bucketSize).ToString("0.##") + @"]")), //TODO: Pull this out to a function, make intervals properly.
				new[]{expectedAccuracyRandom, overallAccuracy}.Concat(Enumerable.Range (minPredictionBucketIndex, bucketsToEnumerate).Select (b => accuracyByPredictedClassAndConfidence[0, b])).Select(item => LatexExtensions.colorPercent (item))
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
				
				result.AppendLine (LatexExtensions.latexMatrixString("bmatrix", true,
				    new[]{
						new []{@"\cdot", @"\begin{turn}{70} \textbf{All Classes} \end{turn}"}.Concat (datasetSchemaTextRotated), //Header row
						@"\mathbf{[0, 1]}".Cons((@"\mathbf{" + LatexExtensions.colorPercent (overallAccuracy) + "}").Cons(Enumerable.Range (0, classCount).Select (c => LatexExtensions.colorPercent(confusionMatrixCounts[c,c] / (double)countColumnSums[c])))), //[0, 1] row //TODO: sumscoreaccuracies
						new[]{"\t\\\\\n"} //Blank row
					}.Concat (
						Enumerable.Range (minPredictionBucketIndex, bucketsToEnumerate).Select (
							b => (@"[" + (b * bucketSize).ToString("0.##") + ", " + ((b + 1) * bucketSize).ToString("0.##") + "]").Cons(
							Enumerable.Range (0, classCount + 1).Select (c => LatexExtensions.colorPercent(accuracyByPredictedClassAndConfidence[c, b]))))
					)
				));
			}

			
			result.AppendLine (@"\par\bigskip");
			result.AppendLine ("\\textbf{True Class Accuracy Analysis Matrix}:\n");

			
			if(classCount > maxDisplayClassCount){
				result.AppendLine (@"*\textbf{Omitted because classes exceed limit " + maxDisplayClassCount + "}.");
			}
			else{
				result.AppendLine (LatexExtensions.latexMatrixString("bmatrix", true,
				                                     
				    new[]{
						new []{@"\cdot", @"\begin{turn}{70} \textbf{All Classes} \end{turn}"}.Concat (datasetSchemaTextRotated), //Header row
						@"\mathbf{[0, 1]}".Cons((@"\mathbf{" + LatexExtensions.colorPercent (overallAccuracy) + "}").Cons(Enumerable.Range (0, classCount).Select (c => LatexExtensions.colorPercent(classCountAccuracies[c])))), //[0, 1] row
						new[]{"\t\\\\\n"} //Blank row
					}.Concat (
						Enumerable.Range (minPredictionBucketIndex, bucketsToEnumerate).Select (
							b => (@"[" + (b * bucketSize).ToString("0.##") + ", " + ((b + 1) * bucketSize).ToString("0.##") + "]").Cons(
							Enumerable.Range (0, classCount + 1).Select (c => LatexExtensions.colorPercent(accuracyByTrueClassAndConfidence[c, b]))))
					)
				));
			}

			if(testOverfitting)
			{
				//Name, true class, predicted class, scores, winning score;
				double trainingDataAccuracy = trainingDataClassificationInstances.Where(instance => instance.Item3 == instance.Item2).Count() / (double) trainingDataClassificationInstances.Count;

				//TODO: Sum weight as well?

				result.AppendLine (subsubsection + "{Overfitting Analysis}");
				result.AppendLine ("Classifier performance on training data was measured " + trainingDataAccuracy.ToString(LatexExtensions.formatString) + ", whereas test data accuracy was measured at " + overallAccuracy.ToString (LatexExtensions.formatString) + ".");
				if(trainingDataAccuracy < overallAccuracy) result.AppendLine ("Assuming sufficient coverage in testing data, this is a good sign that the learned models are very general, and do not overfit the data.");
				else result.AppendLine ("This discrepancy suggests the learned model's output is " + LatexExtensions.quantificationAdverbPhrase(trainingDataAccuracy - overallAccuracy) + " succeptible to overfitting.");

				//TODO: Overfitting by class?
				//TODO: Overfitting by confidence?
			}

			//May be good to have a space filling charts of correct vs incorrect classifications by confidence.
			
			result.AppendLine (subsubsection + "{Insights}");
			result.AppendLine ("This subsection presents insights that can be gleaned from the above data.  It is particularly useful for large datasets, where it is difficult to interpret results presented in enormous matrices.");

			result.AppendLine (@"\par\bigskip");
			result.AppendLine (@"\textbf{Classifier Skew}");
			result.AppendLine (@"\begin{itemize}");
			result.AppendLine (Enumerable.Range (0, classCount).Select(
								i => new Tuple<string, double>(datasetSchema[i], (countColumnSums[i] - countRowSums[i]) / instancesClassifiedDouble)).Where (item => Math.Abs (item.Item2) > .02).OrderByDescending(item => Math.Abs (item.Item2)).Take(20).Select(
									tup => "The classifier is " + LatexExtensions.quantificationAdverbPhrase(Math.Pow(Math.Abs (tup.Item2), .65)) + " skewed " + ((tup.Item2 > 0) ? "toward" : "against") + " class \"" + tup.Item1 + "\" by " + LatexExtensions.colorPercent(Math.Abs (tup.Item2)) + ".").FoldToString("\t\\item ", "", "\t\\item "));
			result.AppendLine (@"\end{itemize}");

			result.AppendLine (@"\par\bigskip");
			result.AppendLine (@"\textbf{Interclass Confusion Bias Detection}");
			result.AppendLine (@"\begin{itemize}");
			result.AppendLine (Enumerable.Range (0, classCount).SelectMany(row => Enumerable.Range (0, classCount).Select (col => new Tuple<string, string, double>(datasetSchema[row], datasetSchema[col], confusionMatrixCounts[row, col] / (double) countRowSums[row]))).Where (tup => tup.Item1 != tup.Item2).Where(tup => tup.Item3 >= .02).OrderByDescending(tup => tup.Item3).Take(30) //Select the top n highest values not on the diagonal (greatest mistakes)
									.Select(tup => "Instances of class \"" + tup.Item1 + "\" are " + LatexExtensions.frequencyQuantificationAdverbPhrase(Math.Pow (tup.Item3, .8)) + " mistaken for those of class \"" + tup.Item2 + "\" (This occurs in " + LatexExtensions.colorPercent (tup.Item3) + " of test instances).").FoldToString ("\t\\item ", "", "\t\\item ")); 
									//TODO, somehow ignore small classes being mistaken as larger ones, could be chance.  Better yet, add statistical significance of this bias.
			result.AppendLine (@"\end{itemize}");

			result.AppendLine (@"\par\bigskip");

			//TODO: Don't show for empties ^^^

			//Poor class accuracy insights:
			{
				double poorCutoff = Math.Min (.5, expectedAccuracyRandom + topClassSelectionAccuracy);
				int poorClassesMax = 15;
				Tuple<string, double>[] poorClasses = Enumerable.Range (0, classCount).Select(i => new Tuple<string, double>(datasetSchema[i], classCountAccuracies[i])).Where (item => Math.Abs (item.Item2) < poorCutoff).OrderBy(item => Math.Abs (item.Item2)).ToArray();

				if(poorClasses.Length > 0){
					//result.AppendLine (@"\subsubsection{Poor Class Performance Detection}");
					result.AppendLine ("Instances of " + LatexExtensions.englishCountOfString("class", poorClasses.Length) + " were classified poorly.\n");
					result.AppendLine (@"\begin{itemize}");
					result.AppendLine (poorClasses.Take(poorClassesMax).Select(
										tup => "Instances of class \"" + tup.Item1 + "\" are " + LatexExtensions.frequencyQuantificationAdverbPhrase(1 - tup.Item2) + " misclassified (" + LatexExtensions.colorPercent(Math.Abs (tup.Item2)) + " accuracy).  "
											+ ((tup.Item2 < expectedAccuracyRandom) ? (@"\textit{This performance is worse than that of random selection} (" + LatexExtensions.colorPercent (expectedAccuracyRandom) + ").") : ""))
											.FoldToString("\t\\item ", "", "\t\\item "));
					result.AppendLine (@"\end{itemize}");
					if(poorClasses.Length > poorClassesMax){
						result.AppendLine(@"\textit{Top " + poorClassesMax + " most misclassified classes shown above, " + (poorClasses.Length - poorClassesMax) + " classes omitted." + "}");
					}

					result.AppendLine(@"\par\bigskip");
				}
			}






			//POSSIBLE CLASS EQUIVALENCE

			//Calculate error rate between strings using Levenshtein distance 
			double[,] levErrorRates = LevenshteinDistance.ErrorRateMatrix(datasetSchema);

			double levErrorCutoff = .4;

			result.AppendLine ();

			double errorFactor = .8;

			double minErrorRate1 = overallAccuracy * errorFactor;

			//TODO: Program this right.  None of this "for loop" business.

			//Name1, Name2, LevError, Instances1, Instances2, Confusion 1->2, Confusion 2->1, "Class Similarity Score
			List<TupleStruct<string, string, double, int, int, double, double, double>> type1Mistakes = new List<TupleStruct<string, string, double, int, int, double, double, double>>();
			for(int i = 0; i < classCount; i++){
				for(int j = 0; j < classCount; j++){
					if(i == j) continue;
					double ijConfusion = confusionMatrixCounts[i,j] / (double)countRowSums[i];
					double jiConfusion = confusionMatrixCounts[j,i] / (double)countRowSums[j];
					if(levErrorRates[i,j] < levErrorCutoff && countRowSums[i] < countRowSums[j] / 3 && ijConfusion > minErrorRate1){
						type1Mistakes.Add (new TupleStruct<string, string, double, int, int, double, double, double>
		                    (
								datasetSchema[i], datasetSchema[j], 
								levErrorRates[i,j], 
								(int)countRowSums[i], (int)countRowSums[j], 
								ijConfusion, 
								jiConfusion, ijConfusion / (1 + levErrorRates[i, j] + countRowSums[i] / countRowSums[j] )
							)
						);
					}
				}
			}

		
			HashSet<Tuple<string, string>> type1Pairs = new HashSet<Tuple<string, string>>(type1Mistakes.Select (item => new Tuple<string, string>(item.Item1, item.Item2)).Concat (type1Mistakes.Select (item => new Tuple<string, string>(item.Item2, item.Item1))));

			double minErrorRate2 = overallAccuracy * errorFactor / 2; // Only half can be expected to be misclassified this time.
			double sizeDiffCutoff = 4;

			List<TupleStruct<string, string, double, int, int, double, double, double>> type2Mistakes = new List<TupleStruct<string, string, double, int, int, double, double, double>>();
			for(int i = 0; i < classCount; i++){
				for(int j = i + 1; j < classCount; j++){
					double ijConfusion = confusionMatrixCounts[i,j] / (double)countRowSums[i];
					double jiConfusion = confusionMatrixCounts[j,i] / (double)countRowSums[j];
					if(levErrorRates[i,j] < levErrorCutoff && countRowSums[i] < countRowSums[j] * sizeDiffCutoff && countRowSums[j] < countRowSums[i] * sizeDiffCutoff && ijConfusion > minErrorRate2 && jiConfusion > minErrorRate2 && Math.Abs (ijConfusion - jiConfusion) < .25){
						type2Mistakes.Add (new TupleStruct<string, string, double, int, int, double, double, double>
		                    (
								datasetSchema[i], datasetSchema[j], 
								levErrorRates[i,j], 
							 	(int)countRowSums[i], (int)countRowSums[j], 
								ijConfusion, jiConfusion, 
								(ijConfusion + jiConfusion) / (2 * (1 + levErrorRates[i, j]) + (2 * Math.Abs(countRowSums[i] - countRowSums[j]) / (countRowSums[i] + countRowSums[j])))
							)
						);
					}
				}
			}

			if(type1Mistakes.Count + type2Mistakes.Count > 0){
				result.AppendLine (@"\subsubsection{Possible Class Equivalence}");
				result.AppendLine ("With poor data quality, instances are often mislabeled.  In this section, possible mislabeling events are noted.");
				result.AppendLine ("Results are based on misclassification frequencies, lexicographic distances, relative abundances, and total class count.");

				result.AppendLine (@"\par\bigskip");

				if(type1Mistakes.Count > 0){
					
					result.AppendLine ("Case 1: A small class is frequently mistaken for a large class.");

					result.AppendLine (LatexExtensions.latexLongTableString(
						"l;l|;c;c|;c;c|;c;c".Split (';'),
						"Class A;Class B;Class Similarity Score;Name Distance;Class A Size;Class B Size;A $\\rightarrow$ B rate;B $\\rightarrow$ A rate".Split (';'),
						type1Mistakes.OrderBy (item => item.Item8).Select (item =>
					     	new[]{
								item.Item1, 
								item.Item2, 
								@"$\mathbf{" + LatexExtensions.colorDouble(1 - item.Item8) + "}",
								@"$\mathbf{" + item.Item8.ToString (LatexExtensions.formatString) +"}$", 
								item.Item4.ToString(), 
								item.Item5.ToString(), 
								LatexExtensions.colorDouble(item.Item6), 
								LatexExtensions.colorDouble (item.Item7)
							}
					)));
				}

				if(type2Mistakes.Count > 0)
				{
					result.AppendLine ();
					result.AppendLine ("Case 2: Two similarly sized classes are frequently confused.");

					result.AppendLine (LatexExtensions.latexLongTableString(
						"l;l|;c;c|;c;c|;c;c".Split (';'),
						"Class A;Class B;Class Similarity Score;Name Distance;Class A Size;Class B Size;A $\\rightarrow$ B rate;B $\\rightarrow$ A rate".Split (';'),
						type2Mistakes.Where (item => !type1Pairs.Contains(new Tuple<string, string>(item.Item1, item.Item2)))
						.OrderBy (item => item.Item8)
						.Select (item =>
					     	new[]{
								item.Item1, 
								item.Item2, 
								@"$\mathbf{" + LatexExtensions.colorDouble(1 - item.Item8) + "}",
								@"$\mathbf{" + item.Item3.ToString (LatexExtensions.formatString) +"}$", 
								item.Item4.ToString(), 
								item.Item5.ToString(), 
								LatexExtensions.colorDouble(item.Item6), 
								LatexExtensions.colorDouble (item.Item7),
							}
					)));
				}

			}

			//result.AppendLine ("Found " + type1Mistakes.Count + "type 1 mistakes and " + type2Mistakes.Count + " type 2 mistakes.");

			//TODO: Only take top n from either group.

			//TODO: Parametrize techniques.

			//TODO: Generalize techniques.

			return result.ToString ();
		}
	}
}

