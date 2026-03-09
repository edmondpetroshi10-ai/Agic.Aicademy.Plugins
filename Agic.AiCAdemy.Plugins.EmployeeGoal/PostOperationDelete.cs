using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text;

namespace Agic.AiCAdemy.Plugins.EmployeeGoal
{
    public class PostOperationDelete : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.Stage != 40 || context.MessageName != "Delete" || context.PrimaryEntityName != "agic_employeegoal")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Plug-in nuk eshte i sakte. Context.Mode: {0}; Context.Stage: {1}; Context.MessageName: {2}; Context.PrimaryEntityName: {3}",
                    context.Mode.ToString(), context.Stage.ToString(), context.MessageName, context.PrimaryEntityName);
                throw new InvalidPluginExecutionException(sb.ToString());
            }

            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            try
            {
                Entity preImage = (context.PreEntityImages != null && context.PreEntityImages.Contains("PreImage"))
                    ? context.PreEntityImages["PreImage"]
                    : null;

                if (preImage == null)
                    throw new InvalidPluginExecutionException("PreImage mungon ne Delete. Regjistro PreImage me kolonat e nevojshme.");

                EntityReference performanceRef = preImage.GetAttributeValue<EntityReference>("agic_employeeperformance");
                if (performanceRef == null || performanceRef.Id == Guid.Empty)
                    throw new Exception("invalid form");

                Rikalkulimidheperditesimi(service, performanceRef.Id);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Gabim gjate llogaritjes se Average Result (Delete).", ex);
            }
        }

        public void Rikalkulimidheperditesimi(IOrganizationService service, Guid performanceId)
        {
            QueryExpression qeueryGetGoal = new QueryExpression("agic_employeegoal")
            {
                ColumnSet = new ColumnSet("agic_weight", "agic_result"),
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