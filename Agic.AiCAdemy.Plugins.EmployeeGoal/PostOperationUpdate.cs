using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text;

namespace Agic.AiCAdemy.Plugins.EmployeeGoal
{
    public class PostOperationUpdate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.Stage != 40 || context.MessageName != "Update" || context.PrimaryEntityName != "agic_employeegoal")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Plug-in nuk eshte i sakte. Context.Mode: {0}; Context.Stage: {1}; Context.MessageName: {2}; Context.PrimaryEntityName: {3}",
                    context.Mode.ToString(), context.Stage.ToString(), context.MessageName, context.PrimaryEntityName);
                throw new InvalidPluginExecutionException(sb.ToString());
            }

            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            Entity targetMessageImage = (Entity)context.InputParameters["Target"];
            Entity preImage = context.PreEntityImages["PreImage"];

            try
            {
                tracingService.Trace("PostOperationUpdate plugin u ekzekutua");
                EntityReference employeeperformanceRef = targetMessageImage.GetAttributeValue<EntityReference>("agic_employeeperformance");
                if (employeeperformanceRef == null)
                   
                    employeeperformanceRef = preImage.GetAttributeValue<EntityReference>("agic_employeeperformance");

                if (employeeperformanceRef == null || employeeperformanceRef.Id == Guid.Empty)
                    return;

                Rikalkulimidheperditesimi(service, employeeperformanceRef.Id);
            }
            catch (Exception ex)
            {
                tracingService.Trace("ERROR EmployeeGoal_PostOperation_Update_Rikalkulimidheperditesimi: " + ex.ToString());
                throw new InvalidPluginExecutionException("Gabim gjate llogaritjes se Average Result (Update).", ex);
            }
        }

        public void Rikalkulimidheperditesimi(IOrganizationService service, Guid performanceId)
        {
            QueryExpression qeueryGetGoal = new QueryExpression("agic_employeegoal")
            {
                ColumnSet = new ColumnSet("agic_weight", "agic_weightedscore"),
                Criteria =
        {
            Conditions =
            {
                new ConditionExpression("agic_employeeperformance", ConditionOperator.Equal, performanceId)
            }
        }
            };

            EntityCollection allGoals = service.RetrieveMultiple(qeueryGetGoal);

            decimal sumWeights = 0m;
            decimal sumWeightedScores = 0m;

            foreach (var goal in allGoals.Entities)
            {
                decimal? weight = goal.GetAttributeValue<decimal?>("agic_weight");
                OptionSetValue resultOption = goal.GetAttributeValue<OptionSetValue>("agic_result");

                if (!weight.HasValue || weight.Value <= 0m || resultOption == null)
                    continue;

                int result = resultOption.Value;

                sumWeights += weight.Value;
                sumWeightedScores += result * weight.Value;
            }

            decimal averageResult = (sumWeights > 0m) ? (sumWeightedScores / sumWeights) : 0m;

            Entity performanceToUpdate = new Entity("agic_employeeperformance");
            performanceToUpdate.Id = performanceId;
            performanceToUpdate["agic_averageresult"] = averageResult;

            service.Update(performanceToUpdate);
        }
    }
}
