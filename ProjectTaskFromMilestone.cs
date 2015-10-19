using System;
using System.Collections.Generic;
using System.Linq;
using HPMSdk;
using Hansoft.ObjectWrapper;
using Hansoft.ObjectWrapper.CustomColumnValues;
using Hansoft.Jean.Behavior.DeriveBehavior;


namespace SE.HansoftExtensions
{
    public class ProjectTaskFromMilestone
    {
        private const string TEAM_PROJECT_PREFIX = "Team - ";
        private static readonly System.Drawing.Color HANSOFT_YELLOW = System.Drawing.Color.FromArgb(0xE1, 0xE1, 0x78);

        /// <summary>
        /// If task is linked to a Milestone, the task is updated with data from the Milestone.
        /// Finish is set to the milestone date.
        /// Status is aggregated from the milestone backlog item status.
        /// Task completed is calculated from completed milestone backlog item points.
        /// </summary>
        /// <param name="task"></param>
        /// <returns>Aggregated status value</returns>
        public static string Update(Task task)
        {
            if (!(task is ScheduledTask))
                throw new NoNewValueException();

            var targetTask = (ScheduledTask)task;

            var linkedMilestones = targetTask.LinkedTasks.Where(t => t is Release && t.Project.Name.StartsWith(TEAM_PROJECT_PREFIX));
            var linkedScheduledTasks = targetTask.LinkedTasks.Where(t => t is ScheduledTask && t.Project.Name.StartsWith(TEAM_PROJECT_PREFIX));
            var linkedStories = targetTask.LinkedTasks.Where(t => NotComittedBacklogItem(t) && t.Project.Name.StartsWith(TEAM_PROJECT_PREFIX));

            if (linkedMilestones.Count() == 0 && linkedStories.Count() == 0 && linkedScheduledTasks.Count() == 0)
                throw new NoNewValueException();

            // If linked to a milestone or a scheduled task, then no other links are allowed.
            if ((linkedMilestones.Count() > 0 || linkedScheduledTasks.Count() > 0) &&
                (linkedMilestones.Count() + linkedStories.Count() + linkedScheduledTasks.Count() > 1))
                return "Blocked";

            targetTask.Color = HANSOFT_YELLOW;

            if (linkedMilestones.Count() == 1)
            {
                var milestone = (Release)linkedMilestones.First();

                targetTask.Finish = milestone.Date < targetTask.Start ? targetTask.Start : milestone.Date;

                var summary = milestone.GetSummary();

                targetTask.SetDefaultColumnValue(EHPMProjectDefaultColumn.ItemStatus, CalcAggregatedStatus(summary.dependentTasks));
                if ((EHPMTaskStatus)targetTask.Status.Value == EHPMTaskStatus.Completed)
                    targetTask.IsCompleted = true;

                if (milestone.Project.DefaultEditorMode == EHPMProjectDefaultEditorMode.Agile)
                {
                    // When milestone is located in an Agile project, progress is calculated from relative points done.
                    targetTask.PercentComplete = summary.points == 0 ? 0 : 100 * (summary.points - summary.pointNotDone) / summary.points;
                }
                else
                {
                    // When milestone is located in a Traditional project, progress is calculated from relative duration done.
                    targetTask.PercentComplete = summary.durationDays == 0 ? 0 : (int)(100 * (summary.durationDays - summary.durationDaysNotDone) / summary.durationDays);

                    targetTask.SetCustomColumnValue("Remaining work", CalcAggregatedCustomIntegerColumnValue(summary.dependentTasks, "Remaining work"));
                    targetTask.SetCustomColumnValue("Completed work", CalcAggregatedCustomIntegerColumnValue(summary.dependentTasks, "Completed work"));
                }

                return targetTask.Status.Text;
            }
            else if (linkedScheduledTasks.Count() == 1)
            {
                var scheduledTask = (ScheduledTask)linkedScheduledTasks.First();

                targetTask.TimeZone = scheduledTask.TimeZone;

                targetTask.SetDefaultColumnValue(EHPMProjectDefaultColumn.ItemStatus, scheduledTask.AggregatedStatus);
                if ((EHPMTaskStatus)targetTask.Status.Value == EHPMTaskStatus.Completed)
                    targetTask.IsCompleted = true;

                var duration = scheduledTask.AggregatedDuration;
                targetTask.PercentComplete = duration == 0.0 ? 0 : (int)(100 * (duration - scheduledTask.AggregatedDurationNotDone) / duration);

                targetTask.SetCustomColumnValue("Remaining work", scheduledTask.GetAggregatedCustomColumnValue("Remaining work"));
                targetTask.SetCustomColumnValue("Completed work", scheduledTask.GetAggregatedCustomColumnValue("Completed work"));

                return targetTask.Status.Text;
            }
            else if (linkedStories.Count() >= 1)
            {
                targetTask.SetDefaultColumnValue(EHPMProjectDefaultColumn.ItemStatus, CalcAggregatedStatus(linkedStories));
                if ((EHPMTaskStatus)targetTask.Status.Value == EHPMTaskStatus.Completed)
                    targetTask.IsCompleted = true;

                var points = linkedStories.Sum(s => s.AggregatedPoints);
                var pointsNotDone = linkedStories.Sum(s => s.AggregatedPointsNotDone);
                targetTask.PercentComplete = points == 0 ? 0 : 100 * (points - pointsNotDone) / points;

                return targetTask.Status.Text;
            }
            else
                throw new NoNewValueException();
        }

        private static bool NotComittedBacklogItem(Task t)
        {
            return t.GetType() == typeof(ProductBacklogItem);
        }

        private static string CalcAggregatedStatus(IEnumerable<Task> items)
        {
            if (items.Count() == 0)
                return "Not done";
            if (items.Any(i => (EHPMTaskStatus)i.AggregatedStatus.Value == EHPMTaskStatus.Blocked))
                return "Blocked";
            if (items.All(i => (EHPMTaskStatus)i.AggregatedStatus.Value == EHPMTaskStatus.Completed))
                return "Completed";
            if (items.All(i => (EHPMTaskStatus)i.AggregatedStatus.Value == EHPMTaskStatus.NotDone))
                return "Not done";
            return "In progress";
        }

        private static long CalcAggregatedCustomIntegerColumnValue(IEnumerable<Task> tasks, string column)
        {
            return tasks.Sum(t => {
                var customColumnValue = t.GetAggregatedCustomColumnValue(column);
                return customColumnValue == null ? 0 : ((IntegerNumberValue)customColumnValue).ToInt();
            });
        }
    }
}
