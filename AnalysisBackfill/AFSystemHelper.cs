using OSIsoft.AF;
using OSIsoft.AF.PI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

class AFSystemHelper
{
    // Connect to PI AF Server
    public static void Connect(string PISystemName, string AFDatabaseName)
    {
        //connections
        PISystems allAFServers = new PISystems();
        PISystem aAFServer = allAFServers[PISystemName];

        try
        {    
            aAFServer.Connect();
        }
        catch (Exception e)
        {
            Console.WriteLine("{0}\nPISystem '{1}' was not found. List of PI Systems in KST:", e.Message, PISystemName);
            foreach (var it in allAFServers)
                Console.WriteLine("\t" + it.Name);
            Environment.Exit(0);
            return;
        }

        try
        {
            AFDatabase aAFDatabase = aAFServer.Databases[AFDatabaseName];
            var name = aAFDatabase.Name;
        }
        catch(Exception e)
        {
            Console.WriteLine("{0}\nAF Database '{1}' was not found. List of AF Databases in {2}:", e.Message, AFDatabaseName, PISystemName);
            foreach (var it in aAFServer.Databases)
                Console.WriteLine("\t" + it.Name);
            Environment.Exit(0);
            return;
        }
    }

    // Connect to PI Data Archive
    public static void Connect(string PIServerName)
    {
        //connections
        PIServers allPIServers = new PIServers();
        PIServer aPIServer = allPIServers[PIServerName];

        try
        {
            aPIServer.Connect();
        }
        catch (Exception e)
        {
            Console.WriteLine("{0}\nPIServer '{1}' was not found. List of PI Servers in KST:", e.Message, PIServerName);
            foreach (var it in allPIServers)
                Console.WriteLine("\t" + it.Name);
            Environment.Exit(0);
            return;
        }
    }
}
