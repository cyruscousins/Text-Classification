using System;

using Whetstone;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Parallel;

using System.Text;

using System.IO;

using System.Diagnostics;

namespace TextCharacteristicLearner
{
	class MainClass
	{
		public static void Main (string[] args)
		{

			Stopwatch sw = new Stopwatch();
			sw.Start ();

			//basicClassifierTest();
			//testClassifiers();

			//runNewsClassification();
			//runNewsClassifierDerivation();
			//testNews ();
			TestLatex ();
			//TestBrokenNormalizer();

			//TestNewDesign();
			//deriveOptimalClassifier();

			//testDatabase ();

			sw.Stop ();

			Console.WriteLine ("Elapsed Time: " + sw.Elapsed);

			//testClassifiers();
		}

		public static void TestNewDesign(){
			DiscreteSeriesDatabase<string> allData = LoadRegionsDatabase();

			Tuple<DiscreteSeriesDatabase<string>, DiscreteSeriesDatabase<string>> split = allData.SplitDatabase (.8);

			DiscreteSeriesDatabase<string> trainingData = split.Item1;
			DiscreteSeriesDatabase<string> testData = split.Item2;


			IFeatureSynthesizer<string> synth = new RegressorFeatureSynthesizerKmerFrequenciesVarK<string>("region", 8, 2, 100, 3);
			//IFeatureSynthesizer<string> synth = new RegressorFeatureSynthesizerKmerFrequencies<string>("region", 4, 10, 100, 3);
			//IFeatureSynthesizer<string> synth = new RegressorFeatureSynthesizerFrequencies<string>("region", 4, 10, 100);
			 
			synth.Train (trainingData);

			Console.WriteLine (synth.ToString ());
			synth.ScoreModel (testData, 2, "filename");
			Console.WriteLine(ClassifyDataSet (synth, testData, "filename")); //TODO may be good to use something unspecifiable in the file syntax such as "filename;"
		

			//Console.WriteLine (allData.DatabaseLatexString("Regional Spanish Database"));
		}

		/*
		public void TestNewClassifiers(){

		}
		*/

		public static void TestLatex ()
		{

			bool test = false;
			bool shorten = true;
			bool costarica = true;
			bool cuba = true;

			if(test){
				costarica = cuba = false;
			}

			DiscreteSeriesDatabase<string> allData = LoadRegionsDatabase (test, shorten, costarica, cuba);


			/*
			IFeatureSynthesizer<string> testSynth = new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, true);
			testSynth.Train (allData);

			Console.WriteLine (testSynth.GetFeatureSchema().FoldToString ());
			Console.WriteLine (testSynth.SynthesizeFeaturesSumToOne(new DiscreteEventSeries<string>(allData.data.First ().labels, allData.data.First ().Take (25).ToArray ())).FoldToString (d => d.ToString ("F3")));
			Console.ReadLine ();
			*/

			/*
			if(test){
				allData = allData.SplitDatabase (.25).Item1;
			}
			*/


			//TODO: Add length distribution for documents and each type.

			//Create a feature synthesizer

			//IFeatureSynthesizer<string> synth = new RegressorFeatureSynthesizerKmerFrequenciesVarK<string>("region", 8, 2, 100, 3); //Slowld way
			//IFeatureSynthesizer<string> synth = new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, true);

			//IEventSeriesProbabalisticClassifier<string> textClassifier // = TextClassifierFactory.TextClassifier ("region", new[]{"region", "type"});

			//string documentTitle, string author, int width, int height, string outFile, IEnumerable<Tuple<string, IEventSeriesProbabalisticClassifier<Ty>>> classifiers, string datasetTitle, DiscreteSeriesDatabase<Ty> dataset, string criterionByWhichToClassify
			//IEnumerable<Tuple<string, IEventSeriesProbabalisticClassifier<string>>> classifiers = TextClassifierFactory.RegionsTestClassifiers().ToArray ();
			IEnumerable<Tuple<string, IEventSeriesProbabalisticClassifier<string>>> classifiers = TextClassifierFactory.RegionsPerceptronTestClassifiers().ToArray ();

			IFeatureSynthesizer<string> synthesizer = new CompoundFeatureSynthesizer<string>(
				"region",
				new IFeatureSynthesizer<string>[]{
					new VarKmerFrequencyFeatureSynthesizerToRawFrequencies<string>("region", 2, 2, 16, .1, false),
					new LatinLanguageFeatureSynthesizer("region"),
					new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, false),
					new VarKmerFrequencyFeatureSynthesizer<string>("type", 3, 3, 50, 2.0, false)
				}
			);


			if(test){
				classifiers = classifiers.Take (2);
			}



