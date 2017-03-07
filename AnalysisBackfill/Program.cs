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

/*
 *  Copyright (C) 2017  Keith Fong

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace AnalysisBackfill
{
    class AnalysisBackfill
    {
        static void Main(string[] args)
        {
            //define variables
            string user_path = null;
            string user_serv = null;
            string user_db = null;
            string user_analysisfilter = null;
            string user_mode = null;

            PIServer aPIServer = null;
            PISystems aSystems = new PISystems();
            PISystem aSystem = null;
            AFAnalysisService aAnalysisService = null;
            AFDatabase aDatabase = null;
            List<AFElement> foundElements = new List<AFElement>(); 
            List<AFAnalysis> foundAnalyses = new List<AFAnalysis>();
            IEnumerable<AFAnalysis> elemAnalyses = null;

            AFTimeRange backfillPeriod = new AFTimeRange();

            AFAnalysisService.CalculationMode mode = AFAnalysisService.CalculationMode.FillDataGaps;
            String reason = null;
            Object response = null;

            String help_message = "This utility backfills/recalculates analyses.  Generic syntax: "
                            + "\n\tAnalysisBackfill.exe \\\\AFServer\\AFDatabase\\pathToElement\\AFElement AnalysisNameFilter StartTime EndTime Mode"
                            + "\n This utility supports two modes: backfill and recalc.  Backfill will fill in data gaps only.  Recalc will replace all values.  Examples:"
                            + "\n\tAnalysisBackfill.exe \\\\AF1\\TestDB\\Plant1\\Pump1 FlowRate_*Avg '*-10d' '*' recalc"
                            + "\n\tAnalysisBackfill.exe \\\\AF1\\TestDB\\Plant1\\Pump1 *Rollup '*-10d' '*' backfill";
            
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

                /* check versions.  need to write this. 
                aSystem.ServerVersion
                aSystems.Version
                aPIServer.ServerVersion
                */

                //find AFElements
                //will eventually include element search filter as well
                if (inputs.Length == 4)
                { //all elements in database
                    foundElements = AFElement.FindElements(aDatabase, null, null, AFSearchField.Name, true, AFSortField.Name, AFSortOrder.Ascending, 1000).ToList();
                }
                else
                { //single element
                    var prelength = user_serv.Length + user_db.Length;
                    var path1 = user_path.Substring(prelength + 4, user_path.Length - prelength - 4);
                    foundElements.Add((AFElement)AFObject.FindObject(path1, aDatabase));
                }

                //other inputs
                user_analysisfilter = args[1];
                AFTime backfillStartTime = new AFTime(args[2].Trim('\''));
                AFTime backfillEndTime = new AFTime(args[3].Trim('\''));
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

                Console.WriteLine("Requested backfills/recalculations:");

                foreach (AFElement elem_n in foundElements)
                {
                    
                    //find analyses
                    String analysisfilter = "Target:=\"" + elem_n.GetPath(aDatabase) + "\" Name:=\"" + user_analysisfilter + "\"";
                    AFAnalysisSearch analysisSearch = new AFAnalysisSearch(aDatabase, "analysisSearch", AFAnalysisSearch.ParseQuery(analysisfilter));
                    elemAnalyses = analysisSearch.FindAnalyses(0, true).ToList();

                    //print details to user
                    Console.WriteLine("\tElement: " + elem_n.GetPath().ToString()
                        + "\n\tAnalyses (" + elemAnalyses.Count() + "):");
                    
                    if (elemAnalyses.Count() == 0)
                    {
                        Console.WriteLine("\t\tNo analyses on this AF Element match the analysis filter.");
                    }
                    else
                    {
                        foundAnalyses.AddRange(elemAnalyses);
                        foreach (var analysis_n in elemAnalyses)
                        {
                            Console.WriteLine("\t\t{0}, {1}, Outputs:{2}", analysis_n.Name, analysis_n.AnalysisRule.Name, analysis_n.AnalysisRule.GetOutputs().Count);
                        }
                    }

                    /* to check for dependent analyses
                    foreach (var analysis_n in foundAnalyses)
                    {

                    }
                    */

                }
                Console.WriteLine("\nTime range: " + backfillPeriod.ToString() + ", " + "{0}d {1}h {2}m {3}s."
                            , backfillPeriod.Span.Days, backfillPeriod.Span.Hours, backfillPeriod.Span.Minutes, backfillPeriod.Span.Seconds);
                Console.WriteLine("Mode: " + user_mode + "=" + mode.ToString());
                //implement wait time
                Console.WriteLine("\nA total of {0} analyses will be queued for processing in 10 seconds.  Press Ctrl+C to cancel.", foundAnalyses.Count);
                DateTime beginWait = DateTime.Now;
                while (!Console.KeyAvailable && DateTime.Now.Subtract(beginWait).TotalSeconds < 10)
                {
                    Console.Write(".");
                    Thread.Sleep(250);
                }
                //no status check
                Console.WriteLine("\n\nAll analyses have been queued.\nThere is no status check after the backfill/recalculate is queued (until AF 2.9.0). Please verify by using other means.", foundAnalyses.Count);

                //queue analyses for backfill/recalc
                foreach (var analysis_n in foundAnalyses)
                {
                    response = aAnalysisService.QueueCalculation(new List<AFAnalysis> { analysis_n }, backfillPeriod, mode);

                    /* no status check info
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


public static class AFAnalysisCustom
{
    public static void StaticToAnalysisDR(PISystem myAFServ, AFElement myElement, AFAnalysis myAnalysis)
    {
        //to use later 
    }
}