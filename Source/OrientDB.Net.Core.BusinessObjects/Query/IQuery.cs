using System.Collections.Generic;

namespace OrientDB.Net.Core.BusinessObjects
{
    public interface IQuery
    {
        string QueryString { get; }

        void Set(string key, object value);

        IDictionary<string, object> Parameters { get; }
    }
}