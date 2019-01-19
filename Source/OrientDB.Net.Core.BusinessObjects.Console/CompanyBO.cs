namespace OrientDB.Net.Core.BusinessObjects.Console
{
    public class CompanyBO : BusinessObject, ICompanyBO
    {
        public CompanyBO()
        {
            ClassName = "Company";
        }

        [DocumentProperty("CompanyName", true)]
        public string Name { get; set; }
    }

    public static class CompanyBOExtension{
        public static ICompanyBO CreateCompany(this ITransaction transaction)
        {
            var company = new CompanyBO();
            transaction.Create(company);
            return company;
        }
    }
}