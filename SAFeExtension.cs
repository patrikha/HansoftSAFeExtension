using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using HPMSdk;
using Hansoft.ObjectWrapper;

namespace Hansoft.Jean.Behavior.DeriveBehavior.Expressions
{
    public class SAFeExtension
    {
        /// <summary>
        /// Creates a feature summary suitable to display for each epic in a SAFe portfolio project where
        /// the associated features are linked to the epic they belong to.
        /// </summary>
        /// <param name="current_task">The item representing an epic in the portfolio project.</param>
        /// <returns></returns>
        public static string FeatureSummary(Task current_task)
        {

            StringBuilder sb = new StringBuilder();
            List<Task> featuresInDevelopment = new List<Task>();
            List<Task> featuresInReleasePlanning = new List<Task>();
            List<Task> featuresInBacklog = new List<Task>();
            foreach (Task task in current_task.LinkedTasks)
            {
                if (task.Project != current_task.Project)
                {
                    if (task.Parent.Name == "Development")
                    {
                        featuresInDevelopment.Add(task);
                    }
                    else if (task.Parent.Name == "Release planning")
                    {
                        featuresInReleasePlanning.Add(task);
                    }
                    else if (task.Parent.Name == "Feature backlog")
                    {
                        featuresInBacklog.Add(task);
                    }
                }
            }

            sb.Append(string.Format("<BOLD>Development ({0})</BOLD>", featuresInDevelopment.Count));
            sb.Append('\n');
            if (featuresInDevelopment.Count > 0)
            {
                string format = "<CODE>{0,-20} │ {1,-13} │ {2, 8} │ {3, 4} │ {4, 10} │ {5, -20}</CODE>";
                sb.Append(string.Format(format, new object[] { "Name", "Status", "Done", "↓14", "Est. Done", "Product Owner"}));
                sb.Append('\n');
                sb.Append("<CODE>─────────────────────┼───────────────┼──────────┼──────┼────────────┼─────────────────────</CODE>");
                sb.Append('\n');
                foreach (Task task in featuresInDevelopment)
                {
                    string daysDone = task.GetCustomColumnValue("Days completed") + "/" + task.AggregatedEstimatedDays;
                    sb.Append(string.Format(format, new object[] { task.Name, task.AggregatedStatus, daysDone, task.GetCustomColumnValue("Velocity (14 days)"), task.GetCustomColumnValue("Estimated done"), task.GetCustomColumnValue("Product Owner")}));
                    sb.Append('\n');
                }
            }
            sb.Append('\n');

            sb.Append(string.Format("<BOLD>Release planning ({0})</BOLD>", featuresInReleasePlanning.Count));
            sb.Append('\n');
            if (featuresInReleasePlanning.Count > 0)
            {
                string format = "<CODE>{0,-20} │ {1, 5} │ {2, -20} │ {3, -20}</CODE>";
                sb.Append(string.Format(format, new object[] { "Name", "Est.", "Team", "Product Owner" }));
                sb.Append('\n');
                sb.Append("<CODE>─────────────────────┼───────┼──────────────────────┼─────────────────────</CODE>");
                sb.Append('\n');
                foreach (Task task in featuresInReleasePlanning)
                {
                    sb.Append(string.Format(format, new object[] { task.Name, task.AggregatedEstimatedDays, task.GetCustomColumnValue("Team"), task.GetCustomColumnValue("Product Owner")}));
                    sb.Append('\n');
                }
            }
            sb.Append('\n');

            sb.Append(string.Format("<BOLD>Feature backlog ({0})</BOLD>", featuresInBacklog.Count));
            sb.Append('\n'); 
            if (featuresInBacklog.Count > 0)
            {
                string format = "<CODE>{0,-20} │ {1, 5} │ {2, 8}";
                sb.Append(string.Format(format, new object[] { "Name", "Est.", "PSI" }));
                sb.Append('\n');
                sb.Append("─────────────────────┼───────┼─────────</CODE>");
                sb.Append('\n');
                foreach (Task task in featuresInBacklog)
                {
                    sb.Append(string.Format(format, new object[] { task.Name, task.AggregatedEstimatedDays, ListUtils.ToString(new List<HansoftItem>(task.TaggedToReleases)) }));
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }
    }


}
