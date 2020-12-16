Visual Studio Public Assemblies folder

### Microsoft.VisualStudio.TemplateWizardInterface

- Reason:
  - Microsoft.VisualStudio.TemplateWizardInterface version 8.0.0.0 is referenced to support both VS 2017 and 2019
  - compiling with VS 2019 16.8 results in build failures (2019 has v16.0.0.0, not v8.0.0.0 of this assembly)
  - inspiration: https://github.com/SpecFlowOSS/SpecFlow.VisualStudio/pull/101
- Files:
  - Microsoft.VisualStudio.TemplateWizardInterface.dll
  - Microsoft.VisualStudio.TemplateWizardInterface.xml
- Source: Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\PublicAssemblies
