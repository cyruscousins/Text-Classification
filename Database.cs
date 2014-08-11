using System;

using System.Collections;
using System.Collections.Generic;

using Whetstone;
using System.Linq;

using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace TextCharacteristicLearner
{
	public class DiscreteSeriesDatabase<Ty> : IEnumerable<DiscreteEventSeries<Ty>>//TODO: Rename DiscreteEventSeriesDatabase
	{

		public List<DiscreteEventSeries<Ty>> data;

		public DiscreteSeriesDatabase ()
		{
			data = new List<DiscreteEventSeries<Ty>>(); //TODO empty set
		}
		public DiscreteSeriesDatabase(List<DiscreteEventSeries<Ty>> data){
			this.data = data;
		}
		
		public DiscreteSeriesDatabase(IEnumerable<DiscreteEventSeries<Ty>> data)
		{
			this.data = data.ToList ();
		}

		public IEnumerable<string> getLabelCriteria(){
			return data.SelectMany (a => a.labels.Keys).Distinct();
		}

		public IEnumerable<string> getLabelClasses(string criterion){
			return data.Select (a => a.labels.GetWithDefault (criterion, "[none]")).Distinct ();
		}

		//Creator functions, make a new database from an existing one.

		public DiscreteSeriesDatabase<Ty> Filter(Predicate<DiscreteEventSeries<Ty>> filter){
			return new DiscreteSeriesDatabase<Ty>(data.Where (item => filter(item)));
		}

		public DiscreteSeriesDatabase<Ty> FilterForCriterion(string criterion){
			return Filter (item => item.labels.ContainsKey(criterion));
		}
		
		public DiscreteSeriesDatabase<Ty> FilterForCriterion(string criterion, string value){
			return Filter (item => item.labels.GetWithDefault(criterion) == value);
		}

		public Tuple<DiscreteSeriesDatabase<Ty>, DiscreteSeriesDatabase<Ty>> SplitDatabase(double frac){
			DiscreteEventSeries<Ty>[] shuffled = data.Shuffle ();
			Tuple<List<DiscreteEventSeries<Ty>>, List<DiscreteEventSeries<Ty>>> split = shuffled.SplitToLists((int)(frac * shuffled.Length));
			return new Tuple<DiscreteSeriesDatabase<Ty>, DiscreteSeriesDatabase<Ty>>(new DiscreteSeriesDatabase<Ty>(split.Item1), new DiscreteSeriesDatabase<Ty>(split.Item2));
		}

		//Helpful functions

		public int TotalItemCount(){ //TODO: Unnecessary with change to IEnumerable.
			return data.TotalItemCount();
		}

		public string[] CriterionClasses(string criterion){
			return data.Map (item => item.labels.GetWithDefault (criterion, "[none]")).Distinct ().ToArray ();
		}

		//IEnumerable

		public IEnumerator<DiscreteEventSeries<Ty>> GetEnumerator(){
			return data.GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator(){
			return GetEnumerator ();
		}
	}

}

