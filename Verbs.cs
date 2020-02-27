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
            [Option('f', "file", Required = true, HelpText = "Path to RosBag or directory")]
            public IEnumerable<string> Input { get; set; }
        }

        [Verb("convert", HelpText = "Return Informtion on the RosBag")]
        internal class ConvertOptions
        {
            [Option('f', "file", Required = true, HelpText = "Path to RosBag or directory")]
            public IEnumerable<string> Input { get; set; }

            [Option('o', "output", Required = true, HelpText = "Path to where to store PsiStore")]
            public string Output { get; set; }

            [Option('n', "name", Required = true, HelpText = "Name of the PsiStores")]
            public string Name { get; set; }

            [Option('h', HelpText = "Whether to use header time (default = false)")]
            public bool useHeaderTime { get; set; } = false;

            [Option('s', HelpText = "Whether to use custom serializer (default = true)")]
            public bool useCustomSerializer { get; set; } = true;

            [Option('r', "restamp", HelpText = "Re-stamp Starting Time to be relative to the beginning of this application")]
            public bool RestampTime { get; set; }

            [Option('t', "topics", HelpText = "List of topics to be converted to PsiStore")]
            public IEnumerable<string> Topics { get; set; }

            [Option('x', "topics", HelpText = "List of topics to be excluded when converting to PsiStore format")]
            public IEnumerable<string> ExcludeTopic { get; set; }

        }
    }
}
