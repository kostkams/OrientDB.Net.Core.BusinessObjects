using System;
using Orient.Client;

namespace OrientDB.Net.Core.BusinessObjects
{
    public class BusinessDocument : IBusinessDocument
    {
        private readonly ConnectionInfo connectionInfo;

        public BusinessDocument(ConnectionInfo connectionInfo)
        {
            this.connectionInfo = connectionInfo;
        }

        public string Server => connectionInfo.HostName;
        public string DatabaseName => connectionInfo.DatabaseName;
        public EDatabaseType DatabaseType => connectionInfo.DatabaseType;
        public ISession OpenSession()
        {
            CurrentSession = new Session(connectionInfo);
            return CurrentSession;
        }

        public ISession CurrentSession { get; private set; }
    }
}