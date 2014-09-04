using System;

using System.Text;

using System.Diagnostics;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Whetstone;

using System.Reflection;

namespace TextCharacteristicLearner
{
	
	public class AlgorithmNameAttribute : Attribute{
		public string AlgorithmName;
		public AlgorithmNameAttribute(string name){
			AlgorithmName = name;
		}
	}

	public class AlgorithmParameterAttribute : Attribute{
		public string ParameterName;
		public int ParameterIndex;
		//public Func<object, object> Transform;

		public AlgorithmParameterAttribute(string name, int index){
			ParameterName = name;
			ParameterIndex = index;
			//Transform = transform;
		}
		
		public object resolveObject(FieldInfo field, object o){
			object res = field.GetValue(o);
			/*
			if(Transform != null){
				return Transform(res);
			}
			else return res;
			*/
			return res;
		}
		
		public object resolveObject(PropertyInfo property, object o){
			object res = property.GetValue(o, null);
			/*
			if(Transform != null){
				return Transform(res);
			}
			else return res;
			*/
			return res;
		}
	}
	
	public class AlgorithmTrainingAttribute : Attribute{
		public string ParameterName;
		public int ParameterIndex;
		//public Func<object, object> Transform;
		
		public AlgorithmTrainingAttribute(string name, int index){
			ParameterName = name;
			ParameterIndex = index;
			//Transform = transform;
		}

		public object resolveObject(FieldInfo field, object o){
			object res = field.GetValue(o);
			/*
			if(Transform != null){
				return Transform(res);
			}
			else return res;
			*/
			return res;
		}
		
		public object resolveObject(PropertyInfo property, object o){
			object res = property.GetValue(o, null);
			/*
			if(Transform != null){
				return Transform(res);
			}
			else return res;
			*/
			return res;
		}
	}

	public static class AlgorithmReflectionExtensions{

		public static AttrTy[] GetCustomAttributes<AttrTy>(this Type ty, bool inherit){
			return ty.GetCustomAttributes (typeof(AttrTy), inherit).Cast<AttrTy>().ToArray (); //Not a great solution, but it gets the job done.
		}
		
		public static string GetAlgorithmName(Type modelType){
			AlgorithmNameAttribute[] nameAttributes = modelType.GetCustomAttributes<AlgorithmNameAttribute> (false);
			if (nameAttributes.Length != 1) {
				return modelType.ToString ();
			}

			return nameAttributes [0].AlgorithmName;
		}

		public static string GetAlgorithmName(Object model){
			return GetAlgorithmName(model.GetType());
		}

		public static IEnumerable<Tuple<FieldInfo, AttrTy>> GetFieldCustomAttributes<AttrTy>(this Type ty, bool inherit){
			return ty.GetFields()
				//TODO: Do with typeof(AttrTy) instead of Where.
				.Select (info => new Tuple<FieldInfo, AttrTy[]>(info, info.GetCustomAttributes(true).Where(attribute => attribute is AttrTy).Cast<AttrTy>().ToArray())
			).Where (
				pair => pair.Item2.Length == 1
			).Select (
				pair => new Tuple<FieldInfo, AttrTy>(pair.Item1, pair.Item2[0])
			);
		}

		public static IEnumerable<Tuple<PropertyInfo, AttrTy>> GetPropertyCustomAttributes<AttrTy>(this Type ty, bool inherit){
			return ty.GetProperties()
				.Select (info => new Tuple<PropertyInfo, AttrTy[]>(info, info.GetCustomAttributes(true).Where(attribute => attribute is AttrTy).Cast<AttrTy>().ToArray())
			).Where (
				pair => pair.Item2.Length == 1
			).Select (
				pair => new Tuple<PropertyInfo, AttrTy>(pair.Item1, pair.Item2[0])
			);
		}

