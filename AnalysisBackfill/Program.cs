using OSIsoft.AF.Analysis;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Time;
using OSIsoft.AF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSIsoft.AF.PI;
using OSIsoft.AF.Search;
using System.Threading;

namespace AnalysisBackfill
{
    class AnalysisBackfill
    {
    /*
        //need to figure out how to do this
        private class AppArguments
        {

        }
    */

        static void Main(string[] args)
        {
            //define variables
            PISystems aSystems = new PISystems();
            PISystem aSystem = null;
            AFDatabase aDatabase = null;
            AFElement aElement = null;
            AFAnalysisService aAnalysisService = null;
            PIServer aPIServer = null;

            List<AFAnalysis> foundAnalyses = null;

            String user_path = null;
            String user_serv = null;
            String user_db = null;
            String user_analysisfilter = null;
            String user_mode = null;

            AFTime backfillStartTime;
            AFTime backfillEndTime;
            AFTimeRange backfillPeriod = new AFTimeRange();

            String reason = null;
            Object response = null;

            String help_message = "This utility backfills an analysis.  Generic syntax: "
                            + "\n\tUpdateFileAttribute.exe \\\\AFServer\\AFDatabase\\AFElementPath AnalysisNameFilter StartTime EndTime Mode"
                            + "\n This utility supports two modes: backfill and recalc.  Examples:"
                            + "\n\tUpdateFileAttribute.exe \\\\AF1\\TestDB\\Plant1\\Pump1 FlowRate_DailyAvg '*-10d' '*' recalculate"
                            + "\n\tUpdateFileAttribute.exe \\\\AF1\\TestDB\\Plant1\\Pump1 FlowRate_DailyAvg '*-10d' '*' backfill";
            
            //bad input handling & help
            if (args.Length < 5 || args.Contains("?"))
            {
                Console.WriteLine(help_message);
                Environment.Exit(0);
            }

            try
            {
                //parse inputs and connect
                user_path = args[0];
                var inputs = user_path.Split('\\');
                user_serv = inputs[2];
                user_db = inputs[3];

                //connect
                AFSystemHelper.Connect(user_serv, user_db);
                aSystem = aSystems[user_serv];
                aDatabase = aSystem.Databases[user_db];

                /*
                //check versions
                var PISystemVersion = aSystem.ServerVersion.Split('.');
                var AFSDKVersion = aSystems.Version.Split('.');
                if (!PISystemVersion.Contains("2.8.5"))
                {
                    Console.WriteLine("Programmatic backfilling/recalculation not supported in PI AF Server {0}." +
                        "Please upgrade PI AF Server '{1}' to version 2.8.5 or higher.", PISystemVersion, user_serv);
                    Environment.Exit(0);
                }
                if (!(AFSDKVersion.Contains("2.8.5") || AFSDKVersion.Contains("2.8.6")))
                {
                    Console.WriteLine("Programmatic backfilling/recalculation not supported in AF SDK Version {0}." +
                    "Please upgrade PI System Explorer on this machine to version 2.8.5 or higher.", AFSDKVersion, user_serv);
                    Environment.Exit(0);
                }
                if (!aAnalysisService.CanQueueCalculation(out reason))
                { 
                    Console.WriteLine(reason);
                    Console.WriteLine("Programmatic backfilling/recalculation not supported in AF SDK Version {0}." +
                        "Please upgrade PI System Explorer on this machine to version 2.8.5 or higher.", AFSDKVersion, user_serv);
                    Environment.Exit(0);
                }
                if (!aPIServer.ServerVersion.Contains(3.4.405))
                { 
                    onsole.WriteLine("Programmatic backfilling/recalculation not supported in AF SDK Version {0}." +
                        "Please upgrade PI System Explorer on this machine to version 2.8.5 or higher.", AFSDKVersion, user_serv);
                    Environment.Exit(0);
                }
                */
                //AFElement
                var preLength = user_serv.Length + user_db.Length;
                var path1 = user_path.Substring(preLength + 3, user_path.Length - preLength - 3);
                aElement = (AFElement)AFObject.FindObject(path1, aDatabase);

                //other inputs
                user_analysisfilter = args[1];
                String start = args[2].Substring(1, args[2].Length - 2);
                String end = args[3].Substring(1, args[3].Length - 2);
                backfillStartTime = new AFTime(start);
                backfillEndTime = new AFTime(end);
                backfillPeriod = new AFTimeRange(backfillStartTime, backfillEndTime);
                user_mode = args[4];
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error with inputs. " + ex.Message);
                Environment.Exit(0);
            }
            try
            {
                //find analyses
                String analysisfilter = "Target:=" + aElement.GetPath(aDatabase) + " Name:=" + user_analysisfilter;
                AFAnalysisSearch analysisSearch = new AFAnalysisSearch(aDatabase, "analysisSearch", AFAnalysisSearch.ParseQuery(analysisfilter));
                foundAnalyses = analysisSearch.FindAnalyses(0, true).ToList();

                //print details to user
                Console.WriteLine("Request information:"
                    + "\n\tElement: " + aElement.GetPath().ToString()
                    + "\n\tTime range: " + backfillPeriod.ToString()
                    + "\n\tMode: " + user_mode
                    + "\n\tAnalyses (" + foundAnalyses.Count() + "):"); 
                foreach (var analysis_n in foundAnalyses)
                {
                    Console.WriteLine("\t\t{0}", analysis_n.Name);
                }

                Console.WriteLine("\n\nProgram will continue after 10 seconds, or after pressing any key.  Press Ctrl+C to kill the program.");
                DateTime beginWait = DateTime.Now;
                while (!Console.KeyAvailable && DateTime.Now.Subtract(beginWait).TotalSeconds < 10)
                    Thread.Sleep(250);

                foreach (var analysis_n in foundAnalyses)
                {
                    response = aAnalysisService.QueueCalculation(new List<AFAnalysis> { analysis_n }, backfillPeriod, AFAnalysisService.CalculationMode.FillDataGaps);

                    /*
                        * in AF 2.9, QueueCalculation will allow for true status checking. In AF 2.8.5, it is not possible to check.  
                        * Documentation (https://techsupport.osisoft.com/Documentation/PI-AF-SDK/html/M_OSIsoft_AF_Analysis_AFAnalysisService_ToString.htm) states:
                        *This method queues the list of analyses on the analysis service to be calculated. 
                        * The operation is asynchronous and returning of the method does not indicate that queued analyses were calculated. 
                        * The status can be queried in the upcoming releases using the returned handle.
                    */
                    
                    //Might be able to add a few check mechanisms using AFAnalysis.GetResolvedOutputs and the number of values in AFTimeRange
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error returned: " + ex.Message);
                Environment.Exit(0);
            }
        }
    }
}
