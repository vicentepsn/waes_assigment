using BinaryDiff.Data.Entities;

namespace BinaryDiff.Data.Contracts
{
    public interface IComparableEncodedDataRepository
    {
        void Update(ComparableEncodedData comparableEncodedData);
        ComparableEncodedData Get(int id);
    }
}
