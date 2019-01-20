namespace OrientDB.Net.Core.BusinessObjects
{
    public abstract class BusinessObject : IBusinessObject
    {
        public virtual string Id { get; internal set; }
        public virtual string ClassName { get; protected set; }
        public virtual int Version { get; internal set; }
    }
}