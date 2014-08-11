using System;

using System.Collections.Generic;

using System.Linq;
using Whetstone;

namespace TextCharacteristicLearner
{

	//This class is equivalent to a RegressorFeatureSynthesizer loaded with VarKmerFrequencyRegressors, but it scales quite a bit better.
	public class VarKmerFrequencyFeatureSynthesizer<Ty> : IFeatureSynthesizer<Ty>
	{
		//PUBLIC DATA
		public uint k;

		public uint minKmerCount;

		public uint kmersToTake;

		public double smoothingAmt;

		public bool useUncategorizedForBaseline;

		//...

		//Constructor
		public VarKmerFrequencyFeatureSynthesizer(string criterion, uint k){
			this.ClassificationCriterion = criterion;
			this.k = k;
			
			minKmerCount = 2;
			kmersToTake = 50;
			smoothingAmt = 1.0;
			useUncategorizedForBaseline = false;
		}

		public VarKmerFrequencyFeatureSynthesizer(string criterion, uint k, uint minKmerCount, uint kmersToTake, double smoothingAmt, bool useUncategorizedForBaseline){
			this.ClassificationCriterion = criterion;
			this.k = k;

			this.minKmerCount = minKmerCount;
			this.kmersToTake = kmersToTake;
			this.smoothingAmt = smoothingAmt;
			this.useUncategorizedForBaseline = useUncategorizedForBaseline;
		}


		//PRIVATE DATA:
		//strings (classes) mapped to ints (indices)
		Dictionary<string, int> classLookup;

		//Dictionary of kmers onto a the set of all classes (ints) and their weights (doubles).
		Dictionary<Kmer<Ty>, Dictionary<int, double>> learnedCharacteristicKmers; //TODO May be good to make the second dictionary a lookup array...

		int classCount;

		//Construction:

		//Implementation:

		public string ClassificationCriterion{ get; private set; }

		//Get the names of the features being synthesized.
		public string[] GetFeatureSchema(){
			return classLookup.OrderBy(kvp => kvp.Value).Select (kvp => kvp.Key).ToArray ();
		}

