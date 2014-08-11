using System;

namespace TextCharacteristicLearner
{
	public struct TupleStruct<Ty1, Ty2>
	{
		public Ty1 Item1;
		public Ty2 Item2;

		public TupleStruct (Ty1 item1, Ty2 item2)
		{
			this.Item1 = item1;
			this.Item2 = item2;
		}
	}
}

