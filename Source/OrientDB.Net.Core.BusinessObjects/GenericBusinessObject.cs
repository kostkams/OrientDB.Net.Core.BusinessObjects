using System;
using System.Collections.Generic;
using System.Linq;
using Orient.Client;

namespace OrientDB.Net.Core.BusinessObjects
{
    public class GenericBusinessObject : BusinessObject, IGenericBusinessObject
    {
        internal ODocument Document { get; set; }

        public IDictionary<string, object> Properties => Document.ToDictionary(item => item.Key, item => item.Value);

        public void SetField(string key, object value)
        {
            Document.SetField(key, value);
        }

        public T GetField<T>(string key)
        {
            return Document.GetField<T>(key);
        }

        public override string ClassName
        {
            get => Document.OClassName;
            protected set => throw new NotImplementedException();
        }

        public override string Id
        {
            get => Document.ORID.ToString();
            internal set => throw new NotImplementedException();
        }

        public override int Version
        {
            get => Document.OVersion;
            internal set => throw new NotImplementedException();
        }
    }
}