			WriteupGenerator.ProduceClassifierComparisonWriteup<string>("Spanish Language Dialect Analysis", "Cyrus Cousins", 11, 16, "../../out/spanish/spanish.tex", classifiers.ToArray (), "Spanish Language", allData, "region", test ? 1 : 4, analysisCriteria: new[]{"region", "type"}, synthesizer: synthesizer);

			/*
			if (classifier is SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>) {
				IFeatureSynthesizer<string> synthesizer = ((SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>)classifier).synthesizer;
				
				//doc.Append ("\\section{Feature Synthesizer Analysis}\n\n");
				//doc.Append (synthesizer.FeatureSynthesizerLatexString(allData));
			}
			*/


		}

		private static HashSet<String> invalidAuthors = new HashSet<string>("Porst_Report;Posr_Report;Post_Reoprt;Post_Repoert;Post_Report;Post_Repo-rt;POST_REPORT;POST_REPORT_\'environmental_Laws_Adequate,_Implementation_Weak\';POST_REPORT_P\';POST_REPORT,_POST_REPORT;Post_Repot;Post_Reprot;Post_Rerport;Post_Roport;Post_Team;PR;Pr);(pr);PR,_PR;RSS;;Rss.;(rss;(rss)".Replace ("_", @"\_").Split (';'));
		private static Dictionary<string, string> manualRenames = 
			//"Shandip_K C:Shandip_K.c.;Shandip_Kc:Shandip_K.c.;William_Pesek_Jr:Williar_Pesek_Jr.;William_Pesekjr:Williar_Pesek_Jr.;Prbhakar_Ghimire:Prabhakar_Ghimire;Himesh_Barjrachrya:Himesh_Bajracharya;Tapas_Barshimha_Thapa:Tapas_Barsimha_Thapa".Replace ("_", @"\_").Split (";:".ToCharArray()).AdjacentPairs().ToDictionary(tup => tup.Item1, tup =>tup.Item2);
			new Dictionary<string, string>();

		public static DiscreteSeriesDatabase<string> getNewsDataset(string size){
			DiscreteSeriesDatabase<string> data = new DiscreteSeriesDatabase<string> ();

			using (StreamReader keyfile = File.OpenText("../../res/shirish" + size + "key")){
				//keyfile.BaseStream.Seek(-70 * 8000, System.IO.SeekOrigin.End);
				//keyfile.ReadLine ();
//				for(int i = 0; i < 8000; i++) keyfile.ReadLine ();
				data.LoadTextDatabase ("../../res/shirish" + size + "/", keyfile, 1);
			}

			//Do some processing on the database
			foreach(DiscreteEventSeries<string> item in data.data){
				string author = AsciiOnly(item.labels["author"], false).RegexReplace (@"_+", @"_").RegexReplace (@"(?:[<])|(?:^[_,])|(?:$)|(?:\')|\\", "").RegexReplace (@"([#_$&])", @"\$1");
				author = manualRenames.GetWithDefault (author, author);

				if(author.StartsWith (@"\_")){ //TODO: Why is this not caught by the regex?
					author = author.Substring (2);
				}
				if(invalidAuthors.Contains (author)){
					//Console.WriteLine ("REMOVED " + author);
					item.labels.Remove("author");
				}
				else{
					item.labels["author"] = author; //Put the formatting done above back into db
				}

				item.labels["filename"] = item.labels["filename"].RegexReplace ("([#_$&])", "\\$1");
			}

			return data;
		}

		public static void runNewsClassification(){
			
			DiscreteSeriesDatabase<string> data = getNewsDataset ("med");


			//Create the classifier
			IEventSeriesProbabalisticClassifier<string> classifier = new SeriesFeatureSynthesizerToVectorProbabalisticClassifierEventSeriesProbabalisticClassifier<string>(
				new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 60, 0.1, false),
				new NullProbabalisticClassifier()
			);

