using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using HPMSdk;
using Hansoft.ObjectWrapper;
using Hansoft.ObjectWrapper.CustomColumnValues;
using System.Collections;

namespace SE.HansoftExtensions
{

    public class SAFeExtension
    {
        public static bool debug = false;

        public static HPMProjectCustomColumnsColumn trackingColumn = null;

        public static bool findSprintTaskID(Task parentTask)
        {
            foreach(Task t in parentTask.Project.ScheduledItems){
                //Console.WriteLine(t.Name);
                
                if (parentTask.Name == t.Name) {
                    //Console.WriteLine("--" + t.ProjectView.Name);
                    //Console.WriteLine("__" + t.Project.Name);
                    //Console.WriteLine("MATCH");
                    return true;

                }
            }
            return false;
        }

        private static string getMilestoneString(Task task) {
            List<HansoftItem> taggs = new List<HansoftItem>(task.TaggedToReleases);

            string milestoneString = ListUtils.ToString(taggs);
            if (taggs.Count > 1 && milestoneString.Length > 28)
            {
                milestoneString = taggs.Count() + " milestones";
            }
            return milestoneString.Substring(0, (milestoneString.Length > 28) ? 27 : milestoneString.Length) + ((milestoneString.Length > 28) ? "…" : "");
        }

        /// <summary>
        /// Creates a feature summary suitable to display for each epic in a SAFe portfolio project where
        /// the associated features are linked to the epic they belong to.
        /// </summary>
        /// <param name="current_task">The item representing an epic in the portfolio project.</param>
        /// <returns></returns>
        public static string FeatureSummary(Task current_task, bool usePoints, string completedColumn)
        {
            StringBuilder sb = new StringBuilder();
            List<Task> featuresInDevelopment = new List<Task>();
            List<Task> featuresInBacklog = new List<Task>();
            foreach (Task task in current_task.LinkedTasks)
            {
                if (task.Project != current_task.Project)
                {
                    if (findSprintTaskID(task))
                    {
                        featuresInDevelopment.Add(task);
                    }
                    else
                    {
                        featuresInBacklog.Add(task);
                    }
                }
            }

            sb.Append(string.Format("<BOLD>Development ({0})</BOLD>", featuresInDevelopment.Count));
            sb.Append('\n');
            if (featuresInDevelopment.Count > 0)
            {
                string format = "<CODE>{0, -20} │ {1, -13} │ {2, 8} │ {3, 1}</CODE>";
                sb.Append(string.Format(format, new object[] { "Name", "Status", "Done", "Milestone(s)" }));
                sb.Append('\n');
                sb.Append("<CODE>─────────────────────┼───────────────┼──────────┼─────────────────────────────</CODE>");
                sb.Append('\n');
                foreach (Task task in featuresInDevelopment)
                {
                    double estimate = 0;
                    if (usePoints)
                        estimate = task.AggregatedPoints;
                    else
                        estimate = task.AggregatedEstimatedDays;
                    Object t = task.GetCustomColumnValue(completedColumn);
                    string daysDone = ((t != null) ? t : "0") + "/" + estimate;
                    string taskShort = task.Name.Substring(0, (task.Name.Length > 20) ? 19 : task.Name.Length) + ((task.Name.Length > 20) ? "…" : "");

                    sb.Append(string.Format(format, new object[] { taskShort, task.AggregatedStatus, daysDone, getMilestoneString(task) }));
                    sb.Append('\n');
                }
            }
            sb.Append('\n');


            sb.Append(string.Format("<BOLD>Feature backlog ({0})</BOLD>", featuresInBacklog.Count));
            sb.Append('\n');
            if (featuresInBacklog.Count > 0)
            {
                string format = "<CODE>{0,-20} │ {1, 6} │ {2, 1}";
                sb.Append(string.Format(format, new object[] { "Name", "Est.", "Milestone(s)" }));
                sb.Append('\n');
                sb.Append("─────────────────────┼────────┼─────────────────────────────</CODE>");
                sb.Append('\n');
                foreach (Task task in featuresInBacklog)
                {
                    double estimate = 0;
                    if (usePoints)
                        estimate = task.AggregatedPoints;
                    else
                        estimate = task.AggregatedEstimatedDays;
                    string taskShort = task.Name.Substring(0, (task.Name.Length > 20) ? 19 : task.Name.Length) + ((task.Name.Length > 20) ? "…" : "");
                    sb.Append(string.Format(format, new object[] { taskShort, estimate, getMilestoneString(task) }));
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }
    }


}

