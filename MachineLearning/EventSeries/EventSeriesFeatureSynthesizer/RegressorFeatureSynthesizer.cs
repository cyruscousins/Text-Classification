using System;

using System.Collections.Generic;

using System.Linq;

using Whetstone;

namespace TextCharacteristicLearner
{
	//The regressor feature synthesizer synthesizes features (double's) using regressors (IEventSeriesScalarRegressor's)
	//Like any IFeatureSynthesizer, the RegressorFeaturesSynthesizer operates on DiscreteEventSeries's.
	public abstract class RegressorFeatureSynthesizer<Ty> : IFeatureSynthesizer<Ty>
	{
		public string ClassificationCriterion{ get; private set;}

		IEventSeriesScalarRegressor<Ty>[] regressors;


		public RegressorFeatureSynthesizer (string catName)
		{
			this.ClassificationCriterion = catName;
		}
		
		public bool NeedsTraining{get{return true;}}
		//Get the names of the features being synthesized.
		public string[] GetFeatureSchema(){
			return regressors.Select(r => r.label).ToArray ();
		}

		//Construction:

		//Train an IFeatureSynthesizer model.
		public void Train(DiscreteSeriesDatabase<Ty> data){
			regressors = CreateRegressors(data);
		}

		protected abstract IEventSeriesScalarRegressor<Ty>[] CreateRegressors(DiscreteSeriesDatabase<Ty> data);

		//Calculation:

		//Synthesize features for an item.
		//TODO: Enforce contract
		public double[] SynthesizeFeatures(DiscreteEventSeries<Ty> item){
			return regressors.Map (r => r.RegressEventSeries(item)).ToArray();
		}

		public override string ToString(){
			return "{Classifier on \"" + ClassificationCriterion + "\":\n" + regressors.FoldToString("{", "}", ",\n") + "\n}";
		}

	}

	public class RegressorFeatureSynthesizerFrequencies<Ty> : RegressorFeatureSynthesizer<Ty> {

		//Model Parameters:
		uint minSignificantCount;
		uint smoothingAmount;
		uint featuresToUse;

		//Constructor sets model parameters
		public RegressorFeatureSynthesizerFrequencies (string categoryLabel, uint minSignificantCount, uint smoothingAmount, uint featuresToUse) : base(categoryLabel)
		{
			this.minSignificantCount = minSignificantCount;
			this.smoothingAmount = smoothingAmount;
			this.featuresToUse = featuresToUse;
		}

		protected override IEventSeriesScalarRegressor<Ty>[] CreateRegressors(DiscreteSeriesDatabase<Ty> data){

			//Partition into class and classless groups.
			Tuple<IEnumerable<DiscreteEventSeries<Ty>>, IEnumerable<DiscreteEventSeries<Ty>>> partitioned = data.data.Partition(item => item.labels.ContainsKey (ClassificationCriterion));
			IEnumerable<DiscreteEventSeries<Ty>> noClass = partitioned.Item2; //This item does not have a class over the category label for which the feature synthezer is being created.

			IEnumerable<DiscreteEventSeries<Ty>> inClass = partitioned.Item1;

			IEnumerable<IGrouping<string, DiscreteEventSeries<Ty>>> groupings = inClass.GroupBy (item => item.labels[ClassificationCriterion]);

			//Establish multisets for each class

			IEnumerable<Tuple<string, Multiset<Ty>>> classSets = groupings.Map (grp => Tuple.Create (grp.Key, grp.ToMultiset ())).ToArray(); //Used twice.  Make it an array.

			//Establish the baseline (all data)
			Multiset<Ty> baseline = noClass.ToMultiset ().Cons(classSets.Select (a => a.Item2)).sum();

			return classSets.Map (ntp => new ItemFrequencyRegressor<Ty>(ntp.Item1, minSignificantCount, smoothingAmount, featuresToUse, baseline, ntp.Item2)).ToArray ();
		}
	}

	
	public class RegressorFeatureSynthesizerKmerFrequencies<Ty> : RegressorFeatureSynthesizer<Ty> {

		//Model Parameters:
		uint k;
		uint minSignificantCount;
		uint smoothingAmount;
		uint featuresToUse;

		//Constructor sets model parameters
		public RegressorFeatureSynthesizerKmerFrequencies (string categoryLabel, uint minSignificantCount, uint smoothingAmount, uint featuresToUse, uint k) : base(categoryLabel)
		{
			this.minSignificantCount = minSignificantCount;
			this.smoothingAmount = smoothingAmount;
			this.featuresToUse = featuresToUse;
			this.k = k;
		}

		protected override IEventSeriesScalarRegressor<Ty>[] CreateRegressors(DiscreteSeriesDatabase<Ty> data){

			//Partition into class and classless groups.
			Tuple<IEnumerable<DiscreteEventSeries<Ty>>, IEnumerable<DiscreteEventSeries<Ty>>> partitioned = data.data.Partition(item => item.labels.ContainsKey (ClassificationCriterion));
			IEnumerable<DiscreteEventSeries<Ty>> noClass = partitioned.Item2; //This item does not have a class over the category label for which the feature synthezer is being created.

			IEnumerable<DiscreteEventSeries<Ty>> inClass = partitioned.Item1;

			IEnumerable<IGrouping<string, DiscreteEventSeries<Ty>>> groupings = inClass.GroupBy (item => item.labels[ClassificationCriterion]);

			//Establish multisets for each class

			IEnumerable<Tuple<string, Multiset<Kmer<Ty>>>> classSets = groupings.Map (grp => Tuple.Create (grp.Key, grp.ToMultisetKmer (k))).ToArray(); //Used twice.  Make it an array.

			//Establish the baseline (all data)
			Multiset<Kmer<Ty>> baseline = noClass.ToMultisetKmer (k).Cons(classSets.Select (a => a.Item2)).sum();

			return classSets.Map (ntp => new ItemKmerFrequencyRegressor<Ty>(ntp.Item1, minSignificantCount, smoothingAmount, featuresToUse, k, baseline, ntp.Item2)).ToArray ();
		}
	}
	
