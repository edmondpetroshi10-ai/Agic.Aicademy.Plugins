using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text;

namespace Agic.AiCademy.Plugins.EmployeePerformance
{
    public class PostOperationCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.Stage != 40 || context.MessageName != "Create" || context.PrimaryEntityName != "agic_employeeperformance")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(
                    "Plug-in nuk eshte i sakte. Context.Mode: {0}; Context.Stage: {1}; Context.MessageName: {2}; Context.PrimaryEntityName: {3}",
                    context.Mode.ToString(), context.Stage.ToString(), context.MessageName, context.PrimaryEntityName
                );
                throw new InvalidPluginExecutionException(sb.ToString());
            }

            IOrganizationServiceFactory factory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity))
            {
                tracingService.Trace("Plugin1: Target mungon ose s’eshte Entity.");
                return;
            }

            Entity targetMessageImage = (Entity)context.InputParameters["Target"];

            Guid employeePerformanceId = targetMessageImage.Id;

            try
            {
                EntityReference employeeRef = targetMessageImage.GetAttributeValue<EntityReference>("agic_contactid");
                if (employeeRef == null)
                {
                    tracingService.Trace("Plugin1: Employee (agic_contactid) eshte bosh.");
                    return;//throw ex
                }

                //QueryExpression queryExisting = new QueryExpression("agic_employeegoal")
                //{
                //    ColumnSet = new ColumnSet("agic_employeegoalid"),
                //    Criteria =
                //    {
                //        Conditions =
                //        {
                //            new ConditionExpression("agic_employeeperformance", ConditionOperator.Equal, employeePerformanceId)
                //        }
                //    },
                //    TopCount = 1
                //};

                //EntityCollection existingGoals = service.RetrieveMultiple(queryExisting);
                //if (existingGoals != null && existingGoals.Entities.Count > 0)
                //{
                //    tracingService.Trace("Plugin1: Employee Goals ekzistojne tashme.");
                //    return;
                //}

                Entity employee = service.Retrieve("contact", employeeRef.Id, new ColumnSet("agic_professionid"));
                EntityReference professionRef = employee.GetAttributeValue<EntityReference>("agic_professionid");

                if (professionRef == null)
                {

                    throw new Exception("profesioni mungon");
                }

                QueryExpression queryProfessionGoals = new QueryExpression("agic_goalofprofession")
                {
                    ColumnSet = new ColumnSet("agic_goalid", "agic_weight"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new ConditionExpression("agic_professionid", ConditionOperator.Equal, professionRef.Id),
                            //new ConditionExpression("statecode", ConditionOperator.Equal, (int)ISACTIVE.yes)
                        }
                    }
                };
                LinkEntity goalLink = queryProfessionGoals.AddLink(
                          "agic_goal",
                         "agic_goalid",
                         "agic_goalid"
                );

                goalLink.LinkCriteria.AddCondition("agic_isactive", ConditionOperator.Equal, true);


                EntityCollection goalsOfProfession = service.RetrieveMultiple(queryProfessionGoals);
                if (goalsOfProfession == null || goalsOfProfession.Entities.Count == 0)
                {
                    throw new Exception("Nuk u gjet asnje GoalOfProfession.");
                }

                foreach (Entity gp in goalsOfProfession.Entities)
                {
                    EntityReference goalRef = gp.GetAttributeValue<EntityReference>("agic_goalid");
                    decimal? weight = gp.GetAttributeValue<decimal?>("agic_weight");

                    if (goalRef == null)
                        continue;

                    if (!weight.HasValue || weight.Value <= 0m)
                        continue;

                    Entity employeeGoal = new Entity("agic_employeegoal");

                    employeeGoal["agic_employeeperformance"] = new EntityReference("agic_employeeperformance", employeePerformanceId);

                    employeeGoal["agic_goal"] = goalRef;

                    employeeGoal["agic_weight"] = weight.Value;

                    service.Create(employeeGoal);
                }

                tracingService.Trace("Employee Goals u krijuan me sukses.");
            }
            catch (Exception ex)
            {
                tracingService.Trace("ERROR Plugin1: " + ex.ToString());
                throw new InvalidPluginExecutionException("Gabim gjate gjenerimit te Employee Goals.", ex);
            }
        }
    }
}