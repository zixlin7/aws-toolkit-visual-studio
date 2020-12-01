using EnvDTE;
using System;
using System.Collections.Generic;
using log4net;

namespace Amazon.AWSToolkit.VisualStudio.Shared
{
    public static class VSLambdaUtility
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(VSLambdaUtility));

        public static IDictionary<string, IList<string>> SearchForLambdaFunctionSuggestions(Project project)
        {
            IDictionary<string, IList<string>> suggestedMethods = new Dictionary<string, IList<string>>();
            try
            {
                SearchForClasses(project.ProjectItems, suggestedMethods);
            }
            catch(Exception e)
            {
                LOGGER.Error("Error getting possibe lambda function suggestions", e);
            }

            return suggestedMethods;
        }

        private static void SearchForClasses(ProjectItems projectItems,
            IDictionary<string, IList<string>> suggestedMethods)
        {
            if (projectItems == null)
                return;

            foreach (ProjectItem projectItem in projectItems)
            {
                if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder &&
                    projectItem.ProjectItems != null)
                {
                    SearchForClasses(projectItem.ProjectItems, suggestedMethods);
                }
                else
                {
                    if (projectItem.Name != null && projectItem.Name.EndsWith(".cs") &&
                        projectItem.FileCodeModel != null && projectItem.FileCodeModel.CodeElements != null)
                    {
                        SearchForClasses(projectItem.FileCodeModel.CodeElements, suggestedMethods);
                    }
                }
            }
        }

        private static void SearchForClasses(CodeElements elements, IDictionary<string, IList<string>> suggestedMethods)
        {
            if (elements == null)
                return;

            foreach (CodeElement element in elements)
            {
                if (element.Kind == vsCMElement.vsCMElementClass)
                {
                    CodeClass cls = element as CodeClass;
                    if (cls != null && !cls.IsAbstract)
                    {
                        List<string> classMethods = new List<string>();

                        bool hasDefaultConstructor;
                        SearchForMethods(cls, classMethods, out hasDefaultConstructor);
                        if (hasDefaultConstructor && classMethods.Count > 0)
                        {
                            suggestedMethods[element.FullName] = classMethods;
                        }
                        else
                        {
                            foreach(CodeElement b in cls.Bases)
                            {
                                CodeClass bc = b as CodeClass;
                                string fullName = bc.FullName;
                                if(fullName.Contains("APIGatewayProxyFunction"))
                                {
                                    suggestedMethods[element.FullName] = new List<string> { "FunctionHandlerAsync" };
                                    break;
                                }
                            }
                        }
                    }
                }

                SearchForClasses(element.Children, suggestedMethods);
            }
        }

        private static void SearchForMethods(CodeClass codeClass, List<string> classMethods, out bool hasDefaultConstructor)
        {
            hasDefaultConstructor = false;
            if (codeClass == null)
                return;

            SearchForMethods(codeClass.Children, classMethods, out hasDefaultConstructor);

            bool baseDefaultConstructorLookup;
            foreach(CodeElement bs in codeClass.Bases)
            {
                CodeClass bc = bs as CodeClass;
                if (bc == null)
                    continue;

                if (bc.InfoLocation != vsCMInfoLocation.vsCMInfoLocationExternal)
                {
                    SearchForMethods(bc, classMethods, out baseDefaultConstructorLookup);
                }                   
            }
        }

        private static void SearchForMethods(CodeElements elements, List<string> classMethods, out bool hasDefaultConstructor)
        {
            hasDefaultConstructor = false;
            if (elements == null)
                return;

            bool hasConstructor = false;
            foreach (CodeElement element in elements)
            {
                if (element.Kind == vsCMElement.vsCMElementFunction)
                {
                    CodeFunction function = element as CodeFunction;
                    if (function != null && function.Access == vsCMAccess.vsCMAccessPublic)
                    {
                        var parameterCount = GetParameterCount(element);
                        var tokens = element.FullName.Split('.');
                        if (string.Equals(tokens[tokens.Length - 1], tokens[tokens.Length - 2], StringComparison.Ordinal))
                        {
                            hasConstructor = true;
                            if (parameterCount == 0)
                                hasDefaultConstructor = true;
                        }
                        else
                        {
                            if (parameterCount == 1 || parameterCount == 2)
                                classMethods.Add(element.Name);
                        }
                    }
                }
            }

            if (!hasDefaultConstructor && !hasConstructor)
                hasDefaultConstructor = true;
        }

        private static int GetParameterCount(CodeElement methodElement)
        {
            if (methodElement.Children == null)
                return 0;

            var parameters = 0;
            foreach (CodeElement element in methodElement.Children)
            {
                if (element.Kind == vsCMElement.vsCMElementParameter)
                {
                    parameters++;
                }
            }

            return parameters;
        }
    }
}
