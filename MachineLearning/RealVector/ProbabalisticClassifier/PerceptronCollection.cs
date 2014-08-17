using System;

using System.Collections.Generic;
using System.Linq;

using Whetstone;

namespace TextCharacteristicLearner
{
	//Perceptron from 

	//http://dynamicnotions.blogspot.com/2008/09/single-layer-perceptron.html

    public class Perceptron
    {
		internal double[] weights; //Shouldn't actually be public, but you know how it is.
		double learningRate = 0.1;

		int maxIterations = 200;

		public Perceptron(int dimension){
			weights = new double[dimension];
		}

		public void Train(TupleStruct<double[], int>[] trainingInstances){

			
            int iteration = 0;
            double globalError;
 
            do
            {
                globalError = 0;
                for (int p = 0; p < trainingInstances.Length; p++)
                {
                    // Calculate output.
                    int output = OutputClass(trainingInstances[p].Item1);
 
                    // Calculate error.
                    double localError = trainingInstances[p].Item2 - output;
 
                    if (localError != 0)
                    {
                        // Update weights.
                        for (int i = 0; i < weights.Length; i++)
                        {
                            weights[i] += learningRate * localError * trainingInstances[p].Item1[i];
                        }
                    }
 
                    // Convert error to absolute value.
                    globalError += Math.Abs(localError);
                }
 
                //Console.WriteLine("Iteration {0}\tError {1}", iteration, globalError);
                iteration++;
 
            } while (globalError != 0 && iteration < maxIterations);

			weights.NormalizeInPlace ();

		}

		//Modified perceptron training: this perceptron takes a double as argument and uses the strength of the output compared to the double to train.
		public void Train(TupleStruct<double[], int, double>[] trainingInstances){


			//Console.WriteLine ("TRAINING PERCEPTRON EVEN WEIGHTS");

            int iteration = 0;
            double globalError;

			//TODO: globalError won't drop to 0 except in cases where all instances are linearly independent (I think).  TODO: Proof of that.
            do
            {
                globalError = 0;
                for (int p = 0; p < trainingInstances.Length; p++)
                {
                    // Calculate output.
                    int outputClass = OutputClass(trainingInstances[p].Item1);
 
                    // Calculate error.
                    double localError = trainingInstances[p].Item2 - outputClass;

                    if (localError != 0)
                    {
						double updateWeight = learningRate * localError * trainingInstances[p].Item3;

                        // Update weights.
                        for (int i = 0; i < weights.Length; i++)
                        {
                            weights[i] += updateWeight * trainingInstances[p].Item1[i];
                        }
                    }
 
                    // Convert error to absolute value.
                    globalError += Math.Abs(localError);

					//Console.WriteLine (weights.FoldToString () + ", le " + localError + ", out " + outputClass + ", train " + trainingInstances[p].Item1.FoldToString ());
                }
 
                //Console.WriteLine("Iteration {0}\tError {1}", iteration, globalError);
                iteration++;
 
            } while (globalError != 0 && iteration < maxIterations);

			weights.NormalizeInPlace ();

		}

		//TODO: Think about this: Could it be converted to an element of [0, 1] if normalized and transformed?
		public double Output(double[] instance){
			return weights.CrossProduct(instance);
		}

		public int OutputClass(double[] instance)
        {
            return (Output (instance) > 0) ? 1 : -1;
        }

		public override string ToString(){
			return "{" + "Perceptron: learning rate = " + learningRate + ", max iterations = " + maxIterations + "\n" + 
				weights.FoldToString (weight => weight.ToString ("F2")) + 
					"}";
		}

    }

	public enum PerceptronTrainingMode{
		TRAIN_ALL_DATA = 0,
		TRAIN_EVEN_SIZE = 1,
		TRAIN_EVEN_WEIGHTS = 2
	}

	[Flags]
	public enum PerceptronClassificationMode{
		NOFLAGS = 0,
		USE_NEGATIVES = 1,
		USE_SCORES = 2,
		USE_SCORES_NEGATIVES = 3
	}

	public class PerceptronCollection : IProbabalisticClassifier
	{
		TupleStruct<Perceptron, int[]>[] perceptrons; //The perceptron and the classes it pertains to.

		double perceptronCountFactor;

		PerceptronTrainingMode trainingMode;

