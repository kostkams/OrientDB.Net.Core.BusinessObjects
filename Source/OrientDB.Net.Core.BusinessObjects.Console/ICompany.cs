using System.Collections.Generic;
using OrientDB.Net.Core.BusinessObjects;

namespace Proj1.Test
{
    public interface ICompany : IBusinessObject
    {
        string Name { get; set; }
        IList<IMember> Members { get; }
    }
}
