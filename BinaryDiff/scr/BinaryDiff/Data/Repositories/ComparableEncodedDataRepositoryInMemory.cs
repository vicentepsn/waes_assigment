using BinaryDiff.Data.Contracts;
using BinaryDiff.Data.Entities;
using System.Collections.Generic;
using System.Linq;

namespace BinaryDiff.Data.Repositories
{
    public class ComparableEncodedDataRepositoryInMemory : IComparableEncodedDataRepository
    {
        private readonly List<ComparableEncodedData> _inMemoryDBSet;

        public ComparableEncodedDataRepositoryInMemory()
        {
            _inMemoryDBSet = new List<ComparableEncodedData>();
        }

        public ComparableEncodedData Get(int id)
        {
            return _inMemoryDBSet.FirstOrDefault(d => d.Id == id);
        }

        public void Update(ComparableEncodedData comparableEncodedData)
        {
            var index = _inMemoryDBSet.FindIndex(d => d.Id == comparableEncodedData.Id);
            if (index >= 0)
            {
                _inMemoryDBSet[index] = comparableEncodedData;
            }
            else
            {
                _inMemoryDBSet.Add(comparableEncodedData);
            }
        }
    }
}
