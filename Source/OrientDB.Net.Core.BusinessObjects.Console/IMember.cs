using OrientDB.Net.Core.BusinessObjects;

namespace Proj1.Test
{
    public interface IMember : IBusinessObject
    {
        string Name { get; set; }
        IAddress Address { get; }
    }
}
