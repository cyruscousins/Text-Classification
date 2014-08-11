using System;

using System.Collections.Generic;

using System.Linq;
//TODO: Most of this should be done with FeatureSynthesizers.
namespace TextCharacteristicLearner
{
	public class TextFeatureSynthesizer : IFeatureSynthesizer<string>
	{
		public TextFeatureSynthesizer (string criterion)
		{
			ClassificationCriterion = criterion;
		}


		public string ClassificationCriterion{get; private set;}

		public bool NeedsTraining{get{return false;} }

		public void Train(DiscreteSeriesDatabase<string> data){
			throw new Exception("Cannot train a TextFeatureSynthesizer.");
		}

		//string[] featureSchema = "Grammar Error Rate;Orthographical Error Rate;Formality;Slang;Textspeak".Split (";");
		string[] featureSchema = "Word Count;Mean Sentence Length;Orthographical Error Rate;Formality;Textspeak".Split (';');
		public string[] GetFeatureSchema(){
			return featureSchema;
		}


		public static HashSet<string> englishSpellingErrors = new HashSet<string>(
			("accomodate, accomodation, acheive, accross, agressive, agression, apparantly, appearence, arguement, assasination, basicly, begining, beleive, belive, bizzare, buisness, calender, Carribean, cemetary, chauffer, collegue, comming, commitee, completly, concious, curiousity, definately, dilemna, dissapear, dissapoint, ecstacy, embarass, enviroment, existance, Farenheit, familar, finaly, florescent, foriegn, forseeable, fourty, foward, freind, futher, irregardless, jist, glamourous, goverment, gaurd, happend, harrass, harrassment, honourary, humourous, idiosyncracy, immediatly, incidently, independant, interupt, irresistable, knowlege, liase, liason, lollypop, millenium, millenia, Neandertal, neccessary, noticable, ocassion, occassion, occured, occuring, occurance, occurence, pavillion, persistant, pharoah, peice, politican, Portugese, posession, prefered, prefering, propoganda, publically, realy, recieve, refered, refering, religous, rember, remeber, resistence, sence, seperate, seige, succesful, supercede, suprise, tatoo, tendancy, therefor, threshhold, tommorow, tommorrow, tounge, truely, unforseen, unfortunatly, untill, wierd, whereever, wich, absense, absance, acceptible, accidentaly, accomodate, acommodate, acheive, acknowlege, aknowledge, acquaintence, aquaintance, aquire, adquire, aquit, acrage, acerage, adress, adultary, adviseable, advizable, effect, agression, aggresion, alchohol, alege, allage, allegaince, " +
			"allegience, alegiance, allmost, alot, amatuer, amature, ammend, anually, annualy, apparant, aparent, artic, arguement, athiest, avrage, averege, awfull, aweful, becuase, becomeing, begining, beleive, bellweather, buoyant, burgler, bisness, bussiness, bizness, buisness, calender, camoflage, camoflague, Carribean, catagory, cauhgt, caugt, cemetary, cematery, changable, cheif, collaegue, collegue, collectable, colum, comming, commited, comitted, conceed, congradulate, consciencious, concious, consious, concensus, contraversy, cooly, dacquiri, daquiri, decieve, definate, definit, definitly, definately, defiantly, desparate, diffrence, dilema, disapoint, disasterous, drunkeness, dumbell, embarass, equiptment, excede, exilerate, existance, experiance, extreem, facinating, firey, flourescent, foriegn, freind, fullfil, guage, gratefull, greatful, garantee, garentee, garanty, guidence, harrass, heighth, heigth, heirarchy, hors d' hors derves, ordeurves, humerous, hygene, hygine, hiygeine, higeine, hygeine, hypocrisy, hipocrit, ignorence, immitate, imediately, independant, indispensible, innoculate, inteligence, intelligance, jewelery, kernal, liesure, liason, libary, liberry, lisence, lightening, loose, maintainance, maintnance, medeval, medevil, mideval, momento, millenium, milennium, miniture, miniscule, mischevous, mischevious, mispell, misspel, neccessary, necessery, neice, nieghbor, noticable, occassion, " +
			"occasionaly, occassionally, occurrance, occurence, occured, ommision, omision, orignal, outragous, parliment, passtime, pasttime, percieve, perseverence, personell, personel, plagerize, playright, playwrite, posession, possesion, potatos, preceed, presance, principal, privelege, priviledge, professer, promiss, pronounciation, prufe, publically, quarentine, que, questionaire, questionnair, readible, realy, recieve, reciept, recomend, reccommend, refered, referance, refrence, relevent, revelant, religous, religius, repitition, restarant, restaraunt, rime, ryme, rythm, rythem, secratary, secretery, sieze, seperate, sargent, similer, skilfull, speach, speeche, succesful, successfull, sucessful, supercede, suprise, surprize, tomatos, tommorrow, twelth, tyrany, underate, untill, upholstry, usible, vaccuum, vacume, vegatarian, vehical, visious, wether, wierd, wellfare, welfair, wether, wilfull, withold, writting, writeing").Split (new[]{", "}, StringSplitOptions.None)
		);

		/*
		public static HashSet<string> = new HashSet<string>(
			""
		);
		*/

		public static HashSet<string> textSpeak = new HashSet<string>(
			"2moro, 2nite, BRB, BTW, B4N, BCNU, BFF, CYA, DBEYR, DILLIGAS, FUD, FWIW, GR8, ILY, IMHO, IRL, ISO, J/K, L8R, LMAO, LOL, LYLAS, MHOTY, NIMBY, NP, NUB, OIC, OMG, OT, POV, RBTL, ROTFLMAO, RT, THX, TX, THKS, SH, SITD, SOL, STBY, SWAK, TFH, RTM, RTFM, TLC, TMI, TTYL, TYVM, VBG, WEG, WTF, WYWH, XOXO".ToLower().Split (new[]{", "}, StringSplitOptions.None)
		);

		public static HashSet<string> englishFormals = new HashSet<string>(
				"sir;madam;pardon;nevertheless;regardless;moreover;notwithstanding".Split (new[]{";"}, StringSplitOptions.None)
		);

		public static HashSet<string> stops = new HashSet<string>(){
			"STOP", ".", "!", "?", ";"
		};

		//Synthesize features for an item.
		public double[] SynthesizeFeatures(DiscreteEventSeries<string> item){
			//"Word Count;Mean Sentence Length;Orthographical Error Rate;Formality;Textspeak"

			return new[]{
				item.data.Length,
				item.data.Length / (double)item.data.Where(word => stops.Contains(word)).Count(),
				item.data.Where(word => englishSpellingErrors.Contains(word.ToLower())).Count() / (double)item.data.Length,
				item.data.Where(word => englishFormals.Contains(word.ToLower())).Count() / (double)item.data.Length,
				item.data.Where(word => textSpeak.Contains(word.ToLower())).Count() / (double)item.data.Length
			};
		}

	}
}

