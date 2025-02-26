using System;
using System.Collections.Generic;

namespace QuickTest.Models
{
    internal class Method
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string Parameters { get; set; }
        public bool IsAsync { get; set; }
        public MethodParameter[] ParameterList { get; set; } = new MethodParameter[0];
        public string[] Attributes { get; set; } = new string[0];
        public string[] SuggestedTestMethods { get; set; } = new string[0];
        public MethodBodyPatterns BodyPatterns { get; set; } = new MethodBodyPatterns();
    }
}