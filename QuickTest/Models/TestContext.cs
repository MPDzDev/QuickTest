using System;
using System.Collections.Generic;

namespace QuickTest.Models
{
    internal class TestContext
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string[] Usings { get; set; }
        public object[] Dependencies { get; set; }
        public Method[] Methods { get; set; } = new Method[0];
        public Property[] Properties { get; set; } = new Property[0];
        public ClassType ClassType { get; set; } = ClassType.Unknown;
        public string[] ClassAttributes { get; set; } = new string[0];
    }
}