using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Web.Administration;

using ThirdParty.Json.LitJson;

namespace AWSDeploymentHostManager
{
    internal static class ConfigHelper
    {
        private static string rewriteConfig = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<configuration>
    <system.webServer>
        <rewrite>
            <rules>
                <rule name=""AWS_DEPLOYMENT"" stopProcessing=""false"">
                    <match url=""^(https?://[^/]+/)MyApp"" ignoreCase=""true"" negate=""true"" />
                    <serverVariables>
                    </serverVariables>
                    <action type=""Rewrite"" url=""{R:1}MyApp{PATH_INFO}"" logRewrittenUrl=""true"" />
                    <conditions>
                        <add input=""{PATH_INFO}"" pattern=""^/MyApp"" negate=""true"" />
                    </conditions>
                </rule>
            </rules>
        </rewrite>
    </system.webServer>
</configuration>";

        private const string VDIR_REPLACE_PATTERN = "MyApp";
        private const string WWW_ROOT_WEB_CONFIG = @"c:\inetpub\wwwroot\Web.config";

        private static string ebs_config_schema_file = Environment.ExpandEnvironmentVariables(@"%windir%\Sysnative\inetsrv\config\schema\AWSDeployment_schema.xml");
        private static string ebs_config_schema_content =
@"<configSchema>
    <sectionSchema name=""AWSDeployment/environment"">
        <collection addElement=""add"" removeElement=""remove"" clearElement=""clear"">
            <attribute name=""key"" isUniqueKey=""true"" type=""string"" />
            <attribute name=""value"" type=""string"" />
        </collection>
    </sectionSchema>
</configSchema>";

        internal static void ConfigAppPool()
        {
            ServerManager manager = new ServerManager();
            ApplicationPool appPool = GetAppPool(manager);
            if (appPool != null)
            {
                appPool.Enable32BitAppOnWin64 = HostManager.Config.Enable32Bit;
                appPool.ManagedRuntimeVersion = String.Format("v{0}", HostManager.Config.TargetRuntime.ToString(2));
                manager.CommitChanges();
            }
            else
            {
                HostManager.LOGGER.Warn("could not find application pool - so not configuring it.");
                Event.LogWarn("HostManager", "could not find application pool - so not configuring it.");
            }
        }
        internal static void ConfigEnvironmentVariables()
        {
            ServerManager manager = new ServerManager();
            Configuration appConfig = manager.GetApplicationHostConfiguration();
            ConfigurationSection enviro;
            try
            {
                enviro = appConfig.GetSection("AWSDeployment/environment");
            }
            catch (Exception)
            {
                CreateEBSEnviroSection(manager, appConfig);
                manager = new ServerManager();
                appConfig = manager.GetApplicationHostConfiguration();
                enviro = appConfig.GetSection("AWSDeployment/environment");
            }
            if (enviro == null)
            {
                HostManager.LOGGER.Warn("could not add AWSDeployment/environment section, environment properties not available.");
                Event.LogWarn("HostManager", "could not add AWSDeployment/environment section, environment properties not available.");
                return;
            }

            ConfigurationElementCollection enviroVars = enviro.GetCollection();
            while (enviroVars.Count > 0)
            {
                RemoveLocalEnvironmentVariable(manager, (string)enviroVars[0].Attributes["key"].Value);
                enviroVars.RemoveAt(0);
            }
            foreach (KeyValuePair<string, JsonData> varToAdd in HostManager.Config.Environment)
            {
                ConfigurationElement newVar = enviroVars.CreateElement();
                newVar.Attributes["key"].Value = varToAdd.Key;
                newVar.Attributes["value"].Value = varToAdd.Value.ToString();
                enviroVars.Add(newVar);
            };
            manager.CommitChanges();
        }

        internal static void ConfigConnectionStrings(Dictionary<string,string> metadata)
        {
            string strDbSubstitue, dbServer, dbUser, dbPassword;
            if (!metadata.TryGetValue(HostManagerConfig.CONFIG_DBSUBSTITUTE, out strDbSubstitue))
            {
                HostManager.LOGGER.Info("Skipping setting up connections strings no substitute flag: " + strDbSubstitue);
                return;
            }
            if (!metadata.TryGetValue(HostManagerConfig.CONFIG_DBSERVER, out dbServer))
            {
                HostManager.LOGGER.Info("Skipping setting up connections strings no db server");
                return;
            }
            if (!metadata.TryGetValue(HostManagerConfig.CONFIG_DBUSER, out dbUser))
            {
                HostManager.LOGGER.Info("Skipping setting up connections strings no db user");
                return;
            }
            if (!metadata.TryGetValue(HostManagerConfig.CONFIG_DBPASSWORD, out dbPassword))
            {
                HostManager.LOGGER.Info("Skipping setting up connections strings no db password");
                return;
            }

            bool substitute = false;
            if (!bool.TryParse(strDbSubstitue, out substitute) || !substitute)
            {
                HostManager.LOGGER.Info("Skipping setting up connections strings: " + strDbSubstitue);
                return;
            }

            var appPath = HostManager.AppPath;
            if (appPath.StartsWith(HostManager.SiteName))
                appPath = appPath.Substring(HostManager.SiteName.Length);

            HostManager.LOGGER.InfoFormat("Updated Connections on site {0} app {1}", HostManager.SiteName, appPath);

            if (dbServer == null || dbUser == null || dbPassword == null)
            {
                HostManager.LOGGER.InfoFormat("{0} was turned on but missing required dependent setting", HostManagerConfig.CONFIG_DBSUBSTITUTE);
                return;
            }

            ServerManager manager = new ServerManager();
            Site webSite = manager.Sites[HostManager.SiteName];
            if (webSite == null)
            {
                HostManager.LOGGER.ErrorFormat("Failed to find {0} web site, skipping setting up connection strings", HostManager.SiteName);
                return;
            }

            var app = webSite.Applications[appPath];
            if (app == null)
            {
                HostManager.LOGGER.ErrorFormat("Failed to find {0} application, skipping setting up connection strings", appPath);
                return;
            }
            var appConfig = app.GetWebConfiguration();
            if (appConfig == null)
            {
                HostManager.LOGGER.ErrorFormat("Failed to find app config, skipping setting up connection strings");
                return;
            }
            var connStrings = appConfig.GetSection("connectionStrings");

            foreach (var child in connStrings.GetCollection())
            {
                if (!child.IsLocallyStored)
                    continue;

                var connName = child.GetAttributeValue("name") as string;
                if (string.IsNullOrWhiteSpace(connName) || child.Attributes["connectionString"] == null)
                    continue;

                string connectionString = string.Format("Database={0};Server={1};User ID={2};Password={3};", connName, dbServer, dbUser, dbPassword);
                child.SetAttributeValue("connectionString", connectionString);
                child.SetAttributeValue("providerName", "System.Data.SqlClient");
                HostManager.LOGGER.InfoFormat("Updated connection string {0}", connName);
            }

            manager.CommitChanges();

        }

        internal static void CreateEBSEnviroSection(ServerManager manager, Configuration config)
        {
            HostManager.LOGGER.Info("could not find AWSDeployment/environment section, creating in applicationHost.config");
            Event.LogInfo("HostManager", "could not find AWSDeployment/environment section, creating in applicationHost.config");

            SectionGroup ebsSection = FindOrCreateGroup("AWSDeployment", config.RootSectionGroup.SectionGroups);
            SectionDefinition environment = FindOrCreateSection("environment", ebsSection.Sections);

            manager.CommitChanges();
            AddEBSEnvironSchema(GetAppPool(manager));
        }
        internal static SectionGroup FindOrCreateGroup(string name, SectionGroupCollection groups)
        {
            SectionGroup ret = null;
            foreach (SectionGroup group in groups)
            {
                if (group.Name == name)
                {
                    ret = group;
                    break;
                }
            }
            if (ret == null)
            {
                ret = groups.Add(name);
            }
            return ret;
        }
        internal static SectionDefinition FindOrCreateSection(string name, SectionDefinitionCollection sections)
        {
            SectionDefinition ret = null;
            foreach (SectionDefinition section in sections)
            {
                if (section.Name == name)
                {
                    ret = section;
                    break;
                }
            }
            if (ret == null)
            {
                ret = sections.Add(name);
            }
            return ret;
        }
        internal static void AddEBSEnvironSchema(ApplicationPool appPool)
        {
            if (!System.IO.File.Exists(ebs_config_schema_file))
            {
                System.IO.StreamWriter ebs_schema = new System.IO.StreamWriter(ebs_config_schema_file);
                ebs_schema.WriteLine(ebs_config_schema_content);
                ebs_schema.Close();

                if (appPool != null)
                {
                    appPool.Recycle();
                    while (appPool.State != ObjectState.Started)
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
            }
        }
        internal static bool RemoveLocalEnvironmentVariable(ServerManager manager, string name)
        {
            ConfigurationElementCollection webEnviro = manager.Sites[HostManager.SiteName].GetWebConfiguration().GetSection("appSettings").GetCollection();
            ConfigurationElement toRemove = null;
            foreach (ConfigurationElement variable in webEnviro)
            {
                if ((string)variable.Attributes["key"].Value == name)
                {
                    toRemove = variable;
                    break;
                }
            }
            if (toRemove != null)
            {
                webEnviro.Remove(toRemove);
                return true;
            }
            return false;
        }
        internal static void SetLocalEnvironmentVariables()
        {
            ServerManager manager = new ServerManager();
            ConfigurationElementCollection appEnviro = manager.GetApplicationHostConfiguration().GetSection("AWSDeployment/environment").GetCollection();
            ConfigurationElementCollection webEnviro = manager.Sites[HostManager.SiteName].GetWebConfiguration().GetSection("appSettings").GetCollection();
            List<ConfigurationElement> toBeRemoved = new List<ConfigurationElement>();
            foreach (ConfigurationElement variable in appEnviro)
            {
                bool present = false;
                foreach (ConfigurationElement webVar in webEnviro)
                {
                    if ((string)webVar.GetAttribute("key").Value == (string)variable.GetAttribute("key").Value)
                    {
                        present = true;
                    }
                }
                if (!present)
                {
                    ConfigurationElement newVar = webEnviro.CreateElement();
                    newVar.GetAttribute("key").Value = variable.GetAttribute("key").Value;
                    newVar.GetAttribute("value").Value = variable.GetAttribute("value").Value;
                    webEnviro.Add(newVar);
                }
                else
                {
                    toBeRemoved.Add(variable);
                }
            }
            foreach (ConfigurationElement variable in toBeRemoved)
            {
                appEnviro.Remove(variable);
            }
            manager.CommitChanges();
        }
        internal static ApplicationPool GetAppPool(ServerManager manager)
        {
            Site webSite = manager.Sites[HostManager.SiteName];
            if (webSite == null)
            {
                HostManager.LOGGER.Warn("could not find web site so can't get application pool");
                Event.LogWarn("HostManager", "could not find web site so can't get application pool");
                return null;
            }
            Application app = webSite.Applications[HostManager.AppName];
            if (app == null)
            {
                HostManager.LOGGER.Info("could not find application so returning the default application pool");
                Event.LogInfo("HostManager", "could not find application so returning the default application pool");
                return manager.ApplicationPools[webSite.ApplicationDefaults.ApplicationPoolName];
            }
            return manager.ApplicationPools[app.ApplicationPoolName];
        }
        internal static void SetBindings()
        {
            ServerManager manager = new ServerManager();
            Site webSite = manager.Sites[HostManager.SiteName];

            int port = 80;
            bool isSecure = false;

            if (webSite != null)
            {
                BindingCollection bindings = webSite.Bindings;
                bool foundPort = false;
                foreach (Binding binding in bindings)
                {
                    if (String.Equals(binding.Protocol, "http", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isSecure = false;
                        string[] info = binding.BindingInformation.Split(':');

                        string ip = info[0];
                        string portNum = info[1];
                        string host = info[2];

                        foundPort = Int32.TryParse(portNum, out port);
                        if (foundPort)
                        {
                            break;
                        }
                    }
                }
                if (!foundPort)
                {
                    foreach (Binding binding in bindings)
                    {
                        if (String.Equals(binding.Protocol, "https", StringComparison.InvariantCultureIgnoreCase))
                        {
                            isSecure = true;
                            string[] info = binding.BindingInformation.Split(':');

                            string ips = info[0];
                            string portNum = info[1];
                            string hosts = info[2];

                            foundPort = Int32.TryParse(portNum, out port);
                            if (foundPort)
                            {
                                break;
                            }
                        }
                    }
                }
                if (!foundPort)
                {
                    isSecure = false;
                    port = 80;
                }
            }

            HostManager.IsSecure = isSecure;
            HostManager.Port = port;
        }
        internal static void AdjustSiteWebConfig(string appPath)
        {
            if (!HostManager.AppName.Equals("/", StringComparison.InvariantCultureIgnoreCase))
            {
                ServerManager manager = new ServerManager();
                try
                {
                    Site webSite = manager.Sites[HostManager.SiteName];
                    if (webSite == null)
                    {
                        HostManager.LOGGER.Warn("Couldn't locate website, adding rewrite rules to default web site");
                        Event.LogWarn("HostManager", "Couldn't locate website, adding rewrite rules to default web site");
                        webSite = manager.Sites["Default Web Site"];
                        if (webSite == null)
                        {
                            HostManager.LOGGER.Warn("Couldn't locate default website, replacing web.config at default location");
                            Event.LogWarn("HostManager", "Couldn't locate default website, replacing web.config at default location");
                            SetRewriteRules(appPath, WWW_ROOT_WEB_CONFIG);
                            return;
                        }
                    }

                    Configuration rootConfig = webSite.GetWebConfiguration();
                    if (rootConfig == null)
                    {
                        HostManager.LOGGER.Warn("Couldn't locate websites Web.config, replacing Web.config at default location");
                        Event.LogWarn("HostManager", "Couldn't locate websites Web.config, replacing Web.config at default location");
                        SetRewriteRules(appPath, WWW_ROOT_WEB_CONFIG);
                        return;
                    }

                    ConfigurationSection rulesSection = null;

                    try
                    {
                        rulesSection = rootConfig.GetSection("system.webServer/rewrite/rules");
                    }
                    catch (Exception)
                    {
                        //Section did not exist so create it.
                        CreateWebServerRewriteSection(manager);
                        rulesSection = rootConfig.GetSection("system.webServer/rewrite/rules");
                    }
                    if (rulesSection == null)
                    {
                        HostManager.LOGGER.Warn("websites Web.config couldn't find system.WebSever/rewrite/rules section, rewrite rules not added");
                        Event.LogWarn("HostManager", "websites Web.config couldn't find system.WebSever/rewrite/rules section, rewrite rules not added");
                        return;
                    }

                    ConfigurationElementCollection rulesCollection = rulesSection.GetCollection();
                    List<ConfigurationElement> rulesToRemove = new List<ConfigurationElement>();
                    foreach (ConfigurationElement ruleElement in rulesCollection)
                    {
                        if (ruleElement.GetAttribute("name").Value.ToString() == "AWS_DEPLOYMENT")
                        {
                            rulesToRemove.Add(ruleElement);
                        }
                    }
                    foreach (ConfigurationElement removeRule in rulesToRemove)
                    {
                        rulesCollection.Remove(removeRule);
                    }
                    AddRule(HostManager.AppName.Substring(1), rulesCollection);
                }
                finally
                {
                    manager.CommitChanges();
                }
            }
        }
        private static void AddRule(string MyApp, ConfigurationElementCollection rules)
        {
            ConfigurationElement newRule = rules.CreateElement("rule");
            newRule.Attributes["name"].Value = "AWS_DEPLOYMENT";
            newRule.Attributes["stopProcessing"].Value = false;

            ConfigurationElement child = newRule.GetChildElement("match");
            child.Attributes["url"].Value = String.Format("^(https?://[^/]+/){0}", MyApp);
            child.Attributes["ignoreCase"].Value = true;
            child.Attributes["negate"].Value = true;

            child = newRule.GetChildElement("serverVariables");

            child = newRule.GetChildElement("action");
            child.Attributes["type"].Value = "Rewrite";
            child.Attributes["url"].Value = String.Format("{0}{1}{2}", "{R:1}", MyApp, "{PATH_INFO}");
            child.Attributes["logRewrittenUrl"].Value = true;

            child = newRule.GetChildElement("conditions");

            ConfigurationElementCollection conditions = child.GetCollection();
            ConfigurationElement conditionsChild = conditions.CreateElement();
            conditionsChild.Attributes["input"].Value = "{PATH_INFO}";
            conditionsChild.Attributes["pattern"].Value = String.Format("^/{0}", MyApp);
            conditionsChild.Attributes["negate"].Value = true;
            child.GetCollection().Add(conditionsChild);

            rules.Add(newRule);
        }
        private static void SetRewriteRules(string appPath, string filePath)
        {
            string[] elements = appPath.Split(new char[] { '/' });

            if (elements.Length < 2) // The package is being deployed at the root, so no rewrite rules needed.
                return;

            string vDir = elements.Last();

            System.IO.StreamWriter config = new System.IO.StreamWriter(filePath);

            string xml = rewriteConfig.Replace(VDIR_REPLACE_PATTERN, vDir);

            config.WriteLine(xml);
            config.Close();
        }
        internal static void SetRootAppPool()
        {
            ServerManager manager = new ServerManager();
            ApplicationPool appPool = ConfigHelper.GetAppPool(manager);
            if (appPool != null)
            {
                Application rootApp = manager.Sites[HostManager.SiteName].Applications["/"];
                if (rootApp != null)
                {
                    rootApp.ApplicationPoolName = appPool.Name;
                    manager.CommitChanges();
                    return;
                }
                HostManager.LOGGER.Warn("could not find root application on web site so not configuring its application pool");
                Event.LogWarn("HostManager", "could not find root application on web site so not configuring its application pool");
                return;
            }
            HostManager.LOGGER.Warn("could not find web site so not configuring root application's application pool");
            Event.LogWarn("HostManager", "could not find web site so not configuring root application's application pool");
        }
        private static void CreateWebServerRewriteSection(ServerManager manager)
        {
            HostManager.LOGGER.Warn("could not find system.webServer\rewrite\rules section of WebConfig - is IIS rewrite module installed?");
            Event.LogWarn("HostManager", "could not find system.webServer\rewrite\rules section of WebConfig - is IIS rewrite module installed?");

            Configuration appHostConfig = manager.GetApplicationHostConfiguration();
            SectionGroup webServerGroup = null;
            foreach (SectionGroup group in appHostConfig.RootSectionGroup.SectionGroups)
            {
                if (group.Name == "system.webServer")
                {
                    webServerGroup = group;
                    break;
                }
            }
            if (webServerGroup == null)
            {
                webServerGroup = appHostConfig.RootSectionGroup.SectionGroups.Add("system.webServer");
            }

            SectionGroup rewriteGroup = null;
            foreach (SectionGroup group in webServerGroup.SectionGroups)
            {
                if (group.Name == "rewrite")
                {
                    rewriteGroup = group;
                    break;
                }
            }
            if (rewriteGroup == null)
            {
                rewriteGroup = webServerGroup.SectionGroups.Add("rewrite");
            }

            SectionDefinition rulesSection = rewriteGroup.Sections.Add("rules");
            rulesSection.OverrideModeDefault = "Allow";

            manager.CommitChanges();
        }
    }
}
