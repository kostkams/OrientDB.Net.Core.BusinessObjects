using System;
using System.Linq;

namespace OrientDB.Net.Core.BusinessObjects
{
    internal static class BoActivator
    {
        public static Type GetImplementationType(Type type)
        {
            var requestedType = type;
            return requestedType.Assembly
                                    .GetExportedTypes()
                                    .Single(t => !t.IsInterface && !t.IsAbstract && requestedType.IsAssignableFrom(t));

        }

        public static BusinessObject GetInstance(Type type)
        {
            return (BusinessObject) Activator.CreateInstance(GetImplementationType(type));
        }
    }
}