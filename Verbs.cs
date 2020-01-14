using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosBagConverter
{
    internal class Verbs
    {
        [Verb("info", HelpText = "Return Informtion on the RosBag")]
        internal class InfoOption
        {
            [Option('f', "file", Required = true, HelpText = "Path to first Rosbag")]
            public IEnumerable<string> Input { get; set; }
        }

        [Verb("convert", HelpText = "Return Informtion on the RosBag")]
        internal class ConvertOptions
        {
            [Option('f', "file", Required = true, HelpText = "Path to first Rosbag")]
            public IEnumerable<string> Input { get; set; }

            [Option('o', "output", Required = true, HelpText = "Path to where to store PsiStore")]
            public string Output { get; set; }

            [Option('n', "name", Required = true, HelpText = "Name of the PsiStore")]
            public string Name { get; set; }

            [Option('h', HelpText = "Use header time")]
            public bool useHeaderTime { get; set; }

            [Option('t', "topics", HelpText = "List of topics to be converted to PsiStore")]
            public IEnumerable<string> Topics { get; set; }
        }
    }
}
