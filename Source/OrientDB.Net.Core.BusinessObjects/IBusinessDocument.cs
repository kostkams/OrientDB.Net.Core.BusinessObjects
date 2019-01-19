namespace OrientDB.Net.Core.BusinessObjects
{
    public interface IBusinessDocument
    {
        string Server { get; }
        string DatabaseName { get; }
        EDatabaseType DatabaseType { get; }

        ISession OpenSession();

        ISession CurrentSession { get; }
    }
}