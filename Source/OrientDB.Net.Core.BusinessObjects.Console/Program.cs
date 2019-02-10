using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OrientDB.Net.Core.BusinessObjects.Generator;
using Proj1.Test;

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
                var l = new
                        {
                            Hi = "ge"
                        };

                var transaction = session.BeginTransaction();
                var comp = transaction.CreateCompany();
                comp.Name = "Neu";
                var member = transaction.CreateMember();
                member.Name = "Member";
                member.Address.Name = "Haus";
                comp.Members.Add(member);
                transaction.Commit();
                var tt = new[] {"Member", "Neu"};
                var companies = session.Get<ICompany>()
                                       .Where(c => c.Members.Any(m => m.Name == "Member"))
                                       .ToList();

                var z = session.Get<ICompany>().Where(c => c.Name != "Neudddd")
                               .OrderBy(c => c.Name)
                               .ToList()
                               .FirstOrDefault();
                
                transaction = session.BeginTransaction();
                comp.Name = "Neu 1";
                member = comp.Members.First();
                member.Name = "Member 1";
                member.Address.Name = "Haus 1";
                transaction.Update(comp);
                transaction.Commit();

                
                transaction = session.BeginTransaction();
                transaction.Commit();
            }
        }
    }
}