		//TODO Can we put a generic constraint on object to mandate that it has an attribute?  I don't think so.  
		public static IEnumerable<Tuple<string, object>> GetAlgorithmParameters(object o){
			Type oTy = o.GetType ();
			return 
				oTy.GetFieldCustomAttributes<AlgorithmParameterAttribute>(true).Select(item => new Tuple<int, string, object>(item.Item2.ParameterIndex, item.Item2.ParameterName, item.Item2.resolveObject(item.Item1, o))).Concat (
				oTy.GetPropertyCustomAttributes<AlgorithmParameterAttribute>(true).Select(item => new Tuple<int, string, object>(item.Item2.ParameterIndex, item.Item2.ParameterName, item.Item2.resolveObject(item.Item1, o)))
				).OrderBy (tup => tup.Item1).Select (tup => new Tuple<string, object>(tup.Item2, tup.Item3));
		}

		public static IEnumerable<Tuple<string, object>> GetAlgorithmTraining(object o){
			Type oTy = o.GetType ();
			return 
				oTy.GetFieldCustomAttributes<AlgorithmTrainingAttribute>(true).Select(item => new Tuple<int, string, object>(item.Item2.ParameterIndex, item.Item2.ParameterName, item.Item2.resolveObject(item.Item1, o))).Concat (
				oTy.GetPropertyCustomAttributes<AlgorithmTrainingAttribute>(true).Select(item => new Tuple<int, string, object>(item.Item2.ParameterIndex, item.Item2.ParameterName, item.Item2.resolveObject(item.Item1, o)))
				).OrderBy (tup => tup.Item1).Select (tup => new Tuple<string, object>(tup.Item2, tup.Item3));
		}

		
		public static string UntrainedModelString (Object model)
		{
			Type modelType = model.GetType ();
			
			string name = GetAlgorithmName(model);
			
			IEnumerable<Tuple<string, object>> algorithmParameters = GetAlgorithmParameters (model);
			IEnumerable<Tuple<string, object>> trainingParameters = GetAlgorithmTraining (model);

			StringBuilder modelString = new StringBuilder ();
			modelString.AppendLine (name + " (" + modelType.Name + "):");
			modelString.AppendLine ("\tAlgorithm Parameters:");
			modelString.AppendLine (algorithmParameters.FoldToString (param => param.Item1 + " = " + ObjectString (param.Item2), "\t\t", "", "\n\t\t"));

			return modelString.ToString();
		}

		public static string UntrainedModelLatexString (Object model)
		{
			Type modelType = model.GetType ();

			AlgorithmNameAttribute[] nameAttributes = modelType.GetCustomAttributes<AlgorithmNameAttribute> (false);
			if (nameAttributes.Length != 1) {
				return model.ToString ();
			}

			string name = nameAttributes [0].AlgorithmName;
			
			IEnumerable<Tuple<string, object>> algorithmParameters = GetAlgorithmParameters (model);
			IEnumerable<Tuple<string, object>> trainingParameters = GetAlgorithmTraining (model);

			StringBuilder modelString = new StringBuilder ();
			modelString.AppendLine (@"\textbf{\textcolor[rgb]{.3,.8,.7}{" + name + "}}" + " (" + @"\texttt{" + modelType.Name + "}" + "):" + "\n");

//			modelString.AppendLine (@"\begin{easylist}[itemize]");
			modelString.AppendLine (@"\begin{description}");

			/*
			//TODO: This concept doesn't really exist in code yet, except in a feature synthesizer, where it really shouldn't.
			if(model is IEventSeriesProbabalisticClassifier<var>){
				modelString.AppendLine ("Classifying along " + @"\texttt{" + ((IEventSeriesProbabalisticClassifier<var>)model).Class)
			}
			*/

			if (algorithmParameters.Any ()) {
				modelString.AppendLine (@"\item[Algorithm Parameters] \hfill \\");
				//TODO: Generic parameters on this one?
//				modelString.AppendLine (@"\begin{easylist}[itemize]");
				modelString.AppendLine (@"\begin{itemize}");
				modelString.AppendLine (algorithmParameters.FoldToString (tup => @"\textcolor[rgb]{.9,.75,.8}{" + tup.Item1 + "}" + " = " + ObjectLatexString (tup.Item2), @"\item ", "", "\n\\item "));
				modelString.AppendLine (@"\end{itemize}");
			}

			modelString.AppendLine (@"\end{description}");

			return modelString.ToString();

			//TODO Add display of schema on a FeatureSynthesizer?  This is only sometimes available before training.
		}

