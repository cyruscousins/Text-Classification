using System;

using System.Collections.Generic;
using System.Linq;

using Whetstone;

namespace TextCharacteristicLearner
{
	public class ClassBasedFeatureSynthesizer <Tyvar>
	{
		public string name;
		public IEnumerable<ClassCharacteristicSet<Tyvar>> classes;

		public ClassBasedFeatureSynthesizer (string name, IEnumerable<ClassCharacteristicSet<Tyvar>> classes)
		{
			this.name = name;
			this.classes = classes;
		}

		public IEnumerable<string> getClassFeatureNames(){
			return classes.Map(c => c.name);
		}

		public IEnumerable<double> classifyAsVector(Multiset<Tyvar> t){
			return classes.Map (c => c.evaluateSet(t));
		}

		public string classifyAsClass(Multiset<Tyvar> t){
			return classes.ArgMax (c => c.evaluateSet (t)).name;
		}

		public string VectorSchema(){
			return classes.FoldToString (a => a.name);
		}
		public override string ToString(){
			return "{" + name + ":" + classes.FoldToString (a => a.ToString (50), "\n\t{", "}", "\n\t") + "}";
		}
	}

}

