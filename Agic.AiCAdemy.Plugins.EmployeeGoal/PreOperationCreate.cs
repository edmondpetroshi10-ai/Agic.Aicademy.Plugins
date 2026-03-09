using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace Agic.AiCAdemy.Plugins.EmployeeGoal
{
    public class PreOperationCreate : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (context.Stage != 20 || context.MessageName != "Create" || context.PrimaryEntityName != "agic_employeegoal")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Plug-in nuk eshte i sakte. Context.Mode: {0}; Context.Stage: {1}; Context.MessageName: {2}; Context.PrimaryEntityName: {3}",
                    context.Mode.ToString(), context.Stage.ToString(), context.MessageName, context.PrimaryEntityName);
                throw new InvalidPluginExecutionException(sb.ToString());
            }

            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);
            Entity targetMessageImage = (Entity)context.InputParameters["Target"];
            try
            {

                if (!context.InputParameters.Contains("Target"))
                    return;
                var resultOptions = targetMessageImage.GetAttributeValue<OptionSetValue>("agic_result");

                decimal? weight = targetMessageImage.GetAttributeValue<decimal?>("agic_weight");
                int result = resultOptions?.Value ?? 0;

                if (result <= 0 || !weight.HasValue)
                {
                    tracingService.Trace("Result ose Weight mungon ne Create. Weighted Score nuk u llogarit.");
                    return;
                }

                decimal weightedScore = result * weight.Value;

                targetMessageImage["agic_weightedscore"] = weightedScore;


            }
            catch (Exception ex)
            {
                tracingService.Trace("ERROR PreOperationCreate_CalculateWeightedScore: " + ex.ToString());


                throw new InvalidPluginExecutionException(
                    "Gabim gjate llogaritjes se Weighted Score (Create).", ex);
            }
        }
    }

}