using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// This is a custom version of the Microsoft.VisualStudio.Shell.RegistrationAttribute to work around a breaking
    /// change in Visual Studio 16.5.0 due to a new feature for delayed intellisense. The fix should be set the 
    /// DeferUntilIntellisenseIsReady to false on RegistrationAttribute but the toolkit uses an older version of 
    /// Microsoft.VisualStudio.Shell that doesn't have DeferUntilIntellisenseIsReady in order to also support Visual Studio 2017.
    /// 
    /// The work around is create this custom version of RegistrationAttribute and set DeferUntilIntellisenseIsReady to false
    /// in the pkgdef during registration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class CustomProvideEditorFactoryAttribute : RegistrationAttribute
    {
        private ProvideEditorFactoryAttribute _underlyingAttribute;

        public CustomProvideEditorFactoryAttribute(Type factoryType, short nameResourceID)
        {
            _underlyingAttribute = new ProvideEditorFactoryAttribute(factoryType, nameResourceID);
        }

        public __VSEDITORTRUSTLEVEL TrustLevel
        {
            get { return _underlyingAttribute.TrustLevel; }
            set { _underlyingAttribute.TrustLevel = value; }
        }
        public int CommonPhysicalViewAttributes
        {
            get { return _underlyingAttribute.CommonPhysicalViewAttributes; }
            set { _underlyingAttribute.CommonPhysicalViewAttributes = value; }
        }

        public override void Register(RegistrationContext context)
        {
            _underlyingAttribute.Register(context);

            using (Key childKey = context.CreateKey($"Editors\\{_underlyingAttribute.FactoryType.GUID:B}"))
            {
                childKey.SetValue("DeferUntilIntellisenseIsReady", "false");
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            _underlyingAttribute.Unregister(context);
        }
    }

}
