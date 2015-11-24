using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HPMSdk;
using Hansoft.ObjectWrapper;
using Hansoft.ObjectWrapper.CustomColumnValues;
using Hansoft.Jean.Behavior.DeriveBehavior;


namespace SE.HansoftExtensions
{
    public class NotAFeatureException : NoNewValueException
    { }

    public class NotAReleaseException : NoNewValueException
    { }

    public class UnlinkedException : NoNewValueException
    { }

    public class TeamStories2ProgramFeatureAggregation
    {
        private static readonly System.Drawing.Color HANSOFT_RED = System.Drawing.Color.FromArgb(0xDC, 0x64, 0x64);
        private const string TEAM_PROJECT_PREFIX = "Team - ";

        private static IEnumerable<Task> LinkedTasks(Task feature, bool followTasks = false, bool followMilestones = true)
        {
            if (!(feature is ProductBacklogItem || feature is ScheduledTask || feature is Release))
                throw new NotAFeatureException();

            var linkedMilestones = followMilestones ? feature.LinkedTasks.Where(t => t is Release && IsTeamProject(t)) : new List<Task>();
            var linkedTasks = followTasks ? feature.LinkedTasks.Where(t => NotComittedBacklogItem(t) && IsTeamProject(t)) : new List<Task>();

            if (linkedMilestones.Count() == 0 && linkedTasks.Count() == 0)
                throw new UnlinkedException();

            var linkedMilestonesTasks = linkedMilestones
                .Cast<Release>()
                .SelectMany(ms => ms.GetSummary().dependentTasks)
                .Where(t => NotComittedBacklogItem(t));
            var linkedMilestonesTasksChildren = linkedMilestonesTasks.SelectMany(t => t.DeepChildren.Cast<Task>());
            var linkedMilestonesTaskLeaves = linkedMilestonesTasks.Concat(linkedMilestonesTasksChildren).Where(t => !t.HasChildren);

            var linkedTasksChildren = linkedTasks.SelectMany(t => t.DeepChildren.Cast<Task>());
            var linkedTaskLeaves = linkedTasks.Concat(linkedTasksChildren).Where(t => !t.HasChildren);

            return linkedMilestonesTaskLeaves.Concat(linkedTaskLeaves).Where(t => (EHPMTaskStatus)t.AggregatedStatus.Value != EHPMTaskStatus.Deleted).Distinct();
        }

        private static bool IsTeamProject(Task task)
        {
            return task.Project.Name.StartsWith(TEAM_PROJECT_PREFIX);
        }

        private static string TeamName(Task task)
        {
            return task.Project.Name.Substring(TEAM_PROJECT_PREFIX.Length);
        }

        private static bool NotComittedBacklogItem(Task t)
        {
            return t.GetType() == typeof(ProductBacklogItem);
        }

        private static string SIGNOFF = "Sign off";
        private static string ACCEPTED = "Accepted by PM/SA";

        public static string AggregatedStatus(Task feature)
        {
            var status = CalcAggregatedStatus(LinkedTasks(feature));
            var signoff = feature.GetCustomColumnValue(SIGNOFF);
            if (signoff == null)
                return status;
            if (status == "Completed")
                return signoff.ToString() == ACCEPTED ? status : "In progress";
            return status;
        }

        public static bool IsCompleted(Task feature)
        {
            var tasks = LinkedTasks(feature);
            if (tasks.Count() == 0)
                return false;
            var completed = tasks.All(i => (EHPMTaskStatus)i.AggregatedStatus.Value == EHPMTaskStatus.Completed);
            var signoffValue = feature.GetCustomColumnValue(SIGNOFF);
            if (signoffValue == null)
                return completed;
            var signoff = signoffValue.ToString() == ACCEPTED;
            return completed && signoff;
        }

        public static int Points(Task feature)
        {
            return LinkedTasks(feature).Sum(t => t.Points);
        }

        public static object PointsNotCompleted(Task feature)
        {
            return LinkedTasks(feature).Where(t => !LeafCompleted(t)).Sum(t => t.Points);
        }

