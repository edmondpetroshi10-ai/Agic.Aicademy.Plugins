using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text;

namespace Agic.AiCAdemy.Plugins.EmployeeGoal
{
    public class PostOperationCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.Stage != 40 || context.MessageName != "Create" || context.PrimaryEntityName != "agic_employeegoal")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(
                    "Plug-in nuk eshte i sakte. Context.Mode: {0}; Context.Stage: {1}; Context.MessageName: {2}; Context.PrimaryEntityName: {3}",
                    context.Mode.ToString(), context.Stage.ToString(), context.MessageName, context.PrimaryEntityName);
                throw new InvalidPluginExecutionException(sb.ToString());
            }

          

            Entity targetMessageImage = (Entity)context.InputParameters["Target"];

            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);


            try
            {
                //EntityReference goalRef = targetMessageImage.GetAttributeValue<EntityReference>("agic_goal");

                //Entity goal = service.Retrieve( "agic_goal",goalRef.Id, new ColumnSet("agic_weight") );

                EntityReference employeeperfomanceRef = targetMessageImage.GetAttributeValue<EntityReference>("agic_employeeperformance");

                if (employeeperfomanceRef == null)
                    throw new InvalidPluginExecutionException("employee performance nuk mund te jete null ");
                

                QueryExpression query = new QueryExpression("agic_employeegoal")
                {
                    ColumnSet = new ColumnSet("agic_weight", "agic_result", "agic_weightedscore"),
                    Criteria =
                           {
                            Conditions =
                                {
                                  new ConditionExpression("agic_employeeperformance", ConditionOperator.Equal, employeeperfomanceRef.Id),
                                  new ConditionExpression("agic_weight",ConditionOperator.GreaterThan,0),
                                  new ConditionExpression("agic_weightedscore",ConditionOperator.NotNull)
                                }
                            }
                };

                EntityCollection goals = service.RetrieveMultiple(query);

                decimal sumWeightedScore = 0m;
                decimal sumWeight = 0m;

                foreach (Entity goalent in goals.Entities)
                {
                    decimal weight = goalent.GetAttributeValue<decimal>("agic_weight");

                    decimal weightScore = goalent.GetAttributeValue<decimal>("agic_weightedscore");
                    sumWeightedScore += weightScore;
                    sumWeight += weight;
                }
                decimal averageResult = sumWeightedScore / sumWeight;

                Entity performance = new Entity("agic_employeeperformance");
                performance.Id = employeeperfomanceRef.Id;
                performance["agic_averageresult"] = averageResult;

                service.Update(performance);
            }
            catch (Exception ex)
            {
                tracingService.Trace("Error calculating average result: " + ex.ToString());
                throw new InvalidPluginExecutionException("Gabim gjate llogaritjes se Average Result.", ex);
            }
        }
    }
}