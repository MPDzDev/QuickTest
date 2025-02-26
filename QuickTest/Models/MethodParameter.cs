using System;
using System.Collections.Generic;

namespace QuickTest.Models
{
    internal class MethodParameter
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public bool IsOut { get; set; }
        public bool IsRef { get; set; }
        public bool IsParams { get; set; }
    }
}