        public static object PointsCompleted(Task feature)
        {
            try
            {
                return LinkedTasks(feature).Where(t => LeafCompleted(t)).Sum(t => t.Points);
            }
            catch (UnlinkedException)
            {
                return "";
            }
        }

        public static object PointsInProgress(Task feature)
        {
            try
            {
                return LinkedTasks(feature).Where(t => LeafStatus(t, EHPMTaskStatus.InProgress)).Select(t => t.Points).DefaultIfEmpty(0).Sum();
            }
            catch (UnlinkedException)
            {
                return "";
            }
        }

        public static object PointsNotDone(Task feature)
        {
            try
            {
                return LinkedTasks(feature).Where(t => LeafStatus(t, EHPMTaskStatus.NotDone)).Select(t => t.Points).DefaultIfEmpty(0).Sum();
            }
            catch (UnlinkedException)
            {
                return "";
            }
        }

        public static object PointsBlocked(Task feature)
        {
            try
            {
                return LinkedTasks(feature).Where(t => LeafStatus(t, EHPMTaskStatus.Blocked)).Select(t => t.Points).DefaultIfEmpty(0).Sum();
            }
            catch (UnlinkedException)
            {
                return "";
            }
        }

        public static object PointsCount(Task feature)
        {
            try 
            { 
                return LinkedTasks(feature).Select(t => t.Points).DefaultIfEmpty(0).Sum(); 
            }
            catch (UnlinkedException) 
            {
                return "";
            }
        }

        public static string Team(Task feature)
        {
            try
            {
                return String.Join(";", feature.LinkedTasks.Where(t => t is Release && IsTeamProject(t)).Select(t => TeamName(t)).Distinct());
            }
            catch (UnlinkedException)
            {
                return "";
            }
        }

        public static object ItemsCompleted(Task feature)
        {
            try
            {
                return LinkedTasks(feature).Where(t => LeafCompleted(t)).Count();
            }
            catch (UnlinkedException)
            {
                return "";
            }
        }

        public static object ItemsInProgress(Task feature)
        {
            try
            {
                return LinkedTasks(feature).Where(t => LeafStatus(t, EHPMTaskStatus.InProgress)).Count();
            }
            catch (UnlinkedException)
            {
                return "";
            }
        }

        public static object ItemsNotDone(Task feature)
        {
            try
            {
                return LinkedTasks(feature).Where(t => LeafStatus(t, EHPMTaskStatus.NotDone)).Count();
            }
            catch (UnlinkedException)
            {
                return "";
            }
        }

        public static object ItemsBlocked(Task feature)
        {
            try
            {
                return LinkedTasks(feature).Where(t => LeafStatus(t, EHPMTaskStatus.Blocked)).Count();
            }
            catch (UnlinkedException)
            {
                return "";
            }
        }

        public static object ItemsCount(Task feature)
        {
            try
            {
                return LinkedTasks(feature).Count();
            }
            catch (UnlinkedException)
            {
                return "";
            }
        }

        public static int Velocity(Task feature)
        {
            return feature.DeepLeaves.FindAll(task => ((Task)task).IsCompleted && ((DateTimeValue)((Task)task).GetCustomColumnValue("Status last changed")).ToDateTime() > DateTime.Now.AddDays(-14)  ).Sum(task => ((Task)task).Points);
        }

        const string FEATURE_SUMMARY_LINE_FORMAT = "<CODE>{0,-15} │ {1,-11} │ {2, -7} │ {3, -8} │ {4, -15} │ {5, -15}</CODE>\n";
        static readonly object[] FEATURE_SUMMARY_HEADINGS = { "Team", "Status", "Points", "Stories", "Product Owner", "Planned sprint" };
        const string FEATURE_SUMMARY_HEADER_SEPARATOR = "<CODE>────────────────┼─────────────┼─────────┼──────────┼─────────────────┼───────────────────</CODE>\n";