		PerceptronClassificationMode classificationMode;

		string[] classes;
		public string[] GetClasses(){
			return classes;
		}
		public void Train(IEnumerable<LabeledInstance> trainingData){
			trainingData = trainingData.ToArray(); //TODO performance.
			classes = trainingData.Select (item => item.label).Distinct ().Order ().ToArray ();

			Dictionary<string, int> classLookup = classes.IndexLookupDictionary();

			int dimension = trainingData.First().values.Length; //TODO 0 case.

			int perceptronCount = (int)Math.Ceiling(Math.Log(classes.Length, 2) * perceptronCountFactor); //Need at least log_2 perceptrons to be able to represent any item with a TRUE combination.  Take twice as many to improve predictive power.

			//TODO Pick classes better.  This technique favors larger classes. (Use training modes).
			switch(trainingMode){
				case PerceptronTrainingMode.TRAIN_ALL_DATA:
				{
					//TODO Don't shuffle every time.  Highly inefficient.
					perceptrons = Enumerable.Range (0, perceptronCount).Select (
						val => buildPerceptronForRandomClasses(trainingData.Shuffle().Select (instance => 
					    	new TupleStruct<double[], int>(instance.values, classLookup[instance.label])), 
					        dimension)
					).ToArray ();
					break;
				}
				case PerceptronTrainingMode.TRAIN_EVEN_SIZE:
				{
					IEnumerable<IGrouping<int, LabeledInstance>> byLabel = trainingData.GroupBy (item => classLookup[item.label]); //TODO: What type of enumerable does this return?  Can it be enumerated multiple times efficiently?
					int[] classCounts = new int[classes.Length];
					foreach(IGrouping<int, LabeledInstance> grp in byLabel){ //TODO: Might be nice to have a higher order for this, like ToDictionary but with first type an int.
						classCounts[grp.Key] = grp.Count ();
					}
					perceptrons = Enumerable.Range (0, perceptronCount).Select (index => 
				        {
							int[] classIndices = Enumerable.Range (0, classes.Length).Shuffle ().Take(classes.Length / 2).ToArray();
							Perceptron perceptron = buildPerceptronEvenClassSizes(byLabel, new HashSet<int>(classIndices), classCounts.Min (), dimension);
							return new TupleStruct<Perceptron, int[]>(perceptron, classIndices);
						}
					).ToArray ();
					break;
				}
				case PerceptronTrainingMode.TRAIN_EVEN_WEIGHTS:
				{
					IEnumerable<IGrouping<int, LabeledInstance>> byLabel = trainingData.GroupBy (item => classLookup[item.label]); //TODO: What type of enumerable does this return?  Can it be enumerated multiple times efficiently?
					int[] classCounts = new int[classes.Length];
					foreach(IGrouping<int, LabeledInstance> grp in byLabel){ //TODO: Might be nice to have a higher order for this, like ToDictionary but with first type an int.
						classCounts[grp.Key] = grp.Count ();
					}
					perceptrons = Enumerable.Range (0, perceptronCount).Select (index => 
				        {
							int[] classIndices = Enumerable.Range (0, classes.Length).Shuffle ().Take(classes.Length / 2).ToArray();
							Perceptron perceptron = buildPerceptronEvenWeights(byLabel, new HashSet<int>(classIndices), classCounts, dimension);
							return new TupleStruct<Perceptron, int[]>(perceptron, classIndices);
						}
					).ToArray ();
					break;
				}
			}
				//TODO: Next Up: Learning Ensemble.  
		}

