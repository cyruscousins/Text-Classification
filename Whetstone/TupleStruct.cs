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
	
	public struct TupleStruct<Ty1, Ty2, Ty3>
	{
		public Ty1 Item1;
		public Ty2 Item2;
		public Ty3 Item3;

		public TupleStruct (Ty1 item1, Ty2 item2, Ty3 item3)
		{
			this.Item1 = item1;
			this.Item2 = item2;
			this.Item3 = item3;
		}
	}


	public struct TupleStruct<Ty1, Ty2, Ty3, Ty4, Ty5, Ty6, Ty7, Ty8>
	{
		public Ty1 Item1;
		public Ty2 Item2;
		public Ty3 Item3;
		public Ty4 Item4;
		public Ty5 Item5;
		public Ty6 Item6;
		public Ty7 Item7;
		public Ty8 Item8;


		public TupleStruct (Ty1 item1, Ty2 item2, Ty3 item3, Ty4 item4, Ty5 item5, Ty6 item6, Ty7 item7, Ty8 item8)
		{
			this.Item1 = item1;
			this.Item2 = item2;
			this.Item3 = item3;
			this.Item4 = item4;
			this.Item5 = item5;
			this.Item6 = item6;
			this.Item7 = item7;
			this.Item8 = item8;
		}
	}
}

