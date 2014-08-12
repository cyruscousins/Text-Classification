using System;

using System.Linq;

using Whetstone;

namespace TextCharacteristicLearner
{
	public class CompoundFeatureSynthesizer<Ty> : IFeatureSynthesizer<Ty>
	{
		//A compound feature synthesizer holds other feature synthesizers and concatenates their results.
		//Training can all be done on the same data safely.

		IFeatureSynthesizer<Ty>[] synths;

		//Data Accessors:

		public string ClassificationCriterion{get; private set;}

		public CompoundFeatureSynthesizer(string criterion, IFeatureSynthesizer<Ty>[] synths){
			ClassificationCriterion = criterion;
			this.synths = synths;
		}

		//Get the names of the features being synthesized.
		public string[] GetFeatureSchema ()
		{
			return synths.SelectMany (synth => synth.GetFeatureSchema()).ToArray ();
		}


		//Construction:

		public bool NeedsTraining {
			get {
				return synths.Select (synth => synth.NeedsTraining).Disjunction ();
			}
		}



		//Train an IFeatureSynthesizer model.
		//This function shall be called before SynthesizeFeatures iff NeedsTraining
		public void Train(DiscreteSeriesDatabase<Ty> data){
			foreach(IFeatureSynthesizer<Ty> synth in synths){
				if(synth.NeedsTraining) synth.Train(data);
			}
		}

		//Calculation:

		//Synthesize features for an item.
		//TODO: Enforce contract
		public double[] SynthesizeFeatures(DiscreteEventSeries<Ty> item){
			return synths.SelectMany (synth => synth.SynthesizeFeatures(item)).ToArray();
		}
		
		public override string ToString(){
			return "{Compound Feature Synthesizer: [Training data configuration not yet available] on " + ClassificationCriterion + "\n" +
				synths.Length + " synthesizers.\n" + 
				synths.FoldToString ("{", "}", ",\n") + "\n" +
				"}";
		}
	}
}

