using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace ItemCreationInterCompanyUtility
{
    /// <summary>The two utilities that now live in this one application. Chosen on the login screen.</summary>
    public enum AppModule
    {
        Vehicle,
        Parts
    }

    /// <summary>
    /// Settings from AppConfig.xml for the module chosen at login.
    ///
    /// AppConfig.xml layout:
    ///   &lt;CompanyDetails&gt;
    ///     &lt;Common&gt;  settings shared by both modules  &lt;/Common&gt;
    ///     &lt;Vehicle&gt; settings for Vehicle only          &lt;/Vehicle&gt;
    ///     &lt;Parts&gt;   settings for Spare Parts only      &lt;/Parts&gt;
    ///   &lt;/CompanyDetails&gt;
    ///
    /// A setting in the module section wins over the same setting in Common, so any setting can be
    /// made module-specific just by moving (or repeating) it there.
    /// The result looks exactly like the old flat file: one table "CompanyDetails" with one row,
    /// so existing code keeps reading  ds.Tables[0].Rows[0]["ServerName"].
    /// </summary>
    public static class AppSettings
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<AppModule, DataSet> _cache = new Dictionary<AppModule, DataSet>();
        private static DateTime _cacheFileTime = DateTime.MinValue;

        /// <summary>Module chosen on the login screen.</summary>
        public static AppModule Module = AppModule.Vehicle;

        /// <summary>"Vehicle" or "Spare Parts" (for titles and messages).</summary>
        public static string ModuleTitle
        {
            get { return Module == AppModule.Parts ? "Spare Parts" : "Vehicle"; }
        }

        public static string ConfigPath
        {
            get
            {
                string path = Path.Combine(Application.StartupPath, "AppConfig.xml");
                return File.Exists(path) ? path : "AppConfig.xml";
            }
        }

        /// <summary>Settings of the current module (common settings + the module's own section).</summary>
        public static DataSet Load()
        {
            return Load(Module);
        }

        public static DataSet Load(AppModule module)
        {
            lock (_lock)
            {
                string path = ConfigPath;
                DateTime fileTime = File.GetLastWriteTimeUtc(path);
                if (fileTime != _cacheFileTime)
                {
                    _cache.Clear();               // file was edited: read it again
                    _cacheFileTime = fileTime;
                }

                DataSet ds;
                if (!_cache.TryGetValue(module, out ds))
                {
                    ds = Read(path, module);
                    _cache[module] = ds;
                }
                return ds;
            }
        }

        private static DataSet Read(string path, AppModule module)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlElement root = doc.DocumentElement;
            if (root == null)
                throw new Exception("AppConfig.xml is empty.");

            // Later sources overwrite earlier ones:
            //   1. settings written directly under the root (old flat layout)
            //   2. <Common>
            //   3. <Vehicle> or <Parts>
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var order = new List<string>();

            string moduleSection = module.ToString();
            bool moduleSectionFound = false;

            foreach (XmlNode node in root.ChildNodes)
            {
                XmlElement el = node as XmlElement;
                if (el == null || IsSection(el.Name)) continue;
                Put(values, order, el.Name, el.InnerText);
            }
            foreach (XmlNode node in root.ChildNodes)
            {
                XmlElement el = node as XmlElement;
                if (el != null && string.Equals(el.Name, "Common", StringComparison.OrdinalIgnoreCase))
                    PutChildren(values, order, el);
            }
            foreach (XmlNode node in root.ChildNodes)
            {
                XmlElement el = node as XmlElement;
                if (el != null && string.Equals(el.Name, moduleSection, StringComparison.OrdinalIgnoreCase))
                {
                    PutChildren(values, order, el);
                    moduleSectionFound = true;
                }
            }

            if (!values.ContainsKey("CurrentSchema") || !values.ContainsKey("BaseDB"))
                throw new Exception("AppConfig.xml has no " + (moduleSectionFound ? "CurrentSchema / BaseDB in the" : "")
                    + " <" + moduleSection + "> section. Add it under <CompanyDetails>.");

            DataTable table = new DataTable("CompanyDetails");
            foreach (string name in order)
                table.Columns.Add(name, typeof(string));
            DataRow row = table.NewRow();
            foreach (string name in order)
                row[name] = values[name];
            table.Rows.Add(row);

            DataSet result = new DataSet("AppConfig");
            result.Tables.Add(table);
            return result;
        }

        private static bool IsSection(string name)
        {
            return string.Equals(name, "Common", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Vehicle", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Parts", StringComparison.OrdinalIgnoreCase);
        }

        private static void PutChildren(Dictionary<string, string> values, List<string> order, XmlElement section)
        {
            foreach (XmlNode node in section.ChildNodes)
            {
                XmlElement el = node as XmlElement;
                if (el != null) Put(values, order, el.Name, el.InnerText);
            }
        }

        private static void Put(Dictionary<string, string> values, List<string> order, string name, string value)
        {
            if (!values.ContainsKey(name)) order.Add(name);
            values[name] = value ?? string.Empty;
        }
    }
}
