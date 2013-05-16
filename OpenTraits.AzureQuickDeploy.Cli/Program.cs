using OpenTraits.Azure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenTraits.AzureQuickDeploy.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            //// get folder where the assembly is
            //var baseFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);

            //// exit one more folder
            //baseFolder = Path.GetDirectoryName(baseFolder);

            //// remove the protocol
            //if (baseFolder.IndexOf("file:\\",  StringComparison.OrdinalIgnoreCase) == 0)
            //    baseFolder = baseFolder.Substring("file:\\".Length);

            var baseFolder = string.Format("{0}sitesroot\\0", Path.GetPathRoot(System.Reflection.Assembly.GetExecutingAssembly().Location));

            QuickDeployService.CheckSync(5, "deploy", baseFolder);
        }
    }
}