	public class RegressorFeatureSynthesizerKmerFrequenciesVarK<Ty> : RegressorFeatureSynthesizer<Ty> {

		//Model Parameters:
		uint k;
		uint minSignificantCount;
		uint smoothingAmount;
		uint featuresToUse;

		//Constructor sets model parameters
		public RegressorFeatureSynthesizerKmerFrequenciesVarK (string categoryLabel, uint minSignificantCount, uint smoothingAmount, uint featuresToUse, uint k) : base(categoryLabel)
		{
			this.minSignificantCount = minSignificantCount;
			this.smoothingAmount = smoothingAmount;
			this.featuresToUse = featuresToUse;
			this.k = k;
		}

		protected override IEventSeriesScalarRegressor<Ty>[] CreateRegressors(DiscreteSeriesDatabase<Ty> data){

			//Partition into class and classless groups.
			Tuple<IEnumerable<DiscreteEventSeries<Ty>>, IEnumerable<DiscreteEventSeries<Ty>>> partitioned = data.data.Partition(item => item.labels.ContainsKey (ClassificationCriterion));
			IEnumerable<DiscreteEventSeries<Ty>> noClass = partitioned.Item2; //This item does not have a class over the category label for which the feature synthezer is being created.

			IEnumerable<DiscreteEventSeries<Ty>> inClass = partitioned.Item1;

			IEnumerable<IGrouping<string, DiscreteEventSeries<Ty>>> groupings = inClass.GroupBy (item => item.labels[ClassificationCriterion]);

			//Establish multisets for each class (parallelized).
			Tuple<string, MultisetKmer<Ty>>[] classSets = groupings.AsParallel ().Select (grp => Tuple.Create (grp.Key, grp.ToMultisetVarKmer (k))).ToArray(); //Used twice.  Make it an array.

			//Establish the baseline (all data)
			MultisetKmer<Ty> baseline = noClass.ToMultisetVarKmer (k).Cons(classSets.Select (a => a.Item2)).sumKmers();

			//Create regressors (in parallel).
			return classSets.AsParallel ().Select (ntp => new ItemVarKmerFrequencyRegressor<Ty>(ntp.Item1, minSignificantCount, smoothingAmount, featuresToUse, k, baseline, ntp.Item2)).ToArray ();
		}
	}

	/*
	public class RegressorFeatureSynthesizerVarKmerFrequencies<Ty> : RegressorFeatureSynthesizer<Ty>
	{
		//TODO: Model off the following class
	}
	*/

	/*
	 * 
	public class ClassBasedFeatureSynthesizerKmer<Tyvar>
	{
		uint k;
		public string name;
		public string categoryName;
		public IEnumerable<ClassCharacteristicSetKmer<Tyvar>> classes;

		public ClassBasedFeatureSynthesizerKmer (uint k, string name, string labelCategory, IEnumerable<ClassCharacteristicSetKmer<Tyvar>> classes)
		{
			this.k = k;
			this.name = name;
			this.categoryName = labelCategory;
			this.classes = classes;
		}

		public IEnumerable<string> getClassFeatureNames(){
			return classes.Map(c => c.name);
		}

		public double[] classifyAsVector(IEnumerable<Tyvar> t){
			return classes.Map (c => c.evaluateSet(t)).ToArray ().NormalizeSum();
		}

		public double[] classifyAsVector(DiscreteEventSeries<Tyvar> t){
			return classifyAsVector(t.data);
		}

		public string classifyAsClass(IEnumerable<Tyvar> t){
			return classes.ArgMax (c => c.evaluateSet (t)).name;
		}

		public string VectorSchema(){
			return classes.FoldToString (a => a.name);
		}
		public string ToString(int kmersToShow){
			return "{" + categoryName + ":" + classes.FoldToString (a => a.ToString (kmersToShow), "\n\t{", "}", "\n\t") + "}";
		}
		public override string ToString(){
			return ToString (50);
		}

		//Returns a value on [0, 1], higher representing a more accurate classifier.
		public double scoreClassifier(DiscreteSeriesDatabase<Tyvar> testData){

			Dictionary<string, int> classRanks = getClassFeatureNames().Select ((item, index) => new Tuple<string, int>(item, index)).ToDictionary(a => a.Item1, a => a.Item2);

			int classifications = 0;
			double score = 0;
			foreach(DiscreteEventSeries<Tyvar> item in testData.data){
				int correctClass;
				if(!classRanks.TryGetValue (item.labels[categoryName], out correctClass)){
					Console.WriteLine ("Classifier does not contain data for " + item.labels[categoryName] + ".  Skipping this item.");
					continue;
				}

				double[] scores = classifyAsVector (item).ToArray ();

				//int foundClass = scores.MaxIndex();

				//Scoring
				score += scores[correctClass];
				classifications ++;
			}

			score /= classifications;
			return score;
		}
	}

	*/
}

