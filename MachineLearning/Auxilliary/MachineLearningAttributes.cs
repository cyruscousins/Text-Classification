using System;

namespace TextCharacteristicLearner
{
	public class AlgorithmParameterAttribute : Attribute{
		public string ParameterName;
		public int ParameterIndex;

		
	}

	public class AlgorithmNameAttribute : Attribute{
		public string AlgorithmName;
	}

	public class AlgorithmTrainingAttribute : Attribute{
		public string ParameterName;
	}
}