		//Here we take in an IGrouping, since one probably already exists.  It could easily just be a list.
		private Perceptron buildPerceptronEvenWeights(IEnumerable<IGrouping<int, LabeledInstance>> groupsByLabel, HashSet<int> positiveClasses, int[] groupSizes, int dimension){
			TupleStruct<double[], int, double>[] perceptronTrainingData = groupsByLabel.SelectMany(
				grp => grp.Select (instance => new TupleStruct<double[], int, double>(instance.values, (positiveClasses.Contains(grp.Key) ? 1 : -1), 1.0 / groupSizes[grp.Key]))
			).ToArray();

			Perceptron p = new Perceptron(dimension); 
			p.Train (perceptronTrainingData);
			return p;
		}
		
		
		private Perceptron buildPerceptronEvenClassSizes(IEnumerable<IGrouping<int, LabeledInstance>> groupsByLabel, HashSet<int> positiveClasses, int minClassSize, int dimension){

    		//TODO: Solve the numbers dilema.

			//Approach 1, take a constant number of every class.  If less exist in a negative, they are not taken.  This may cause inflation of the positive class. 
			TupleStruct<double[], int>[] perceptronTrainingData = groupsByLabel.SelectMany(
				grp => grp.Take(minClassSize).Select (instance => new TupleStruct<double[], int>(instance.values, positiveClasses.Contains(grp.Key) ? 1 : -1))
			).ToArray();

			/*
			//Approach 2, take a constant number of negative examples.  Favors big negative classes.
			TupleStruct<double[], int>[] positiveData = groupsByLabel.Where (grp => positiveClasses.Contains(grp.Key)).SelectMany(
				grp => grp.Take(minClassSize).Select (instance => new TupleStruct<double[], int>(instance.values, 1))
			).ToArray();

			//TODO: This approach has its own problems: the negative data is biased
			TupleStruct<double[], int>[] negativeData = groupsByLabel.Where (grp => !positiveClasses.Contains(grp.Key)).Flatten1().Shuffle ().Take (minClassSize * positiveClasses.Count)
			*/

			Perceptron p = new Perceptron(dimension); 
			p.Train (perceptronTrainingData);
			return p;
		}

		private TupleStruct<Perceptron, int[]> buildPerceptronForRandomClasses(IEnumerable<TupleStruct<double[], int>> trainingData, int dimension){
			int[] positiveClasses = trainingData.Select (item => item.Item2).Distinct ().Take (classes.Length / 2).ToArray ();
			HashSet<int> positiveClassesHash = new HashSet<int>(positiveClasses);
			Perceptron perceptron = new Perceptron(dimension);
			perceptron.Train(trainingData.Select (item => new TupleStruct<double[], int>(item.Item1, positiveClassesHash.Contains (item.Item2) ? 1 : -1)).ToArray());

			return new TupleStruct<Perceptron, int[]>(perceptron, positiveClasses);
		}

		public double[] Classify(double[] values){
			double[] result = new double[classes.Length];
			foreach(TupleStruct<Perceptron, int[]> perceptron in perceptrons){
				double score;
				if(classificationMode.HasFlag (PerceptronClassificationMode.USE_SCORES)){
					score = perceptron.Item1.Output(values);
				}
				else{
					score = perceptron.Item1.OutputClass(values);
				}
				if(classificationMode.HasFlag(PerceptronClassificationMode.USE_NEGATIVES) || score > 0) foreach(int i in perceptron.Item2){
					result[i] += score;
				}
			}

			//Prevent negatives and all 0 classifications.
			double minVal = .0000001;
			result.MapInPlace(item => (item < minVal) ? minVal : item);

			/*
	 		if(classificationMode.HasFlag (PerceptronClassificationMode.USE_NEGATIVES)){
				result.MapInPlace (item => (item < 0) ? (double.Epsilon * classes.Length) : item); //DO NOT set to 0, if all are set to 0, NormalizeSumInPlace will crash and burn.  Not sure if multiplication by classes.Count is necessary; this is to avoid strange numerics artifacts.
			}
			 */


			return result.NormalizeSumInPlace ();
		}

		public PerceptronCollection (double extraFactor = 2.0, PerceptronTrainingMode trainingMode = PerceptronTrainingMode.TRAIN_ALL_DATA, PerceptronClassificationMode classificationMode = PerceptronClassificationMode.NOFLAGS)
		{
			this.perceptronCountFactor = extraFactor;
			this.trainingMode = trainingMode;
			this.classificationMode = classificationMode;
		}

		public override string ToString(){
			IEnumerable<string> perceptronStrings = perceptrons.Select (tup => tup.Item2.Select (index => classes[index]).FoldToString () + ": " + tup.Item1.ToString ());
			return "{" + "Perceptron Collection Probabalistic Classifier: " + "perceptron count factor = " + perceptronCountFactor + ":\n" +
				perceptrons.Length + " perceptrons for " + classes.Length + " classes:\n" + 
				perceptronStrings.FoldToString ("{", "}", ",\n")
				+ "}";
		}
	}
}

