using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Uno.Logging;

namespace Uno.Build.Targets
{
    public static class BuildTargets
    {
        public static readonly BuildTarget Default = new DotNet.DotNetBuild();
        public static readonly BuildTarget Package = new Uno.PackageBuild();

        public static readonly BuildTarget[] All =
        {
            new Android.AndroidBuild(),
            new Native.NativeBuild(),
            new Xcode.iOSBuild(),
            new DotNet.DotNetDllBuild(),
            Default,
            new Uno.CorelibBuild(),
            new Uno.DocsBuild(),
            new DotNet.MetadataBuild(),
            new PInvoke.PInvokeBuild(),
            Package,
        };

        public static IEnumerable<BuildTarget> Enumerate(bool experimental = true, bool obsolete = false)
        {
            return All.Where(e => (experimental || !e.IsExperimental) && (obsolete || !e.IsObsolete));
        }

        public static BuildTarget Get(string name, List<string> args = null, string def = null)
        {
            BuildTarget result;
            if (name == null && args != null)
            {
                if (args.Count > 0 && TryGet(args[0], out result) && (
                        string.Compare(args[0], result.Identifier, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        !Directory.Exists(args[0])
                    ))
                {
                    args.RemoveAt(0);
                    return result;
                }

                name = def ?? Default.Identifier;
            }

            if (name != null && TryGet(name, out result))
                return result;

            throw new ArgumentException(name.Quote() + " is not a valid build target -- see \"uno build --help\" for a list of targets");
        }

        static bool TryGet(string name, out BuildTarget result)
        {
            result = TryGet(name);
            return result != null;
        }

        static BuildTarget TryGet(string name)
        {
            var nameUpper = name.ToUpperInvariant();

            foreach (var t in Enumerate(true, true))
                if (t.Identifier.ToUpperInvariant() == nameUpper)
                    return t;
            foreach (var t in Enumerate(true, true))
            {
                foreach (var n in t.FormerNames)
                {
                    if (n.ToUpperInvariant() == nameUpper)
                    {
                        Log.Default.Warning($"The build target '{n.ToLowerInvariant()}' is deprecated -- use '{t.Identifier.ToLowerInvariant()}' to silence this message.");
                        return t;
                    }
                }
            }

            // Fuzzy --target finder
            BuildTarget result = null;
            if (nameUpper.Length > 0)
            {
                foreach (var t in Enumerate())
                {
                    if ((!t.IsExperimental || Log.Default.EnableExperimental) &&
                        t.Identifier.ToUpperInvariant().StartsWith(nameUpper, StringComparison.InvariantCulture))
                    {
                        if (result != null)
                            return null;

                        result = t;
                    }
                }
            }

            return result;
        }
    }
}
