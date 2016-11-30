using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;

using Amazon.RDS.Model;
using System.Reflection;

namespace Amazon.AWSToolkit.RDS.Model
{
    /// <summary>
    /// Wraps the database engines available in RDS for the purposes of list selection
    /// </summary>
    public class DBEngineVersionWrapper
    {
        public DBEngineVersionWrapper(DBEngineVersion engine)
        {
            EngineVersion = engine;
        }

        private DBEngineVersionWrapper() { }

        public DBEngineVersion EngineVersion { get; private set; }

        public string FormattedEngineVersionAndDescription
        {
            get
            {
                if (string.IsNullOrEmpty(EngineVersion.Engine))
                    return this.EngineVersion.EngineVersion;

                return string.Format("{0} ({1})", this.EngineVersion.EngineVersion, EngineVersion.DBEngineVersionDescription);
            }
        }

        public string Title 
        {
            get { return EngineVersion.Engine; }
        }

        public string Description 
        {
            get { return EngineVersion.DBEngineDescription; }
        }

        public ImageSource EngineIcon
        {
            get { return IconForEngineType(EngineVersion.Engine); }
        }

        public static ImageSource IconForEngineType(string engine)
        {
            string iconPath = null;
            // clunky, but it works for now until we determine how we can describe available engines
            if (engine.StartsWith("mysql", StringComparison.InvariantCultureIgnoreCase))
                iconPath = "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.logo_mysql.png";
            else if (engine.StartsWith("oracle", StringComparison.InvariantCultureIgnoreCase))
                iconPath = "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.logo_oracle.png";
            else if (engine.StartsWith("sqlserver", StringComparison.InvariantCultureIgnoreCase))
                iconPath = "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.logo_sqlserver.png";
            else if (engine.StartsWith("postgres", StringComparison.InvariantCultureIgnoreCase))
                iconPath = "Amazon.AWSToolkit.RDS.Resources.EmbeddedImages.logo_postgres.png";

            Image icon = null;
            if (!string.IsNullOrEmpty(iconPath))
                icon = IconHelper.GetIcon(Assembly.GetExecutingAssembly(), iconPath);

            return icon != null ? icon.Source : null;
        }

    }
}