		public static string TrainedModelString (Object model)
		{
			Type modelType = model.GetType ();
			
			string name = GetAlgorithmName(model); //TODO: If null return ToString();
			
			IEnumerable<Tuple<string, object>> algorithmParameters = GetAlgorithmParameters (model);
			IEnumerable<Tuple<string, object>> trainingParameters = GetAlgorithmTraining (model);

			StringBuilder modelString = new StringBuilder ();
			modelString.AppendLine (name + " (" + modelType.Name + "):");
			modelString.AppendLine ("\tAlgorithm Parameters:");
			modelString.AppendLine (algorithmParameters.FoldToString (param => param.Item1 + " = " + ObjectString (param.Item2), "\t\t", "", "\n\t\t"));

			//TRAINING:

			if(model is IFeatureSynthesizer<string>){ //TODO: Generic!
				modelString.AppendLine ("Output Schema:");
				modelString.AppendLine(((IFeatureSynthesizer<string>)model).GetFeatureSchema().FoldToString ());
			}

			if (trainingParameters.Any ()) {
				modelString.AppendLine ("\tAlgorithm Training:");
				modelString.AppendLine (trainingParameters.FoldToString (param => param.Item1 + " = " + ObjectString (param.Item2), "\t\t", "", "\n\t\t"));
			}
			return modelString.ToString();
		}

		public static string TrainedModelLatexString (Object model)
		{
			Type modelType = model.GetType ();

			AlgorithmNameAttribute[] nameAttributes = modelType.GetCustomAttributes<AlgorithmNameAttribute> (false);
			if (nameAttributes.Length != 1) {
				return model.ToString (); //TODO escape this string.
			}

			string name = nameAttributes [0].AlgorithmName;
			
			IEnumerable<Tuple<string, object>> algorithmParameters = GetAlgorithmParameters (model);
			IEnumerable<Tuple<string, object>> trainingParameters = GetAlgorithmTraining (model);

			StringBuilder modelString = new StringBuilder ();
			modelString.AppendLine (@"\textbf{\textcolor[rgb]{.3,.8,.7}{" + name + "}}" + " (" + @"\texttt{" + modelType.Name + "}" + "):" + "\n");

//			modelString.AppendLine (@"\begin{easylist}[itemize]");
			modelString.AppendLine (@"\begin{description}");

			/*
			//TODO: This concept doesn't really exist in code yet, except in a feature synthesizer, where it really shouldn't.
			if(model is IEventSeriesProbabalisticClassifier<var>){
				modelString.AppendLine ("Classifying along " + @"\texttt{" + ((IEventSeriesProbabalisticClassifier<var>)model).Class)
			}
			*/

			if (algorithmParameters.Any ()) {
				modelString.AppendLine (@"\item[Algorithm Parameters] \hfill \\");
				//TODO: Generic parameters on this one?
//				modelString.AppendLine (@"\begin{easylist}[itemize]");
				modelString.AppendLine (@"\begin{itemize}");
				modelString.AppendLine (algorithmParameters.FoldToString (tup => @"\textcolor[rgb]{.9,.75,.8}{" + tup.Item1 + "}" + " = " + ObjectLatexString (tup.Item2), @"\item ", "", "\n\\item "));
				modelString.AppendLine (@"\end{itemize}");
			}
			//TRAINING:

			if(model is IFeatureSynthesizer<string>){ //TODO: Generic!
				modelString.AppendLine (@"\item[Output Schema] \hfill \\");
				modelString.AppendLine(((IFeatureSynthesizer<string>)model).GetFeatureSchema().FoldToString (item => @"``\texttt{" + ObjectLatexString(item) + "}''", @"$\langle$", @"$\rangle$", ", "));
			}

			if (trainingParameters.Any ()) {
				modelString.AppendLine (@"\item[Algorithm Training] \hfill \\");

				//TODO Finalize color scheme.
				//modelString.AppendLine (@"\begin{easylist}[itemize]");
				modelString.AppendLine (@"\begin{itemize}");
				modelString.AppendLine (trainingParameters.FoldToString (tup => @"\textcolor[rgb]{.75,.55,.95}{" + tup.Item1 + "}" + " = " + ObjectLatexString (tup.Item2), @"\item ", "", "\n\\item "));
				modelString.AppendLine (@"\end{itemize}");
			}
//			modelString.AppendLine (@"\end{easylist}");
			modelString.AppendLine (@"\end{description}");

			return modelString.ToString();

			//Add display of schema on a FeatureSynthesizer?
		}

