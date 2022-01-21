// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using OperatingSystem = Microsoft.Azure.Management.AppService.Fluent.OperatingSystem;

namespace ConsoleApp1
{
	public class Program
	{
		/**
         * This sample shows how to programmatically implement scenario described <a href="https://docs.microsoft.com/en-us/azure/monitoring-and-diagnostics/monitor-tutorial-autoscale-performance-schedule">here</a>.
         *  - Create a Web App and App Service Plan
         *  - Configure autoscale rules for scale-in and scale out based on the number of requests a Web App receives
         *  - Trigger a scale-out action and watch the number of instances increase
         *  - Trigger a scale-in action and watch the number of instances decrease
         *  - Clean up your resources
         */
		public static void RunSample(IAzure azure)
		{
			string planName = SdkContext.RandomResourceName("plan", 25);
			string appName = SdkContext.RandomResourceName("MyTestScaleWebApp", 25);
			string rgName = SdkContext.RandomResourceName("rgMonitor", 15);

			try
			{
				// ============================================================
				// Create a Web App and App Service Plan
				Utilities.Log("Creating a web app and service plan");

				Utilities.Log("Creating or updating App Service plan...");
				var plan = azure.AppServices.AppServicePlans.Define(planName)
					.WithRegion(Region.EuropeWest)
					.WithNewResourceGroup(rgName)
					.WithPricingTier(PricingTier.PremiumP2v3)
					.WithOperatingSystem(OperatingSystem.Linux)
					.Create();

				Utilities.Log($"Creating or updating App Service '{appName}'...");
				var appService = azure.AppServices.WebApps.Define(appName)
					.WithExistingLinuxPlan(plan)
					.WithExistingResourceGroup(rgName)
					.WithBuiltInImage(new RuntimeStack("DOTNETCORE", "5.0"))
					.WithClientAffinityEnabled(false)
					.WithHttpsOnly(false)
					.WithWebAppAlwaysOn(true)
					.WithSystemAssignedManagedServiceIdentity()
					.Create();

				Utilities.Log("Created a web app:");
				Utilities.Print(appService);

			Utilities.Log($"Applying autoscale settings...");
			var scaleSettings = azure.AutoscaleSettings.Define("cde-autoscale")
			   .WithRegion(Region.EuropeWest)
			   .WithExistingResourceGroup(rgName)
			   .WithTargetResource(plan.Id);

			scaleSettings.DefineAutoscaleProfile("metrics-scale")
			   .WithMetricBasedScale(7, 30, 12)
			   .DefineScaleRule()
			   .WithMetricSource(plan.Id)
			   .WithMetricName("CpuPercentage")
			   .WithStatistic(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2), MetricStatisticType.Average)
			   .WithCondition(TimeAggregationType.Average, ComparisonOperationType.GreaterThan, 20)
			   .WithScaleAction(ScaleDirection.Increase, ScaleType.ChangeCount, 5, TimeSpan.FromMinutes(5))
			   .Attach()
			   .DefineScaleRule()
			   .WithMetricSource(plan.Id)
			   .WithMetricName("CpuPercentage")
			   .WithStatistic(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(2), MetricStatisticType.Average)
			   .WithCondition(TimeAggregationType.Average, ComparisonOperationType.GreaterThan, 16)
			   .WithScaleAction(ScaleDirection.Increase, ScaleType.ChangeCount, 1, TimeSpan.FromMinutes(2))
			   .Attach()
			   .DefineScaleRule()
			   .WithMetricSource(plan.Id)
			   .WithMetricName("CpuPercentage")
			   .WithStatistic(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(2), MetricStatisticType.Average)
			   .WithCondition(TimeAggregationType.Average, ComparisonOperationType.LessThan, 13)
			   .WithScaleAction(ScaleDirection.Decrease, ScaleType.ChangeCount, 1, TimeSpan.FromMinutes(2))
			   .Attach()
			   .Attach()
			   .CreateAsync();


				var deployedWebAppUrl = "https://" + appService.HostNames.First() + "/";
				// Trigger scale-out action
				for (int i = 0; i < 11; i++)
				{
					SdkContext.DelayProvider.Delay(5000);
					Utilities.CheckAddress(deployedWebAppUrl);
				}

				// Now you can browse the history of autoscale form the azure portal
				// 1. Open the App Service Plan.
				// 2. From the left-hand navigation pane, select the Monitor option. Once the page loads select the Autoscale tab.
				// 3. From the list, select the App Service Plan used throughout this tutorial.
				// 4. On the autoscale setting, click the Run history tab.
				// 5. You see a chart reflecting the instance count of the App Service Plan over time.
				// 6. In a few minutes, the instance count should rise from 1, to 2.
				// 7. Under the chart, you see the activity log entries for each scale action taken by this autoscale setting.
			}
			finally
			{
				// if (azure.ResourceGroups.GetByName(rgName) != null)
				// {
				// 	Utilities.Log("Deleting Resource Group: " + rgName);
				// 	azure.ResourceGroups.BeginDeleteByName(rgName);
				// 	Utilities.Log("Deleted Resource Group: " + rgName);
				// }
				// else
				// {
				// 	Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
				// }
			}
		}

		public static void Main(string[] args)
		{
			try
			{
				var azure = AzureAuth.Authenticate().WithDefaultSubscription();

				// Print selected subscription
				Utilities.Log("Selected subscription: " + azure.SubscriptionId);

				RunSample(azure);
			}
			catch (Exception ex)
			{
				Utilities.Log(ex);
			}
		}
	}
}
