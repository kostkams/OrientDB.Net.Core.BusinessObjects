using System.Collections.Generic;

namespace OrientDB.Net.Core.BusinessObjects
{
    public class ReferenceList<TBO> : List<TBO> where TBO : IBusinessObject
    {
    }
}