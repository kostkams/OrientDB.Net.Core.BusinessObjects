using System.Collections.Generic;
using OrientDB.Net.Core.BusinessObjects;

namespace Proj1.Test
{
    public class CompanyBO : BusinessObject, ICompany
    {
        public CompanyBO()
        {
            ClassName = "Company";
            Members = new ReferenceList<IMember>();
        }

        [DocumentProperty("CompanyName", true)]
        public string Name { get; set; }
        [ReferenceList("hasMember")]
        public IList<IMember> Members { get; }
    }

    public static class CompanyBOExtension
    {
        public static ICompany CreateCompany(this ITransaction transaction)
        {
            var company = new CompanyBO();
            transaction.Create(company);
            return company;
        }
    }
}
