using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Agic.AiCAdemy.Plugins.EmployeeGoal
{
    public class PreOperationUpdate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.Stage != 20 || context.MessageName != "Update" || context.PrimaryEntityName != "agic_employeegoal")
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

                bool hasResultChanged = targetMessageImage.Attributes.Contains("agic_result");
                bool hasWeightChanged = targetMessageImage.Attributes.Contains("agic_weight");
                if (!hasResultChanged && !hasWeightChanged)
                {
                    tracingService.Trace("Asnjera nga fushat nuk ka ndryshuar");
                    return;
                }
                decimal weight =
                targetMessageImage.Contains("agic_weight") ? targetMessageImage.GetAttributeValue<decimal>("agic_weight")
                : preImage != null ? preImage.GetAttributeValue<decimal>("agic_weight")
                : 0m;
                int result =
                                targetMessageImage.Contains("agic_result") ? (targetMessageImage.GetAttributeValue<OptionSetValue>("agic_result")?.Value ?? 0)
                                : preImage != null ? (preImage.GetAttributeValue<OptionSetValue>("agic_result")?.Value ?? 0)
                                : 0;

                if (result <= 0 || weight <= 0)
                {
                    tracingService.Trace("Result ose Weight mungon ne Create. Weighted Score nuk u llogarit.");
                    return;
                }

                decimal weightedScore = result * weight;

                targetMessageImage["agic_weightedscore"] = weightedScore;
            }
            catch (Exception ex)
            {
                tracingService.Trace("ERROR PreOperationUpdate WeightedScore: " + ex.ToString());
                throw new InvalidPluginExecutionException("Gabim gjate llogaritjes se Weighted Score (Update).", ex);
            }
        }
    }
}




