using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    public class AboutWindowModel : ModelBase
    {
        public string ApplicationName => "USortThis";
        public string Version
        {
            get
            {
                // Prefer informational version (includes prerelease/build metadata) when present
                var entry = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                if (entry != null)
                {
                    var infoAttr = entry.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                    if (infoAttr != null && !string.IsNullOrWhiteSpace(infoAttr.InformationalVersion))
                        return infoAttr.InformationalVersion;

                    // Fall back to product/file version
                    try
                    {
                        var fileVer = FileVersionInfo.GetVersionInfo(entry.Location).ProductVersion;
                        if (!string.IsNullOrEmpty(fileVer))
                            return fileVer;
                    }
                    catch
                    {
                        // ignore and fall back to assembly version
                    }

                    var asmVer = entry.GetName().Version;
                    if (asmVer != null)
                        return asmVer.ToString();
                }

                return "Unknown";
            }
        }
        public string Copyright
        {
            get
            {
                var entry = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                if (entry != null)
                {
                    var attr = entry.GetCustomAttribute<AssemblyCopyrightAttribute>();
                    if (attr != null && !string.IsNullOrWhiteSpace(attr.Copyright))
                        return attr.Copyright;
                }
                return string.Empty;
            }
        }
        public string Author
        {
            get
            {
                var entry = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                if (entry != null)
                {
                    var attr = entry.GetCustomAttribute<AssemblyCompanyAttribute>();
                    if (attr != null && !string.IsNullOrWhiteSpace(attr.Company))
                        return attr.Company;
                }
                return string.Empty;
            }
        }

        public AboutWindowModel() : base(0)
        {

        }
    }
}