        public static string FeatureSummary(Task feature)
        {
            IEnumerable<Task> allTasks;
            try
            {
                allTasks = LinkedTasks(feature);
            }
            catch (UnlinkedException)
            {
                if (feature.GetCustomColumnValue("Planned sprint").ToString() != string.Empty)
                    feature.SetCustomColumnValue("Planned sprint", string.Empty);
                return string.Empty;
            }
            var projects = allTasks.Select(t => t.Project.Name).Distinct();
            var allPlannedSprints = new SortedSet<string>();

            var summary_builder = new StringBuilder();
            summary_builder.Append(string.Format(FEATURE_SUMMARY_LINE_FORMAT, FEATURE_SUMMARY_HEADINGS));
            summary_builder.Append(FEATURE_SUMMARY_HEADER_SEPARATOR);

            foreach (var project in projects)
            {
                var team = project.Substring(TEAM_PROJECT_PREFIX.Length);
                var teamShort = team.Substring(0, (team.Length > 14) ? 13 : team.Length) + ((team.Length > 14) ? "…" : "");
                var tasks = allTasks.Where(t => t.Project.Name == project);
                var status = CalcAggregatedStatus(tasks);
                var completedPoints = tasks.Where(t => LeafCompleted(t)).Sum(t => t.Points);
                var totalPoints = tasks.Sum(t => t.Points);
                var completedStories = tasks.Where(t => LeafCompleted(t)).Count();
                var totalStories = tasks.Count();
                var plannedSprints = tasks
                    .Where(t => !LeafCompleted(t))
                    .Select(t => t.GetCustomColumnValue("Planned sprint"))
                    .Where(sprintColumn => sprintColumn != null)
                    .Select(sprintColumn => sprintColumn.ToString())
                    .Where(sprint => sprint.Length != 0)
                    .Distinct()
                    .OrderBy(sprint => sprint);
                var plannedSprintsString = plannedSprints.Aggregate(
                        new StringBuilder(),
                        (sb, sprint) => sb.Append(sprint).Append(", "),
                        sb => sb.Length > 0 ? sb.ToString(0, sb.Length-2) : "");
                var productOwner = ProductOwnerConfig.GetProductOwner(team, "unknown");
                var productOwnerShort = productOwner.Substring(0, (productOwner.Length > 14) ? 13 : productOwner.Length) + ((productOwner.Length > 14) ? "…" : "");

                summary_builder.AppendFormat(FEATURE_SUMMARY_LINE_FORMAT,
                    teamShort, status, completedPoints + "/" + totalPoints, completedStories + "/" + totalStories, productOwnerShort, plannedSprintsString);

                if (plannedSprintsString.Length > 0)
                {
                    var maxPlannedSprint = plannedSprints.Where(t => t.StartsWith("S")).Max();
                    allPlannedSprints.Add(maxPlannedSprint);
                }
            }

            if (allPlannedSprints.Count() > 0)
                feature.SetCustomColumnValue("Planned sprint", allPlannedSprints.Max());

            return summary_builder.ToString();
        }

        private static string MaxPlannedSprint(CustomColumnValue plannedSprint)
        {
            return plannedSprint.ToString().Split(';').Where(s => s.StartsWith("S")).Max();
        }

        private static bool LeafCompleted(Task t)
        {
            return t.IsCompleted || (EHPMTaskStatus)t.AggregatedStatus.Value == EHPMTaskStatus.Completed;
        }

        private static bool LeafStatus(Task t, EHPMTaskStatus status)
        {
            return (EHPMTaskStatus)t.AggregatedStatus.Value == status;
        }

        private static string CalcAggregatedStatus(IEnumerable<HansoftEnumValue> values)
        {
            if (values.Count() == 0)
                return "Not done";
            if (values.Any(i => (EHPMTaskStatus)i.Value == EHPMTaskStatus.Blocked))
                return "Blocked";
            if (values.All(i => (EHPMTaskStatus)i.Value == EHPMTaskStatus.Completed))
                return "Completed";
            if (values.All(i => (EHPMTaskStatus)i.Value == EHPMTaskStatus.NotDone || (EHPMTaskStatus)i.Value == EHPMTaskStatus.NoStatus))
                return "Not done";
            return "In progress";
        }

        private static string CalcAggregatedStatus(IEnumerable<Task> tasks)
        {
            return CalcAggregatedStatus(tasks.Select(t => t.AggregatedStatus));
        }
    }
}
