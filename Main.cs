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

			//testNews ();
			TestLatex ();

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

		public static void TestLatex(){

			bool test = true;

			DiscreteSeriesDatabase<string> allData = LoadRegionsDatabase(test);

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

			LatexDocument doc = new LatexDocument("Spanish Language Dialect Analysis", "Cyrus Cousins");

			doc.Append ("\\section{Spanish Language Database Overview}\n\n");
			doc.Append (allData.DatabaseLatexString("Spanish Language Dialect Analysis", null, 6));

			//TODO: Add length distribution for documents and each type.

			//Create a feature synthesizer

			//IFeatureSynthesizer<string> synth = new RegressorFeatureSynthesizerKmerFrequenciesVarK<string>("region", 8, 2, 100, 3); //Slowld way
			IFeatureSynthesizer<string> synth = new VarKmerFrequencyFeatureSynthesizer<string>("region", 3, 4, 50, 2.0, true);
			synth.Train (allData);

			doc.Append ("\\section{Feature Synthesizer and Classifier Overview}\n\n");
			doc.Append (synth.ClassifierLatexString("Region Classifier", 80));

			doc.Append (synth.ClassifierAccuracyLatexString(allData, "region", .8, test ? 1 : 4, .1));

			//TODO: Show classifiers.
			//TODO: Profile accuracy, preferably in multiple ways?
			//TODO: Number formatting.
			//TODO: Accuracy Random Model comparisons.

			doc.AppendClose ();
			doc.Write ("../../out/spanish/spanish.tex");
		}

		public static HashSet<String> invalidAuthors = new HashSet<string>("Porst_Report;Posr_Report;Post_Reoprt;Post_Repoert;Post_Report;Post_Repo-rt;POST_REPORT;POST_REPORT,_\'environmental_Laws_Adequate,_Implementation_Weak\';POST_REPORT,_P\';POST_REPORT,_POST_REPORT;Post_Repot;Post_Reprot;Post_Rerport;Post_Roport;Post_Team;PR;Pr);(pr);PR,_PR;RSS;;Rss.;(rss;(rss)".Replace ("_", @"\_").Split (';'));

		public static void testNews(){

			//Load the database:
			DiscreteSeriesDatabase<string> data = new DiscreteSeriesDatabase<string> ();

			using (StreamReader keyfile = File.OpenText("../../res/shirishmedkey")){
				data.LoadTextDatabase ("../../res/shirishmed/", keyfile, 1);
			}

			//Do some processing on the database

			foreach(DiscreteEventSeries<string> item in data.data){
				string author = AsciiOnly(item.labels["author"], false).RegexReplace (@"_+", @"_").RegexReplace (@"(?:[<])|(?:^[_,])|(?:$)|(?:\')|\\", "").RegexReplace (@"([#_$&])", @"\$1");
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


			//Testing author.
			//Console.WriteLine (data.data.Select (item => item.labels.GetWithDefault("author", "[none]")).Where (item => item.ToUpper()[0] == 'P').Distinct ().Order().FoldToString ("", "", "\n"));
			//Console.ReadLine ();
			
			LatexDocument doc = new LatexDocument("Analysis of the Shirish Pokharel News Database", "Cyrus Cousins", .8, 20, 20);

			doc.Append ("\\section{Input Data Overview}\n\n");
			doc.Append (data.DatabaseLatexString("News Database Analysis", new[]{"author"}, 20));

			Console.WriteLine ("Generated Database Overview.");

			//TODO: Add length distribution for documents and each type.

			//Create a feature synthesizer
			//IFeatureSynthesizer<string> synth = new RegressorFeatureSynthesizerKmerFrequenciesVarK<string>("author", 8, 2, 100, 3);
			IFeatureSynthesizer<string> synth = new VarKmerFrequencyFeatureSynthesizer<string>("author", 3, 2, 50, 0.1, false);
			synth.Train (data);

			doc.Append ("\\section{Feature Synthesizer and Classifier Overview}\n\n");
			doc.Append (synth.ClassifierLatexString("Author Classifier", 160));

			Console.WriteLine ("Generated Classifier Overview.");

			doc.Append ("\\section{Classification Report}\n\n");
			doc.Append (synth.ClassificationReportLatexString(data, "author"));
			
			Console.WriteLine ("Generated Classification Report.");
			
			//classificationReport.Append ("\\section{Classification Report}\n\n");
			//classificationReport.Append (synth.ClassificationReportLatexString(data, "author"));

			//accuracyReport.Append("\\section{Classifier Accuracy Report}\n\n");
			//accuracyReport.Append (synth.ClassifierAccuracyLatexString(data, "author", .8, 8, .05));


			doc.Append ("\\section{Classifier Accuracy Report}\n\n");
			doc.Append ("*\textbf{Section omitted due to abnormally high number of classes.");
			doc.Append (synth.ClassifierAccuracyLatexString(data, "author", .8, 32, .05));
			
			Console.WriteLine ("Generated Classifier Accuracy Report.");


			doc.AppendClose ();
			doc.Write ("../../out/news/news.tex", s => AsciiOnly(s, false)); //s => s.RegexReplace (@"[^\u0000-\u007F\u0080-\u0099]", string.Empty));
			
			//accuracyReport.AppendClose ();
			//accuracyReport.Write ("../../out/news/accuracy.tex", s => s.Replace ("â", "'").Replace("â", "???").Replace ("â", "'"));

			//classificationReport.AppendClose ();
			//classificationReport.Write ("../../out/news/classification.tex", s => s.Replace ("â", "'").Replace("â", "???").Replace ("â", "'"));
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

		public static DiscreteSeriesDatabase<string> LoadRegionsDatabase (bool test = false)
		{
			//Load training data and create classifier.

			string directory = "../../res/regiones/";

			string[] regions = "españa argentina méxico colombia costarica".Split (' ');

			string file = "";

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

			Console.WriteLine ("Regions Database:");
			Console.WriteLine(file);

			TextReader reader = new StringReader(file);

			DiscreteSeriesDatabase<string> d = new DiscreteSeriesDatabase<string> ();
			d.LoadTextDatabase (directory, reader, 3);

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
			return (item.labels[nameField] + ": " + synth.SynthesizeLabelFeature(item) + " (" + max + " confidence)");
		}
	}
}
