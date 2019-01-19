namespace OrientDB.Net.Core.BusinessObjects
{
    public static class BusinessDocumentFactory
    {
        public static IBusinessDocument Connect(ConnectionInfo connectionInfo)
        {
            var businessDocument = new BusinessDocument(connectionInfo);
            return businessDocument;
        }
    }
}
