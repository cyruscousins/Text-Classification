using System;

using System.Collections.Generic;

namespace TextCharacteristicLearner
{
	public interface IEventSeriesProbabalisticClassifier<Ty>
	{
		string[] GetClasses();
		void Train(DiscreteSeriesDatabase<Ty> series);

		double[] Classify(DiscreteEventSeries<Ty> series);
	}
}

