using System.Collections.Generic;

namespace OrientDB.Net.Core.BusinessObjects
{
    public interface IGenericBusinessObject : IBusinessObject
    {
        IDictionary<string, object> Properties { get; }

        void SetField(string key, object value);
        T GetField<T>(string key);
    }
}