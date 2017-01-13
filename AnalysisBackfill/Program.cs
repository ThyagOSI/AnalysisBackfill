﻿using OSIsoft.AF.Analysis;
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
        static void Main(string[] args)
        {
            //define variables
            String user_path = null;
            String user_serv = null;
            String user_db = null;
            String user_analysisfilter = null;
            String user_mode = null;

            PIServer aPIServer = null;
            PISystems aSystems = new PISystems();
            PISystem aSystem = null;
            AFDatabase aDatabase = null;
            AFElement foundElements = null;
            AFAnalysisService aAnalysisService = null;
            List<AFAnalysis> foundAnalyses = null;

            AFTime backfillStartTime;
            AFTime backfillEndTime;
            AFTimeRange backfillPeriod = new AFTimeRange();

            AFAnalysisService.CalculationMode mode = AFAnalysisService.CalculationMode.FillDataGaps;
            String reason = null;
            Object response = null;

            String help_message = "This utility backfills/recalculates analyses.  Generic syntax: "
                            + "\n\tUpdateFileAttribute.exe \\\\AFServer\\AFDatabase\\pathToElement\\AFElement AnalysisNameFilter StartTime EndTime Mode"
                            + "\n This utility supports two modes: backfill and recalc.  Backfill will fill in data gaps only.  Recalc will replace all values.  Examples:"
                            + "\n\tUpdateFileAttribute.exe \\\\AF1\\TestDB\\Plant1\\Pump1 FlowRate_*Avg '*-10d' '*' recalc"
                            + "\n\tUpdateFileAttribute.exe \\\\AF1\\TestDB\\Plant1\\Pump1 *Rollup '*-10d' '*' backfill";
            
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
                aAnalysisService = aSystem.AnalysisService;


                /*
                 * need to rewrite this bit.  there has to be a better way to comparsion the version to "2.8.5" than parsing the string
                //check versions
                var PISystemVersion = aSystem.ServerVersion.Split('.');
                var AFSDKVersion = aSystems.Version.Split('.');
                                
                //if less than version 2.8.5, then programmatic backfill/recalc is not supported.
                if (Convert.ToInt32(PISystemVersion[0])<3 && Convert.ToInt32(PISystemVersion[1]) < 8 && Convert.ToInt32(PISystemVersion[2]) < 6)
                {
                    Console.WriteLine("Programmatic backfilling/recalculation is not supported in PI AF Server {0}." +
                        "Please upgrade PI AF Server '{1}' to version 2.8.5 or higher to use this utility.", aSystem.ServerVersion, user_serv);
                    Environment.Exit(0);
                }
                if (!(AFSDKVersion.Contains("2.8.5") || AFSDKVersion.Contains("2.8.6")))
                {
                    Console.WriteLine("Programmatic backfilling/recalculation is not supported in AF SDK Version {0}." +
                    "Please upgrade PI System Explorer on this machine to version 2.8.6 or higher to use this utility.", AFSDKVersion, user_serv);
                    Environment.Exit(0);
                }
                if (!aAnalysisService.CanQueueCalculation(out reason))
                { 
                    Console.WriteLine(reason);
                    Console.WriteLine("Programmatic backfilling/recalculation is not supported in AF SDK Version {0}." +
                        "Please upgrade PI System Explorer on this machine to version 2.8.5 or higher to use this utility.", AFSDKVersion, user_serv);
                    Environment.Exit(0);
                }
                if (!aPIServer.ServerVersion.Contains(3.4.405))
                {
                    Console.WriteLine("Programmatic backfilling/recalculation not supported in AF SDK Version {0}." +
                        "Please upgrade PI System Explorer on this machine to version 2.8.5 or higher.", AFSDKVersion, user_serv);
                    Environment.Exit(0);
                }
                */

                //AFElement
                var preLength = user_serv.Length + user_db.Length;
                var path1 = user_path.Substring(preLength + 3, user_path.Length - preLength - 3);
                foundElements = (AFElement)AFObject.FindObject(path1, aDatabase);

                //other inputs
                user_analysisfilter = args[1];
                String start = args[2].Substring(1, args[2].Length - 2);
                String end = args[3].Substring(1, args[3].Length - 2);
                backfillStartTime = new AFTime(start);
                backfillEndTime = new AFTime(end);
                backfillPeriod = new AFTimeRange(backfillStartTime, backfillEndTime);

                //user_mode
                user_mode = args[4];
                switch (user_mode.ToLower())
                {
                    case "recalc":
                        mode = AFAnalysisService.CalculationMode.DeleteExistingData;
                        break;
                    case "backfill":
                        mode = AFAnalysisService.CalculationMode.FillDataGaps;
                        break;
                    default:
                        Console.WriteLine("Invalid mode specified.  Supported modes: backfill, recalc");
                        Environment.Exit(0);
                        break;
                }
            
                //find analyses
                String analysisfilter = "Target:=" + foundElements.GetPath(aDatabase) + " Name:=" + user_analysisfilter;
                AFAnalysisSearch analysisSearch = new AFAnalysisSearch(aDatabase, "analysisSearch", AFAnalysisSearch.ParseQuery(analysisfilter));
                foundAnalyses = analysisSearch.FindAnalyses(0, true).ToList();

                //print details to user
                Console.WriteLine("Requested backfills/recalculations:"
                    + "\n\tElement: " + foundElements.GetPath().ToString()
                    + "\n\tAnalyses (" + foundAnalyses.Count() + "):"); 
                foreach (var analysis_n in foundAnalyses)
                {
                    Console.WriteLine("\t\t{0}\t{1}\tOutputs:{2}", analysis_n.Name, analysis_n.AnalysisRule.Name,analysis_n.AnalysisRule.GetOutputs().Count);
                }

                Console.WriteLine("\tTime range: " + backfillPeriod.ToString() + ", " + "{0}d {1}h {2}m {3}s."
                    , backfillPeriod.Span.Days, backfillPeriod.Span.Hours, backfillPeriod.Span.Minutes, backfillPeriod.Span.Seconds);
                Console.WriteLine("\tMode: " + user_mode + "=" + mode.ToString());

                if (foundAnalyses.Count == 0)
                {
                    Console.WriteLine("\nNo analyses on AF Element '{0}' match this analysis filter: '{1}'.  Exiting.", user_path, user_analysisfilter);
                    Environment.Exit(0);
                }
                
                //implement wait time
                Console.WriteLine("\nAnalyses will be queued for processing in 10 seconds.  Press Ctrl+C to cancel.");
                DateTime beginWait = DateTime.Now;
                while (!Console.KeyAvailable && DateTime.Now.Subtract(beginWait).TotalSeconds < 10) Thread.Sleep(250);

                //no status check
                Console.WriteLine("\nThere will be no status check after the backfill/recalculate is queued (until AF 2.9.0). Please verify using other means.");

                //queue analyses
                foreach (var analysis_n in foundAnalyses)
                {
                    response = aAnalysisService.QueueCalculation(new List<AFAnalysis> { analysis_n }, backfillPeriod, mode);

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
