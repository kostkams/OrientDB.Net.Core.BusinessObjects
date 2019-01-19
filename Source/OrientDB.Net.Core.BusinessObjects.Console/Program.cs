using System.Linq;

namespace OrientDB.Net.Core.BusinessObjects.Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var businessDocument = BusinessDocumentFactory.Connect(new ConnectionInfo("localhost",
                                                                                      2424,
                                                                                      "root",
                                                                                      "rootpwd",
                                                                                      "Helios",
                                                                                      EDatabaseType.Graph));

            using (var session = businessDocument.OpenSession())
            {
                var company = session.Get<ICompanyBO>(e => "Test" == e.Name && (e.Name != "tu" || e.Name != "tsssu") /**/);
                var transaction = session.BeginTransaction();
                var comp = transaction.CreateCompany();
                comp.Name = "Neu";
                transaction.Commit();
                var company1 = session.Get<ICompanyBO>();
                
                transaction = session.BeginTransaction();
                comp.Name = "Neu 1";
                transaction.Update(comp);
                transaction.Commit();

                
                transaction = session.BeginTransaction();
                transaction.Delete(session.Get<ICompanyBO>(c => c.Name == "Neu 1").First());
                transaction.Commit();
            }
        }
    }
}