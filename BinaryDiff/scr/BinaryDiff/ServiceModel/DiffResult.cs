using System.Collections.Generic;

namespace BinaryDiff.ServiceModel
{
    public class DiffResult
    {
        public DiffResultType Result { get; set; }
        public IEnumerable<DiffResultDetail> DiffDetails { get; set; }
    }
}
