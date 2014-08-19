using System;

namespace TextCharacteristicLearner
{
	public class ZScoreNomalizerSynthesizerWrapper<Ty> : IFeatureSynthesizer<Ty>
	{
		IFeatureSynthesizer<Ty> synth;

		public ZScoreNomalizerSynthesizerWrapper (IFeatureSynthesizer<Ty> synth){
			this.synth = synth;
		}

		public void Train(DiscreteSeriesDatabase<Ty> data){
			synth.Train (data);
		}
	}
}

