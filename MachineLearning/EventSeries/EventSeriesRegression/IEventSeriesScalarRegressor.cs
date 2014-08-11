using System;

using System.Collections.Generic;

namespace TextCharacteristicLearner
{

	//This interface represents a model that regresses a value for an event series.
	//The model contract function is thus f:(EventSeries<Ty> -> R).
	public interface IEventSeriesScalarRegressor<A>
	{
		string label {get;}

		//void Train(IEnumerable<DiscreteEventSeries<A>> data);

		double RegressEventSeries(DiscreteEventSeries<A> series);

	}
}

