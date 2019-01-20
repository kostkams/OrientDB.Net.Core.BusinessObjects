using System.Reflection;
using OrientDB.Net.Core.BusinessObjects;

namespace Proj1.Test
{
    public class MemberBO : BusinessObject, IMember
    {
        public MemberBO()
        {
            ClassName = "Member";
            Address = new AddressBO();
        }

        [DocumentProperty("Name", true)]
        public string Name { get; set; }
        [Child("memberAddress")]
        public IAddress Address { get; set; }
    }

    public static class MemberBOExtension
    {
        public static IMember CreateMember(this ITransaction transaction)
        {
            var member = new MemberBO();
            transaction.Create(member);
            return member;
        }
    }
}
