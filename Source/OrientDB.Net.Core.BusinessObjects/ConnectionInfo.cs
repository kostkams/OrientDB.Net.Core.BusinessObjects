namespace OrientDB.Net.Core.BusinessObjects
{
    public class ConnectionInfo
    {
        public ConnectionInfo(string hostName,
                              int port,
                              string userName,
                              string password,
                              string databaseName,
                              EDatabaseType databaseType,
                              string poolAlias)
        {
            HostName = hostName;
            UserName = userName;
            Password = password;
            Port = port;
            DatabaseName = databaseName;
            DatabaseType = databaseType;
            PoolAlias = poolAlias;
        }

        public ConnectionInfo(string hostName,
                              int port,
                              string userName,
                              string password,
                              string databaseName,
                              EDatabaseType databaseType)
            : this(hostName, port, userName, password, databaseName, databaseType, "Default")
        {
        }

        public string HostName { get; }
        public string UserName { get; }
        public string Password { get; }
        public int Port { get; }
        public string DatabaseName { get; }
        public EDatabaseType DatabaseType { get; }
        public string PoolAlias { get; }
    }
}