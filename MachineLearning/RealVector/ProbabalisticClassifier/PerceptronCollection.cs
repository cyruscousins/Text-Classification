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
		double[] weights;
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

		public double Output(double[] instance){
			return weights.CrossProduct(instance);
		}

		public int OutputClass(double[] instance)
        {
            return (Output (instance) > 0) ? 1 : -1;
        }

		public override string ToString(){
			return "{" + "Perceptron: learning rate = " + learningRate + ", max iterations = " + maxIterations + "\n" + 
				weights.FoldToString () + 
					"}";
		}

    }

	public class PerceptronCollection : IProbabalisticClassifier
	{
		TupleStruct<Perceptron, int[]>[] perceptrons; //The perceptron and the classes it pertains to.

		double perceptronCountFactor;

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

			//TODO Pick classes better.  This technique favors larger classes.  
			//TODO Don't shuffle every time.  Highly inefficient.
			perceptrons = Enumerable.Range (0, perceptronCount).Select (
				val => buildPerceptronForRandomClasses(trainingData.Shuffle().Select (instance => 
			    	new TupleStruct<double[], int>(instance.values, classLookup[instance.label])), 
			        dimension)
			).ToArray ();
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
				double score = perceptron.Item1.Output(values);
				if(score > 0) foreach(int i in perceptron.Item2){ //Why only use positives (other than avoiding - %)?  Should this be a mode?
					result[i] += score;
				}
			}

			//TODO: bool useNegatives, bool useScores.

			return result.NormalizeSumInPlace ();
		}

		public PerceptronCollection (double extraFactor = 2.0)
		{
			this.perceptronCountFactor = extraFactor;
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