		public bool NeedsTraining{get{return true;}}
		//Train an IFeatureSynthesizer model.
		public void Train(DiscreteSeriesDatabase<Ty> trainingData){
			//Partition into class and classless groups.
			Tuple<IEnumerable<DiscreteEventSeries<Ty>>, IEnumerable<DiscreteEventSeries<Ty>>> partitioned = trainingData.data.Partition(item => item.labels.ContainsKey (ClassificationCriterion));
			IEnumerable<DiscreteEventSeries<Ty>> classedSeries = partitioned.Item1;
			IEnumerable<DiscreteEventSeries<Ty>> classlessSeries = partitioned.Item2; //These items does not have a class over the category label for which the feature synthezer is being created.

			TupleStruct<string, MultisetKmer<Ty>>[] classes = classedSeries.AsParallel()
				.GroupBy (series => series.labels[ClassificationCriterion]) //Group by class
				.Select (grp => new TupleStruct<string, MultisetKmer<Ty>>(grp.Key, ((IEnumerable<DiscreteEventSeries<Ty>>)grp).ToMultisetVarKmer<Ty> (k))) //Make classes into a single multiset each.
				.OrderBy (tup => tup.Item1) //Sort by name
				.ToArray ();

			/*
			Console.WriteLine("GROUPS");
			foreach(var v in classes){
				Console.WriteLine ("Class " + v.Item1 + " size " + v.Item2.Size ());
			}
			*/


			MultisetKmer<Ty> baseline;
			if(useUncategorizedForBaseline){
				baseline = classlessSeries.ToMultisetVarKmer(k).Cons (classes.Select (@class => @class.Item2)).sumKmers (); //TODO reuse the classless multiset.
			}
			else{
				baseline = classedSeries.ToMultisetVarKmer(k);
			}

			//We now have data for all classes and the baseline.

			//Create the data structures.

			classCount = classes.Length;

			//Lookup for all class strings.
			classLookup = classes.Select (tup => tup.Item1).IndexLookupDictionary();

			//Console.WriteLine ("Training.  " + classes.Length + " classes, " + trainingData.data.Count + " instances.");

			/* Not parallelized:
			foreach(int classIndex in Enumerable.Range (0, classCount)){
				//All the kmers found in this class.
				TupleStruct<string, MultisetKmer<Ty>> thisClass = classes[classIndex];
				List<TupleStruct<Kmer<Ty>, double>> thisClassCharacteristicKmersStore = new List<TupleStruct<Kmer<Ty>, double>>(); 
				foreach(KeyValuePair<Kmer<Ty>, uint> kvp in thisClass.Item2){
					if(kvp.Value > minKmerCount){
						double thisFreq = kvp.Value / (double) thisClass.Item2.Size (kvp.Key.data.Count);
						double baseFreq = baseline.GetKeyFracLaplace(kvp.Key, smoothingAmt);

						//Console.WriteLine ("Class: " +  classIndex + " Kmer: " + kvp.Value + ", class freq " + thisFreq + ", base freq " + baseFreq);

						//TODO: Advanced logic.
						if(thisFreq > baseFreq){
							double kmerValue = thisFreq - baseFreq;
							//Console.WriteLine ("Adding kmer " + kvp.Key + " weight " + kmerValue + " for class " + classIndex);
							thisClassCharacteristicKmersStore.Add (new TupleStruct<Kmer<Ty>, double>(kvp.Key, kmerValue));
						}
					}
				}
				foreach(TupleStruct<Kmer<Ty>, double> kmerToAdd in thisClassCharacteristicKmersStore.OrderBy (tup => Math.Abs (tup.Item2)).Take ((int)kmersToTake)){ //TODO: Unordered kth order statistic.
					learnedCharacteristicKmers.GetWithDefaultAndAdd(kmerToAdd.Item1, () => new Dictionary<int, double>(classCount))[classIndex] = kmerToAdd.Item2;
				}
			}
			*/

			//Parallelized (find characteristic kmers for each class in parallel)

			IEnumerable<TupleStruct<int, IEnumerable<TupleStruct<Kmer<Ty>, double>>>> characteristicKmers = Enumerable.Range(0, classCount).AsParallel ().Select(index => new TupleStruct<int, IEnumerable<TupleStruct<Kmer<Ty>, double>>> (index, ExtractCharacteristicKmersForClass(index, classes[index].Item2, baseline)));

			//This part probably can't be parallelized (adding to same dictionary), but should be light
			learnedCharacteristicKmers = new Dictionary<Kmer<Ty>, Dictionary<int, double>>();

			foreach(TupleStruct<int, IEnumerable<TupleStruct<Kmer<Ty>, double>>> thisClass in characteristicKmers){ //TODO: Unordered kth order statistic.

				//Console.WriteLine ("Class " + thisClass.Item1 + " contains " + thisClass.Item2.Count() + " above average kmers.");


				int thisClassIndex = thisClass.Item1;
				IEnumerable<TupleStruct<Kmer<Ty>, double>> thisClassCharacteristicKmersStore = thisClass.Item2;
				foreach(TupleStruct<Kmer<Ty>, double> kmerToAdd in thisClassCharacteristicKmersStore){
					learnedCharacteristicKmers.GetWithDefaultAndAdd(
						kmerToAdd.Item1, 
						() => new Dictionary<int, double>(classCount / 2)  //TODO: Dictionary size, we're guessing is classCount / 2, this is a bad heuristic.
					)[thisClassIndex] = kmerToAdd.Item2;
				}
			}

			//TODO: Negative Kmers (note, may complicate sizing.  Will not work wittout a lot of data.)

		}

