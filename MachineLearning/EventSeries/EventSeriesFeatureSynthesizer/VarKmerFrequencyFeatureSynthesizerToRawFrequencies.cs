using System;

using System.Collections.Generic;

using System.Linq;
using Whetstone;

namespace TextCharacteristicLearner
{

	//This class is equivalent to a RegressorFeatureSynthesizer loaded with VarKmerFrequencyRegressors, but it scales quite a bit better.
	public class VarKmerFrequencyFeatureSynthesizerToRawFrequencies<Ty> : IFeatureSynthesizer<Ty>
	{
		//PUBLIC DATA
		public uint k;

		public uint minKmerCount;

		public uint kmersToTake;

		public double smoothingAmt;

		public bool useUncategorizedForBaseline;

		//...

		//Constructor
		public VarKmerFrequencyFeatureSynthesizerToRawFrequencies(string criterion, uint k){
			this.ClassificationCriterion = criterion;
			this.k = k;
			
			minKmerCount = 2;
			kmersToTake = 50;
			smoothingAmt = 1.0;
			useUncategorizedForBaseline = false;
		}

		public VarKmerFrequencyFeatureSynthesizerToRawFrequencies(string criterion, uint k, uint minKmerCount, uint kmersToTake, double smoothingAmt, bool useUncategorizedForBaseline){
			this.ClassificationCriterion = criterion;
			this.k = k;

			this.minKmerCount = minKmerCount;
			this.kmersToTake = kmersToTake;
			this.smoothingAmt = smoothingAmt;
			this.useUncategorizedForBaseline = useUncategorizedForBaseline;
		}

		//Dictionary of kmers onto a the set of all classes (ints) and their weights (doubles).
		Dictionary<Kmer<Ty>, int> kmersOntoIndex; //TODO May be good to make the second dictionary a lookup array...
		int kmerCount;

		//Construction:

		//Implementation:

		public string ClassificationCriterion{ get; private set; }

		//Get the names of the features being synthesized.
		public string[] GetFeatureSchema(){
			return kmersOntoIndex.OrderBy (kmer => kmer.Value).Select (kmer => kmer.Key.ToString ()).ToArray();
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

			IEnumerable<TupleStruct<Kmer<Ty>, double>> characteristicKmers = Enumerable.Range(0, classes.Length).AsParallel ().SelectMany(index => ExtractCharacteristicKmersForClass(index, classes[index].Item2, baseline));

			
			//Lookup for all kmers.

			kmersOntoIndex = characteristicKmers.OrderByDescending(item => item.Item2).Select(item => item.Item1).Distinct().Take((int)kmersToTake).IndexLookupDictionary();
			kmerCount = kmersOntoIndex.Count;


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
			double[] vals = new double[kmerCount];

			MultisetKmer<Ty> ms = item.ToMultisetVarKmer<Ty>(k);

			foreach(KeyValuePair<Kmer<Ty>, uint> kvp in ms){
				int index = 0;
				if(kmersOntoIndex.TryGetValue (kvp.Key, out index)){
					vals[index] = kvp.Value / (double) ms.Size ((uint)kvp.Key.data.Count);
				}
			}

			return vals;
		}

		public override string ToString(){
			//Classifier Name
			return "{Kmer Frequency based synthesizer trained on \"" + ClassificationCriterion + "\" (" +
				//Classifier Variables
				"k;Min Kmer Count;Kmers Per Class;Smoothing Amount".Split (';').Zip (new[]{k, minKmerCount, kmersToTake, smoothingAmt}, (name, val) => name + " = " + val.ToString ("0.###")).FoldToString ("", "", ", ") + ", Use unclassified data in baseline = " + useUncategorizedForBaseline + "):" + "\n" +
				//Classes
				"Kmers Analyzed: " + GetFeatureSchema().FoldToString() +
				"}";
		}

	}
}

