namespace OrientDB.Net.Core.BusinessObjects
{
    public abstract class BusinessObject : IBusinessObject
    {
        public string Id { get; internal set; }
        public string ClassName { get; protected set; }
        public int Version { get; internal set; }
    }
}