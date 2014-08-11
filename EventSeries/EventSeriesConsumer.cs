using System;

using System.Collections.Generic;

namespace TextCharacteristicLearner
{
	public interface EventSeriesConsumer<Ty>
	{
		void ConsumeEventSeries(IEnumerable<Ty> series);
	}

	public static class EventSeriesConsumerExtensions{
		public static void ConsumeEventSeries<Ty>(this EventSeriesConsumer<Ty> consumer, DiscreteEventSeries<Ty> series){
			consumer.ConsumeEventSeries(series.data);
		}
	}
}
