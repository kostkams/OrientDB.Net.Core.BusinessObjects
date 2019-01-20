using System.Collections.Generic;

namespace OrientDB.Net.Core.BusinessObjects.Query
{
    public class Query : IQuery
    {
        public Query(string queryString)
        {
            QueryString = queryString;
            Parameters = new Dictionary<string, object>();
        }

        public string QueryString { get; }

        public void Set(string key, object value)
        {
            Parameters.Add(key, value);
        }

        public IDictionary<string, object> Parameters { get; }

        public override string ToString()
        {
            return QueryString;
        }
    }
}