			//string documentTitle, string author, int width, int height, string outFile, IEventSeriesProbabalisticClassifier<Ty> classifier, DiscreteEventSeries<Ty> dataset, string datasetTitle, string criterionByWhichToClassify
			WriteupGenerator.ProduceClassificationReport<string>("Analysis and Classification of " + data.data.Count + " Ekantipur Articles", "Cyrus Cousins with Shirish Pokharel", 20, 20, "../../out/news/news.tex", classifier, "characteristic kmer classifier", data, "News", "author");

		}
		public static void runNewsClassifierDerivation ()
		{

			//Load the database:
			DiscreteSeriesDatabase<string> data = getNewsDataset ("med");
			//data = data.SplitDatabase (.1).Item1;


			IEnumerable<Tuple<string, IEventSeriesProbabalisticClassifier<string>>> classifiers = TextClassifierFactory.NewsTestAdvancedClassifiers();
			WriteupGenerator.ProduceClassifierComparisonWriteup<string>("Classifier Comparison Analysis on Ekantipur News Articles", "Cyrus Cousins with Shirish Pokharel", 20, 20, "../../out/news/newsclassifiers.tex", classifiers.ToArray (), "News", data, "author", 12, new[]{"author", "location", "date"});
		}


		public static string AsciiOnly(string input, bool includeExtendedAscii)
		{
		    int upperLimit = includeExtendedAscii ? 255 : 127;
		    char[] asciiChars = input.Where(c => (int)c <= upperLimit).ToArray();
		    return new string(asciiChars);
		}



		
		public static IFeatureSynthesizer<string> deriveOptimalClassifier(){

			//Load databases
			DiscreteSeriesDatabase<string> allData = LoadRegionsDatabase();

			Tuple<DiscreteSeriesDatabase<string>, DiscreteSeriesDatabase<string>> split = allData.SplitDatabase (.8);

			DiscreteSeriesDatabase<string> trainingData = split.Item1;
			DiscreteSeriesDatabase<string> testData = split.Item2;

			string cat = "region";

			double optimalScore = 0;
			IFeatureSynthesizer<string> optimalClassifier = null;
			string optimalInfoStr = null;

			//Preliminary scan
			
			uint[] ks = new uint[]{2, 3, 4};
			//uint[] minCutoffs = new uint[]{5, 10, 20};
			uint[] minCutoffs = new uint[]{10};
			uint[] kmerCounts = new uint[]{10, 25, 50, 100};
			uint[] smoothingAmounts = new uint[]{1, 5, 10};

			string[] colNames = "k minCutoff kmerCount smoothingAmount score".Split (' ');

			Console.WriteLine (colNames.FoldToString ("", "", ","));

			foreach(uint k in ks){
				foreach(uint minCutoff in minCutoffs){
					foreach(uint kmerCount in kmerCounts){
						foreach(uint smoothingAmount in smoothingAmounts){

							IFeatureSynthesizer<string> classifier = new RegressorFeatureSynthesizerKmerFrequenciesVarK<string>(cat, minCutoff, smoothingAmount, kmerCount, k);
							classifier.Train (trainingData);

							double score = classifier.ScoreModel (testData);

							string infoStr = new double[]{k, minCutoff, kmerCount, smoothingAmount, score}.FoldToString ("", "", ",");

							Console.WriteLine (infoStr);
							if(score > optimalScore){
								optimalScore = score;
								optimalClassifier = classifier;
								optimalInfoStr = infoStr;
							}
						}
					}
				}
			}

			Console.WriteLine ("Optimal Classifier:");
			Console.WriteLine (optimalInfoStr);
			Console.WriteLine (optimalClassifier);

			return optimalClassifier;

		}

		public static DiscreteSeriesDatabase<string> LoadRegionsDatabase (bool test = false, bool shorten = false, bool costarica = true, bool cuba = true)
		{
			//Load training data and create classifier.

			string directory = "../../res/regiones/";

			string[] regions = "españa argentina méxico colombia".Split (' ');

			string file = "";

			if(costarica){
				regions = "costarica".Cons (regions).ToArray ();
			}
			if(cuba){
				regions = "cuba".Cons (regions).ToArray ();
			}

			//string[] prefixes = new[]{"", "literatura", "historia", "lengua"};
			//file += prefixes.Select (prefix => regions.FoldToString ((sum, val) => sum + "region" + ":" + val + ";" + "type" + ":" + "news" + " " + prefix + val, "", "", "\n")).FoldToString ("", "", "\n");

			file += regions.Aggregate ("", (sum, val) => sum + "region" + ":" + val + ";" + "type" + ":" + "news" + " " + val + "\n");
			file += regions.Aggregate ("", (sum, val) => sum + "region" + ":" + val + ";" + "type" + ":" + "wiki" + " " + "literatura" + val + "\n");
			file += regions.Aggregate ("", (sum, val) => sum + "region" + ":" + val + ";" + "type" + ":" + "wiki" + " " + "historia" + val + "\n");
			file += regions.Aggregate ("", (sum, val) => sum + "region" + ":" + val + ";" + "type" + ":" + "wiki" + " " + "lengua" + val + "\n");
			file += regions.Aggregate ("", (sum, val) => sum + "region" + ":" + val + ";" + "type" + ":" + "receta" + " " + "recetas" + val + "\n");

			if (!test) {
				{
					string[] literatureRegions = "costarica costarica españa españa españa argentina argentina argentina argentina argentina argentina españa españa españa españa méxico méxico méxico méxico méxico méxico méxico colombia colombia colombia colombia colombia".Split (' ');
					string[] literatureNames = "leyendascr elisadelmar juanvaleraavuelaplumaespaña juanvaleraloscordobesesespaña marianela historiauniversal lamuerte buenosaires derroterosyviages fundaciondelaciudad laargentina mosenmillan historiadejudios viajosporespaña recuerdosybellezas leyendasmayas nahuatl laberinto comoaguaparachocolate mitoshorroresmexicanos leyendasmexicanas mitosurbanesmexicanos lamultituderrante viajoscolombianos leyendasurbanascolombianas mitoscolombianos mitoscolombianos2".Split (' ');

					IEnumerable<string> classesStrings = literatureRegions.Select (r => "region:" + r + ";" + "type:" + "literature");

					file += classesStrings.Zip (literatureNames, (thisClasses, thisPath) => thisClasses + " " + thisPath).Aggregate (new StringBuilder (), (sum, val) => sum.Append (val).Append ("\n"));
				}

				{
					string[] names = (
					"salud antologia9 escorpionescr teca vacunoscr lanación universidadcr recetascostarica2 recetascostarica3 crcrawl presidentecostarica gobiernocostarica " +
						"arqueologiamaya poesiamexicana catolicismosocial unam mxcrawl cocrawl cocrawl2 desplazadoscolombianos mexicocnn méxicolgbt méxicogob historiaazteca historiaazteca2 " +
						"ordenamientoterretorrial competitividad ministerio"
				).Split (' ');
					string[] tags = (
					"region:costarica region:costarica region:costarica region:costarica region:costarica;type:paper region:costarica;type:news region:costarica region:costarica;type:receta region:costarica;type:receta region:costarica;type:website region:costarica;type:wiki region:costarica;type:wiki " +
						"region:méxico region:méxico;type:paper region:méxico;type:paper region:méxico;type:paper region:méxico;type:website region:colombia;type:website region:colombia;type:website region:colombia;type:wiki region:méxico;type:news region:méxico;type:brochure region:méxico;type:website region:méxico region:méxico " +
						"region:colombia region:colombia region:colombia"
				).Split (' ');

					file += tags.Zip (names, (tag, name) => tag + " " + name).FoldToString ("", "\n", "\n");
				}
			}

			if(cuba){
				file += "region:cuba;type:wiki cubaisla\n";
				file += "region:cuba;type:receta recetascuba2\n";
				file += "region:cuba;type:receta recetascuba3\n";
				file += "region:cuba;type:literatura lahistoriame\n";
				file += "region:cuba;type:literatura elencuentro\n";
			}

			Console.WriteLine ("Regions Database:");
			Console.WriteLine(file);

			TextReader reader = new StringReader(file);

			DiscreteSeriesDatabase<string> d = new DiscreteSeriesDatabase<string> ();
			d.LoadTextDatabase (directory, reader, 3);

			if(shorten){
				d = new DiscreteSeriesDatabase<string>(d.Select (item => new DiscreteEventSeries<string>(item.labels, item.data.Take (750).ToArray ())));
			}

			return d;
		}



		//CLASSIFICATION:
		public static string ClassifyDataSet<Ty>(IFeatureSynthesizer<Ty> synth, DiscreteSeriesDatabase<Ty> db, string nameField){
			return db.data.AsParallel().Select (item => ClassifyItem(synth, item, nameField)).FoldToString ();
		}

		public static string ClassifyItem<Ty>(IFeatureSynthesizer<Ty> synth, DiscreteEventSeries<Ty> item, string nameField){

			double[] scores = synth.SynthesizeFeaturesSumToOne(item);

			double max = scores.Max ();
			//TODO don't report ambiguous cases.
			return (item.labels[nameField] + ": " + synth.SynthesizeLabelFeature(item) + "" +
				"(" + max + " confidence)");
		}




		public static void TestBrokenNormalizer(){
			ZScoreNormalizerClassifierWrapper normalizer = new ZScoreNormalizerClassifierWrapper(new NullProbabalisticClassifier());

			double[][] data = new double[][]{
				new double[] {-1, 100, 100, 0},
				new double[] {0, 0, 120, 0},
				new double[] {1, -100, 80, 0}
			};

			IEnumerable<LabeledInstance> tdata = 
				data.Select ((vals, index) => new LabeledInstance(index.ToString(), vals));

			normalizer.Train (tdata);

			Console.WriteLine ("Normalizer: " + normalizer);

			double[] test = {10, 10, 100, 7};

			Console.WriteLine ("Z(" + test.FoldToString () + ") = " + normalizer.applyNormalization(test).FoldToString ());
		}
	}
}