		public static string ObjectString(Object o){
			if(o.GetType ().GetCustomAttributes<AlgorithmNameAttribute>(false).Length > 0){
				return TrainedModelString(o);
			}
			if(!(o is string) && o is IEnumerable){ //TODO: Use IsAssignableFrom?
				Type genericType = typeof(object);
				//TODO: Discover the type.  Cannot just examine generic arguments, have to get generic arguments of IEnumerable in particular.
				IEnumerable<Object> enumerable = ((IEnumerable)o).Cast<object>().ToArray ();

				//Output simple types on a single line.  
				if(o is IEnumerable<double> || o is IEnumerable<int>){ //TODO: Express this more cleanly.
					return genericType.Name + "[" + enumerable.Count() + "]: " + enumerable.FoldToString(item => ObjectString (item));
				}
				return genericType.Name + "[" + enumerable.Count() + "]: " + enumerable.FoldToString(item => ObjectString (item), "{\n\t", "\n}", ",\n\t");
			}
			if(o is Double){
				return ((Double)o).ToString ("G4");
			}
			/*
			if(o.GetType ().IsGenericTypeDefinition && o.GetType().GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>)){
				Type genericType = o.GetType ().GetGenericArguments[0];
				return genericType.Name + "[" + typeof(IEnumerable<>).MakeGenericType (o.GetType ().GetGenericArguments).InvokeMember
			}*/
			return o.ToString ();
		}

		public static string ObjectLatexString(Object o){
			if(o.GetType ().GetCustomAttributes<AlgorithmNameAttribute>(false).Length > 0){
				return TrainedModelLatexString(o);
			}
			if(!(o is string) && o is IEnumerable){ //TODO: Use IsAssignableFrom?
				Type genericType = typeof(object);
				//TODO: Discover the type.  Cannot just examine generic arguments, have to get generic arguments of IEnumerable in particular.
				IEnumerable<Object> enumerable = ((IEnumerable)o).Cast<object>().ToArray ();
				if(o is IEnumerable<double> || o is IEnumerable<int>){ //TODO: Express this more cleanly.
					return genericType.Name + "[" + enumerable.Count() + "]: " + enumerable.FoldToString(item => ObjectString (item), @"$\mathlarger\langle$", @"$\mathlarger\rangle$", ", ");
				}
//				return genericType.Name + "[" + enumerable.Count() + "]: " + enumerable.FoldToString(item => ObjectLatexString (item), "\\begin{easylist}[enumerate]\n\\item ", "\n\\end{easylist}", "\n\\item ");
				return genericType.Name + "[" + enumerable.Count() + "]: " + enumerable.FoldToString(item => ObjectLatexString (item), "\n\\begin{enumerate}[1)]\n\\item ", "\n\\end{enumerate}", "\n\\item ");
			}
			if(o is double){
				double d = (double)o;
				if(Double.IsNaN (d)){
					return @"\textcolor[gray]{.8}{--}";
				}
				if(d == Double.PositiveInfinity){
					return @"\infty";
				}
				if(d == Double.NegativeInfinity){
					return @"-\infty";
				}
				return ((Double)o).ToString ("G4");
			}
			//TODO: Dictionary output.
			//TODO: IEnumerable<IGrouping> output?  (Use this for the output of varkfeaturesynthesizer?)

			//TODO: Colored booleans.
			return LatexExtensions.latexEscapeString(o.ToString ());
		}

	}
}