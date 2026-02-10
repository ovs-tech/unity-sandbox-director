using System;
using System.Reflection;

namespace Systems.MiniTimeline.Core
{
    /// <summary>
    /// Helper to resolve types across loaded assemblies robustly.
    /// </summary>
    public static class TypeResolver
    {
        public static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            Type t = Type.GetType(typeName);
            if (t != null) return t;

            string shortName = typeName;
            int comma = typeName.IndexOf(',');
            if (comma >= 0) shortName = typeName.Substring(0, comma).Trim();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(shortName);
                    if (t != null) return t;

                    foreach (var candidate in asm.GetTypes())
                    {
                        if (candidate.FullName == shortName || candidate.Name == shortName || candidate.FullName.EndsWith("." + shortName))
                        {
                            return candidate;
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // ignore assemblies we can't fully inspect
                }
                catch (Exception)
                {
                    // ignore unexpected failures per-assembly
                }
            }

            return null;
        }
    }
}