		//Extract characteristic kmers (top n more common than baseline that occur at least q times).
		private IEnumerable<TupleStruct<Kmer<Ty>, double>> ExtractCharacteristicKmersForClass(int classIndex, MultisetKmer<Ty> thisClass, MultisetKmer<Ty> baseline){
			List<TupleStruct<Kmer<Ty>, double>> thisClassCharacteristicKmersStore = new List<TupleStruct<Kmer<Ty>, double>>(); 
			foreach(KeyValuePair<Kmer<Ty>, uint> kvp in thisClass){
				if(kvp.Value > minKmerCount){
					double thisFreq = kvp.Value / (double) thisClass.Size (kvp.Key.data.Count);
					double baseFreq = baseline.GetKeyFracLaplace(kvp.Key, smoothingAmt);

					//Console.WriteLine ("Class: " +  classIndex + " Kmer: " + kvp.Value + ", class freq " + thisFreq + ", base freq " + baseFreq);

					//TODO: Advanced logic.
					if(thisFreq > baseFreq){
						double kmerValue = thisFreq / baseFreq - 1;
						//Console.WriteLine ("Adding kmer " + kvp.Key + " weight " + kmerValue + " for class " + classIndex);
						thisClassCharacteristicKmersStore.Add (new TupleStruct<Kmer<Ty>, double>(kvp.Key, kmerValue));
					}
				}
			}
			return thisClassCharacteristicKmersStore.OrderByDescending (tup => Math.Abs (tup.Item2)).Take ((int)kmersToTake);
		}

		/*
		private IEnumerable<TupleStruct<Kmer<Ty>, double>> ExtractUncharacteristicKmersForClass (int classIndex, MultisetKmer<Ty> thisClass, MultisetKmer<Ty> baseline)
		{
			//This will only work with an enormous amount of data for low k.
		}
		*/
		
		
		//Calculation:

		//Synthesize features for an item.
		//TODO: Enforce contract
		public double[] SynthesizeFeatures(DiscreteEventSeries<Ty> item){
			double[] vals = new double[classCount];

			MultisetKmer<Ty> ms = item.ToMultisetVarKmer<Ty>(k);

			foreach(KeyValuePair<Kmer<Ty>, uint> kvp in ms){
				Dictionary<int, double> classesWithKvp;
				if(learnedCharacteristicKmers.TryGetValue (kvp.Key, out classesWithKvp)){
					//Console.WriteLine ("\tFound kmer " + kvp.Key + ".");
					foreach(KeyValuePair<int, double> @class in classesWithKvp){
						//Console.WriteLine ("\t\tClass " + @class.Key + ", Value " + @class.Value + ", Times " + kvp.Value);
						vals[@class.Key] += kvp.Value * @class.Value;
					}
				}
			}

			return vals;
		}

		//This function is very complicated, as it has to in a sense invert the manner in which data is stored.
		public override string ToString(){
			List<TupleStruct<Kmer<Ty>, double>>[] vals = Enumerable.Range (0, classCount).Select (i => new List<TupleStruct<Kmer<Ty>, double>>()).ToArray ();

			foreach(KeyValuePair<Kmer<Ty>, Dictionary<int, double>> kmerKvp in learnedCharacteristicKmers){
				foreach(KeyValuePair<int, double> scores in kmerKvp.Value){
					vals[scores.Key].Add (new TupleStruct<Kmer<Ty>, double>(kmerKvp.Key, scores.Value));
				}
			}

			//Classifier Name
			return "{Variable Kmer Frequency to Classes Analyzer on \"" + ClassificationCriterion + "\" (" +
				//Classifier Variables
				"k;Min Kmer Count;Kmers Per Class;Smoothing Amount".Split (';').Zip (new[]{k, minKmerCount, kmersToTake, smoothingAmt}, (name, val) => name + " = " + val.ToString ("0.###")).FoldToString ("", "", ", ") + ", Use unclassified data in baseline = " + useUncategorizedForBaseline + "):" + "\n" +
				//Classes
				GetFeatureSchema().Zip (vals, (className, classKmers) => 
					//Class Name
					className + ": " + 
					//Class Kmers
				   	classKmers.OrderByDescending (tup => Math.Abs (tup.Item2)).FoldToString (item => item.Item1 + ":" + item.Item2.ToString("F3"))
			    ).FoldToString("{", "}", ",\n");
		}

	}
}
