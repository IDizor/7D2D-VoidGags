using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VoidGags
{
    public class TypeToPatch
    {
        public Type Type { get; set; }
        public string MethodName { get; set; }
        public Type[] MethodParameters { get; set; }

        public static List<TypeToPatch> GetAllFromAssembly(Assembly asm, string methodName, string parameterName)
        {
            var result = new List<TypeToPatch>();
            if (asm != null)
            {
                if (asm.DefinedTypes?.Count() > 0)
                {
                    foreach (var typeInfo in asm.DefinedTypes)
                    {
                        var method = typeInfo.DeclaredMethods.FirstOrDefault(m => m.Name == methodName);
                        if (method != null)
                        {
                            var parameters = method.GetParameters();
                            if (parameters.Any(p => p.Name == parameterName))
                            {
                                result.Add(new TypeToPatch
                                {
                                    Type = typeInfo.AsType(),
                                    MethodName = methodName,
                                    MethodParameters = parameters.Select(p => p.ParameterType).ToArray(),
                                });
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
