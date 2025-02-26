using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickTest.Models
{
    internal class TestContext
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string[] Usings { get; set; }
        public object[] Dependencies { get; set; }
    }
}