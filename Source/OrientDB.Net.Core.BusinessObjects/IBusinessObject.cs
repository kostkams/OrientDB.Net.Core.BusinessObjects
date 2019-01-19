namespace OrientDB.Net.Core.BusinessObjects
{
    public interface IBusinessObject
    {
        string Id { get; }

        string ClassName { get; }

        int Version { get; }
    }
}