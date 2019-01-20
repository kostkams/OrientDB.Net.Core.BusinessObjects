using OrientDB.Net.Core.BusinessObjects;

namespace Proj1.Test
{
    public interface IAddress : IBusinessObject
    {
        string Name { get; set; }
    }
}
