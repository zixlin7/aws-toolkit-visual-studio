using System;
using System.Collections.Generic;
using Amazon.IdentityManagement;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using log4net;
using ThirdParty.Json.LitJson;
using System.Text.RegularExpressions;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.ECS.WizardPages
{
    public static class ECSWizardUtils
    {
        internal const string CREATE_NEW_TEXT = "Create New";
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ECSWizardUtils));

        public const string DEFAULT_ECS_TASK_EXECUTION_ROLE = "ecsTaskExecutionRole";
        public const string PERSISTED_DEPLOYMENT_MODE = "vstoolkit-deployment-mode";

        /// <summary>
        /// Creates a service client based on the host wizard's account and region properties.
        /// </summary>
        /// <typeparam name="T">Type of client to create</typeparam>
        /// <param name="hostWizard">host wizard - expected to contain account and region data</param>
        public static T CreateServiceClient<T>(IAWSWizard hostWizard) where T : class, IAmazonService
        {
            var account = hostWizard.GetSelectedAccount(PublishContainerToAWSWizardProperties.UserAccount);
            var region = hostWizard.GetSelectedRegion(PublishContainerToAWSWizardProperties.Region);

            if (account == null)
            {
                throw new Exception("Wizard did not have account defined");
            }

            if (region == null)
            {
                throw new Exception("Wizard did not have region defined");
            }

            return account.CreateServiceClient<T>(region);
        }

        public static List<Role> LoadECSRoles(IAWSWizard hostWizard)
        {
            using (var client = CreateServiceClient<AmazonIdentityManagementServiceClient>(hostWizard))
            {
                var roles = new List<Role>();
                var response = new ListRolesResponse();
                do
                {
                    var request = new ListRolesRequest() { Marker = response.Marker };
                    response = client.ListRoles(request);

                    var validRoles = RolePolicyFilter.FilterByAssumeRoleServicePrincipal(response.Roles, "ecs.amazonaws.com");
                    foreach (var role in validRoles)
                    {
                        roles.Add(role);
                    }
                } while (!string.IsNullOrEmpty(response.Marker));
                return roles;
            }
        }

        public static bool IsFargateLaunch(this IAWSWizard hostWizard)
        {
            var launchType = hostWizard[PublishContainerToAWSWizardProperties.LaunchType] as string;
            return string.Equals(launchType, Amazon.ECS.LaunchType.FARGATE, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsEc2Launch(this IAWSWizard hostWizard)
        {
            var launchType = hostWizard[PublishContainerToAWSWizardProperties.LaunchType] as string;
            return string.Equals(launchType, Amazon.ECS.LaunchType.EC2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryParseRateExpression(this IAWSWizard hostWizard, out int value, out string unit)
        {
            // https://docs.aws.amazon.com/eventbridge/latest/userguide/eb-create-rule-schedule.html
            if (hostWizard.TryGetProperty(PublishContainerToAWSWizardProperties.ScheduleExpression, out string expr) && expr?.StartsWith("rate(") == true)
            {
                Match match = Regex.Match(expr, @"^rate\((\d+) (minute(?:s)?|hour(?:s)?|day(?:s)?)\)$", RegexOptions.Singleline, TimeSpan.FromSeconds(5 /* Use a sane timeout */));
                if (match.Groups.Count == 3 && int.TryParse(match.Groups[1].Value, out value))
                {
                    // To validate, would likely want to promote out and refactor expression parts of RunIntervalUnitItem in ScheduleTaskPage.xaml.cs
                    unit = match.Groups[2].Value;
                    return true;
                }
            }

            value = 0;
            unit = null;
            return false;
        }

        public static bool TryGetCronExpression(this IAWSWizard hostWizard, out string cronExpr)
        {
            // https://docs.aws.amazon.com/eventbridge/latest/userguide/eb-create-rule-schedule.html
            if (hostWizard.TryGetProperty(PublishContainerToAWSWizardProperties.ScheduleExpression, out string expr) && expr?.StartsWith("cron(") == true)
            {
                cronExpr = expr;
                return true;
            }

            cronExpr = null;
            return false;
        }

        static IList<TaskCPUItemValue> _taskCPUAllowedValues;

        public static IList<TaskCPUItemValue> TaskCPUAllowedValues
        {
            get
            {
                if(_taskCPUAllowedValues == null)
                {
                    var collection = new List<TaskCPUItemValue>();
                    try
                    {
                        var content = S3FileFetcher.Instance.GetFileContent("ServiceMeta/ECSServiceMeta.json");
                        var rootData = JsonMapper.ToObject(content);
                        var allowedValues = rootData["FargateAllowedCPUSettings"];
                        foreach(JsonData cpuItem in allowedValues)
                        {
                            var c = new TaskCPUItemValue
                            {
                                DisplayName = cpuItem["DisplayName"].ToString(),
                                SystemName = cpuItem["SystemName"].ToString()
                            };

                            c.MemoryOptions = new List<MemoryOption>();
                            foreach(JsonData memoryItem in cpuItem["MemoryValues"])
                            {
                                var m = new MemoryOption
                                {
                                    DisplayName = memoryItem["DisplayName"].ToString(),
                                    SystemName = memoryItem["SystemName"].ToString()
                                };
                                c.MemoryOptions.Add(m);
                            }

                            collection.Add(c);
                        }

                        _taskCPUAllowedValues = collection;

                    }
                    catch(Exception e)
                    {
                        LOGGER.Error("Error loading Fargate allowed CPU values", e);
                    }
                }

                return _taskCPUAllowedValues;
            }
        }

        public class TaskCPUItemValue
        {
            public string DisplayName { get; set; }
            public string SystemName { get; set; }
            public IList<MemoryOption> MemoryOptions { get; set; }
        }

        public class MemoryOption
        {
            public string DisplayName { get; set; }
            public string SystemName { get; set; }
        }


        public class PlacementTemplates
        {
            public static readonly PlacementTemplates[] Options = new PlacementTemplates[]
            {
                new PlacementTemplates
                {
                    DisplayName = "AZ Balanced Spread",
                    Description = "This template will spread tasks across availability zones and within the availability zone spread tasks across instances.",
                    PlacementStrategy = new string[]{ "spread=attribute:ecs.availability-zone", "spread=instanceId" }
                },
                new PlacementTemplates
                {
                    DisplayName = "AZ Balanced BinPack",
                    Description = "This template will spread tasks across availability zones and within the availability zone pack tasks on least number of instances by memory.",
                    PlacementStrategy = new string[]{ "spread=attribute:ecs.availability-zone", "binpack=memory" }
                },
                new PlacementTemplates
                {
                    DisplayName = "BinPack",
                    Description = "This template will pack tasks least number of instances by memory.",
                    PlacementStrategy = new string[]{ "binpack=memory" }
                },
                new PlacementTemplates
                {
                    DisplayName = "One Task Per Host",
                    Description = "This template will place only one task per instance.",
                    PlacementConstraints = new string[]{ "distinctInstance" }
                }
            };

            public string DisplayName { get; set; }
            public string Description { get; set; }

            public string[] PlacementConstraints { get; set; }
            public string[] PlacementStrategy { get; set; }
        }

    }
}
