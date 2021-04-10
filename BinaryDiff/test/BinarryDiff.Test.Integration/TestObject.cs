using System;

namespace BinarryDiff.Test.Integration
{
    [Serializable]
    public class TestObject
    {
        public int Id { get; set; }
        public double DoubleValue1 { get; set; }
        public double DoubleValue2 { get; set; }
        public string SomeText { get; set; }
    }
}
