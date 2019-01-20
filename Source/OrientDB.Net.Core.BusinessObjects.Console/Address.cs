using OrientDB.Net.Core.BusinessObjects;

namespace Proj1.Test
{
    public class AddressBO : BusinessObject, IAddress
    {
        public AddressBO()
        {
            ClassName = "Address";
        }

        [DocumentProperty("Name", true)]
        public string Name { get; set; }
    }
}
