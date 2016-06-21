using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace WoxRestarter
{
    class Options
    {
        [Option('a', "app", Required = true)]
        public string App { get; set; }

        [Option('d', "working-directory", Required = true)]
        public string WorkingDirectory { get; set; }

        [Option('p', "process-id", Required = true)]
        public int PID { get; set; }

        [Option('q', "query")]
        public string Query { get; set; }
    }
}
