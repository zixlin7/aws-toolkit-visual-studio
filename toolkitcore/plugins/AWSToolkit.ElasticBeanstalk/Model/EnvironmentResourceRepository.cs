using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancingV2;

using log4net;

using DescribeLoadBalancersRequest = Amazon.ElasticLoadBalancingV2.Model.DescribeLoadBalancersRequest;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class EnvironmentResourceRepository
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(EnvironmentResourceRepository));

        private readonly ToolkitContext _toolkitContext;

        private readonly IAmazonElasticBeanstalk _beanstalkClient;
        private readonly IAmazonCloudWatch _cwClient;
        private readonly IAmazonEC2 _ec2Client;
        private readonly IAmazonElasticLoadBalancing _elbClient;
        private readonly IAmazonElasticLoadBalancingV2 _elbV2Client;
        private readonly IAmazonAutoScaling _asClient;

        public EnvironmentResourceRepository(AwsConnectionSettings connectionSettings, ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;

            _beanstalkClient =
                _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonElasticBeanstalkClient>(
                    connectionSettings);
            _cwClient =
                _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonCloudWatchClient>(connectionSettings);
            _ec2Client = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(connectionSettings);
            _elbClient =
                _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonElasticLoadBalancingClient>(
                    connectionSettings);
            _elbV2Client =
                _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonElasticLoadBalancingV2Client>(
                    connectionSettings);
            _asClient =
                _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonAutoScalingClient>(connectionSettings);
        }

        public async Task<EnvironmentResources> LoadEnvironmentResourcesAsync(string environmentId)
        {
            var environmentResources = new EnvironmentResources();
            var resources = GetEnvironmentResourceDescription(environmentId);

            var loadingTasks = new List<Task>()
            {
                LoadInstancesAsync(resources.Instances, environmentResources),
                LoadLoadBalancersAsync(resources.LoadBalancers, environmentResources),
                LoadAutoScalingGroupsAsync(resources.LaunchConfigurations, resources.AutoScalingGroups,
                    environmentResources),
                LoadAlarmsAsync(resources.Triggers, environmentResources),
            };

            await Task.WhenAll(loadingTasks);

            return environmentResources;
        }

        public EnvironmentResourceDescription GetEnvironmentResourceDescription(string environmentId)
        {
            var response = _beanstalkClient.DescribeEnvironmentResources(
                new DescribeEnvironmentResourcesRequest() { EnvironmentId = environmentId });
            return response.EnvironmentResources;
        }

        private async Task LoadInstancesAsync(List<Amazon.ElasticBeanstalk.Model.Instance> instances,
            EnvironmentResources environmentResources)
        {
            try
            {
                var instanceIds = instances.Select(i => i.Id).ToList();
                environmentResources.Instances = (await DescribeInstancesAsync(instanceIds))
                    .OrderBy(i => i.Id)
                    .ToList();
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.OutputToHostConsole(
                    $"Unable to load EC2 Instance details for environment: {e.Message}", true);
                _logger.Error("Error describing instances", e);
                environmentResources.Instances = new List<InstanceWrapper>();
            }
        }

        public async Task<IEnumerable<InstanceWrapper>> DescribeInstancesAsync(ICollection<string> instanceIds)
        {
            var instances = new List<InstanceWrapper>();

            if (instanceIds.Count == 0)
            {
                return instances;
            }

            var response = await _ec2Client.DescribeInstancesAsync(new DescribeInstancesRequest()
            {
                InstanceIds = instanceIds.ToList()
            });

            instances.AddRange(response.Reservations.SelectMany(r =>
                r.Instances.Select(i => new InstanceWrapper(i))));

            return instances;
        }

        private async Task LoadLoadBalancersAsync(List<Amazon.ElasticBeanstalk.Model.LoadBalancer> loadBalancers,
            EnvironmentResources environmentResources)
        {
            try
            {
                var lbNames = loadBalancers.Select(lb => lb.Name).ToList();

                environmentResources.LoadBalancers = (await DescribeLoadBalancersAsync(lbNames))
                    .OrderBy(lb => lb.Name)
                    .ToList();
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.OutputToHostConsole(
                    $"Unable to get load balancer details for environment: {e.Message}", true);
                _logger.Error("Error describing load balancers", e);
                environmentResources.LoadBalancers = new List<LoadBalancer>();
            }
        }

        public async Task<IEnumerable<LoadBalancer>> DescribeLoadBalancersAsync(ICollection<string> loadBalancerNames)
        {
            if (!loadBalancerNames.Any())
            {
                return Enumerable.Empty<LoadBalancer>();
            }

            // The type of load balancer affects which call we can query for details.
            // Try getting details from V2, falling back to V1 if needed.
            try
            {
                return await DescribeLoadBalancersV2Async(loadBalancerNames);
            }
            catch (Exception)
            {
                // Swallow the error and proceed to try V1
            }

            return await DescribeLoadBalancersV1Async(loadBalancerNames);
        }

        private async Task<IEnumerable<LoadBalancer>> DescribeLoadBalancersV1Async(
            ICollection<string> loadBalancerNames)
        {
            var loadBalancers = new List<LoadBalancer>();

            if (!loadBalancerNames.Any())
            {
                return loadBalancers;
            }

            var response = await _elbClient.DescribeLoadBalancersAsync(
                new ElasticLoadBalancing.Model.DescribeLoadBalancersRequest()
                {
                    LoadBalancerNames = loadBalancerNames.ToList(),
                });

            loadBalancers.AddRange(response.LoadBalancerDescriptions.Select(AsLoadBalancer));

            return loadBalancers;
        }

        private LoadBalancer AsLoadBalancer(ElasticLoadBalancing.Model.LoadBalancerDescription loadBalancer)
        {
            var model = new LoadBalancer()
            {
                DNSName = loadBalancer.DNSName,
                HostedZoneNameId = loadBalancer.CanonicalHostedZoneNameID,
                Name = loadBalancer.LoadBalancerName,
            };

            model.AvailabilityZones.AddRange(loadBalancer.AvailabilityZones.Select(az => new AvailabilityZone()
            {
                ZoneId = az,
            }));

            return model;
        }

        private async Task<IEnumerable<LoadBalancer>> DescribeLoadBalancersV2Async(ICollection<string> loadBalancerArns)
        {
            var loadBalancers = new List<LoadBalancer>();

            if (!loadBalancerArns.Any())
            {
                return loadBalancers;
            }

            var response = await _elbV2Client.DescribeLoadBalancersAsync(new DescribeLoadBalancersRequest()
            {
                LoadBalancerArns = loadBalancerArns.ToList(),
            });

            loadBalancers.AddRange(response.LoadBalancers.Select(AsLoadBalancer));

            return loadBalancers;
        }

        private LoadBalancer AsLoadBalancer(ElasticLoadBalancingV2.Model.LoadBalancer loadBalancer)
        {
            var model = new LoadBalancer()
            {
                DNSName = loadBalancer.DNSName,
                HostedZoneNameId = loadBalancer.CanonicalHostedZoneId,
                Name = loadBalancer.LoadBalancerName,
            };

            model.AvailabilityZones.AddRange(loadBalancer.AvailabilityZones.Select(AsAvailabilityZone));
            return model;
        }

        private AvailabilityZone AsAvailabilityZone(ElasticLoadBalancingV2.Model.AvailabilityZone availabilityZone)
        {
            return new AvailabilityZone() { ZoneId = availabilityZone.ZoneName, };
        }

        private async Task LoadAutoScalingGroupsAsync(
            List<Amazon.ElasticBeanstalk.Model.LaunchConfiguration> launchConfigurations,
            List<Amazon.ElasticBeanstalk.Model.AutoScalingGroup> autoScalingGroups,
            EnvironmentResources environmentResources)
        {
            try
            {
                var launchConfigurationNames = launchConfigurations.Select(lc => lc.Name).ToList();
                var autoScalingGroupNames = autoScalingGroups.Select(asg => asg.Name).ToList();

                environmentResources.AutoScalingGroups =
                    (await DescribeAutoScalingGroupsAsync(launchConfigurationNames, autoScalingGroupNames))
                    .ToList();
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.OutputToHostConsole(
                    $"Unable to load auto scaling group details for environment: {e.Message}", true);
                _logger.Error("Error describing auto scaling groups", e);

                environmentResources.AutoScalingGroups = new List<AutoScalingGroupWrapper>();
            }
        }

        public async Task<IEnumerable<AutoScalingGroupWrapper>> DescribeAutoScalingGroupsAsync(
            ICollection<string> launchConfigurationNames,
            ICollection<string> autoScalingGroupNames)
        {
            var autoScalingGroupWrappers = new List<AutoScalingGroupWrapper>();

            var loadLaunchConfigTask = DescribeLaunchConfigurationsAsync(launchConfigurationNames);
            var loadAutoScalingTask = DescribeAutoScalingGroupsAsync(autoScalingGroupNames);

            await Task.WhenAll(loadLaunchConfigTask, loadAutoScalingTask);

            var launchConfigurations = (await loadLaunchConfigTask).ToList();
            var autoScalingGroups = await loadAutoScalingTask;

            autoScalingGroupWrappers.AddRange(
                autoScalingGroups.Select(asg =>
                    {
                        var lc = launchConfigurations.FirstOrDefault(x =>
                            x.LaunchConfigurationName == asg.LaunchConfigurationName);
                        return lc == null ? null : new AutoScalingGroupWrapper(asg, lc);
                    })
                    .Where(x => x != null)
                    .OrderBy(x => x.Name)
            );

            return autoScalingGroupWrappers;
        }

        public async Task<IEnumerable<AutoScaling.Model.LaunchConfiguration>> DescribeLaunchConfigurationsAsync(
            ICollection<string> launchConfigurationNames)
        {
            var launchConfigurations = new List<AutoScaling.Model.LaunchConfiguration>();

            if (launchConfigurationNames.Count == 0)
            {
                return launchConfigurations;
            }

            var request = new DescribeLaunchConfigurationsRequest
            {
                LaunchConfigurationNames = launchConfigurationNames.ToList(),
            };

            do
            {
                var response = await _asClient.DescribeLaunchConfigurationsAsync(request);

                launchConfigurations.AddRange(response.LaunchConfigurations);
                request.NextToken = request.NextToken;
            } while (!string.IsNullOrEmpty(request.NextToken));

            return launchConfigurations;
        }

        public async Task<IEnumerable<AutoScaling.Model.AutoScalingGroup>> DescribeAutoScalingGroupsAsync(
            ICollection<string> autoScalingGroupNames)
        {
            var autoScalingGroups = new List<AutoScaling.Model.AutoScalingGroup>();

            if (autoScalingGroupNames.Count == 0)
            {
                return autoScalingGroups;
            }

            var request = new DescribeAutoScalingGroupsRequest
            {
                AutoScalingGroupNames = autoScalingGroupNames.ToList(),
            };

            do
            {
                var response = await _asClient.DescribeAutoScalingGroupsAsync(request);

                autoScalingGroups.AddRange(response.AutoScalingGroups);
                request.NextToken = response.NextToken;
            } while (!string.IsNullOrEmpty(request.NextToken));

            return autoScalingGroups;
        }

        private async Task LoadAlarmsAsync(List<Trigger> triggers, EnvironmentResources environmentResources)
        {
            try
            {
                var alarmNames = new List<string>();
                foreach (var t in triggers)
                {
                    alarmNames.Add($"{t.Name}{"-lower"}");
                    alarmNames.Add($"{t.Name}{"-upper"}");
                }

                environmentResources.Alarms = (await DescribeAlarmsAsync(alarmNames))
                    .OrderBy(a => a.AlarmName)
                    .ToList();
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.OutputToHostConsole(
                    $"Unable to load alarms for environment: {e.Message}", true);
                _logger.Error("Error describing alarms", e);

                environmentResources.Alarms = new List<MetricAlarmWrapper>();
            }
        }

        public async Task<IEnumerable<MetricAlarmWrapper>> DescribeAlarmsAsync(ICollection<string> alarmNames)
        {
            var metricAlarms = new List<MetricAlarmWrapper>();

            if (alarmNames.Count == 0)
            {
                return metricAlarms;
            }

            var request = new DescribeAlarmsRequest() { AlarmNames = alarmNames.ToList() };

            do
            {
                var response = await _cwClient.DescribeAlarmsAsync(request);

                metricAlarms.AddRange(response.MetricAlarms.Select(alarm => new MetricAlarmWrapper(alarm)));
                request.NextToken = response.NextToken;
            } while (!string.IsNullOrEmpty(request.NextToken));

            return metricAlarms;
        }
    }
}
