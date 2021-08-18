using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unity.Cloud.UserReporting
{
    /// <summary>
    ///     Represents a user report.
    /// </summary>
    public class UserReport : UserReportPreview
    {
        #region Constructors

        /// <summary>
        ///     Creates a new instance of the <see cref="UserReport" /> class.
        /// </summary>
        public UserReport()
        {
            AggregateMetrics = new List<UserReportMetric>();
            Attachments = new List<UserReportAttachment>();
            ClientMetrics = new List<UserReportMetric>();
            DeviceMetadata = new List<UserReportNamedValue>();
            Events = new List<UserReportEvent>();
            Fields = new List<UserReportNamedValue>();
            Measures = new List<UserReportMeasure>();
            Screenshots = new List<UserReportScreenshot>();
        }

        #endregion

        #region Nested Types

        /// <summary>
        ///     Provides sorting for metrics.
        /// </summary>
        private class UserReportMetricSorter : IComparer<UserReportMetric>
        {
            #region Methods

            /// <inheritdoc />
            public int Compare(UserReportMetric x, UserReportMetric y)
            {
                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }

            #endregion
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the attachments.
        /// </summary>
        public List<UserReportAttachment> Attachments { get; set; }

        /// <summary>
        ///     Gets or sets the client metrics.
        /// </summary>
        public List<UserReportMetric> ClientMetrics { get; set; }

        /// <summary>
        ///     Gets or sets the device metadata.
        /// </summary>
        public List<UserReportNamedValue> DeviceMetadata { get; set; }

        /// <summary>
        ///     Gets or sets the events.
        /// </summary>
        public List<UserReportEvent> Events { get; set; }

        /// <summary>
        ///     Gets or sets the fields.
        /// </summary>
        public List<UserReportNamedValue> Fields { get; set; }

        /// <summary>
        ///     Gets or sets the measures.
        /// </summary>
        public List<UserReportMeasure> Measures { get; set; }

        /// <summary>
        ///     Gets or sets the screenshots.
        /// </summary>
        public List<UserReportScreenshot> Screenshots { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Clones the user report.
        /// </summary>
        /// <returns>The cloned user report.</returns>
        public UserReport Clone()
        {
            var userReport = new UserReport();
            userReport.AggregateMetrics = AggregateMetrics != null ? AggregateMetrics.ToList() : null;
            userReport.Attachments = Attachments != null ? Attachments.ToList() : null;
            userReport.ClientMetrics = ClientMetrics != null ? ClientMetrics.ToList() : null;
            userReport.ContentLength = ContentLength;
            userReport.DeviceMetadata = DeviceMetadata != null ? DeviceMetadata.ToList() : null;
            userReport.Dimensions = Dimensions.ToList();
            userReport.Events = Events != null ? Events.ToList() : null;
            userReport.ExpiresOn = ExpiresOn;
            userReport.Fields = Fields != null ? Fields.ToList() : null;
            userReport.Identifier = Identifier;
            userReport.IPAddress = IPAddress;
            userReport.Measures = Measures != null ? Measures.ToList() : null;
            userReport.ProjectIdentifier = ProjectIdentifier;
            userReport.ReceivedOn = ReceivedOn;
            userReport.Screenshots = Screenshots != null ? Screenshots.ToList() : null;
            userReport.Summary = Summary;
            userReport.Thumbnail = Thumbnail;
            return userReport;
        }

        /// <summary>
        ///     Completes the user report. This is called by the client and only needs to be called when constructing a user report
        ///     manually.
        /// </summary>
        public void Complete()
        {
            // Thumbnail
            if (Screenshots.Count > 0) Thumbnail = Screenshots[Screenshots.Count - 1];

            // Aggregate Metrics
            var aggregateMetrics = new Dictionary<string, UserReportMetric>();
            foreach (var measure in Measures)
            foreach (var metric in measure.Metrics)
            {
                if (!aggregateMetrics.ContainsKey(metric.Name))
                {
                    var userReportMetric = new UserReportMetric();
                    userReportMetric.Name = metric.Name;
                    aggregateMetrics.Add(metric.Name, userReportMetric);
                }

                var aggregateMetric = aggregateMetrics[metric.Name];
                aggregateMetric.Sample(metric.Average);
                aggregateMetrics[metric.Name] = aggregateMetric;
            }

            if (AggregateMetrics == null) AggregateMetrics = new List<UserReportMetric>();
            foreach (var kvp in aggregateMetrics) AggregateMetrics.Add(kvp.Value);
            AggregateMetrics.Sort(new UserReportMetricSorter());
        }

        /// <summary>
        ///     Fixes the user report by replace null lists with empty lists.
        /// </summary>
        public void Fix()
        {
            AggregateMetrics = AggregateMetrics ?? new List<UserReportMetric>();
            Attachments = Attachments ?? new List<UserReportAttachment>();
            ClientMetrics = ClientMetrics ?? new List<UserReportMetric>();
            DeviceMetadata = DeviceMetadata ?? new List<UserReportNamedValue>();
            Dimensions = Dimensions ?? new List<UserReportNamedValue>();
            Events = Events ?? new List<UserReportEvent>();
            Fields = Fields ?? new List<UserReportNamedValue>();
            Measures = Measures ?? new List<UserReportMeasure>();
            Screenshots = Screenshots ?? new List<UserReportScreenshot>();
        }

        /// <summary>
        ///     Gets the dimension string for the dimensions associated with this user report.
        /// </summary>
        /// <returns></returns>
        public string GetDimensionsString()
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < Dimensions.Count; i++)
            {
                var dimension = Dimensions[i];
                stringBuilder.Append(dimension.Name);
                stringBuilder.Append(": ");
                stringBuilder.Append(dimension.Value);
                if (i != Dimensions.Count - 1) stringBuilder.Append(", ");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        ///     Removes screenshots above a certain size from the user report.
        /// </summary>
        /// <param name="maximumWidth">The maximum width.</param>
        /// <param name="maximumHeight">The maximum height.</param>
        /// <param name="totalBytes">The total bytes allowed by screenshots.</param>
        /// <param name="ignoreCount">The number of screenshots to ignoreCount.</param>
        public void RemoveScreenshots(int maximumWidth, int maximumHeight, int totalBytes, int ignoreCount)
        {
            var byteCount = 0;
            for (var i = Screenshots.Count; i > 0; i--)
            {
                if (i < ignoreCount) continue;
                var screenshot = Screenshots[i];
                byteCount += screenshot.DataBase64.Length;
                if (byteCount > totalBytes) break;
                if (screenshot.Width > maximumWidth || screenshot.Height > maximumHeight) Screenshots.RemoveAt(i);
            }
        }

        /// <summary>
        ///     Casts the user report to a user report preview.
        /// </summary>
        /// <returns>The user report preview.</returns>
        public UserReportPreview ToPreview()
        {
            var userReportPreview = new UserReportPreview();
            userReportPreview.AggregateMetrics = AggregateMetrics != null ? AggregateMetrics.ToList() : null;
            userReportPreview.ContentLength = ContentLength;
            userReportPreview.Dimensions = Dimensions != null ? Dimensions.ToList() : null;
            userReportPreview.ExpiresOn = ExpiresOn;
            userReportPreview.Identifier = Identifier;
            userReportPreview.IPAddress = IPAddress;
            userReportPreview.ProjectIdentifier = ProjectIdentifier;
            userReportPreview.ReceivedOn = ReceivedOn;
            userReportPreview.Summary = Summary;
            userReportPreview.Thumbnail = Thumbnail;
            return userReportPreview;
        }

        #endregion
    }
}