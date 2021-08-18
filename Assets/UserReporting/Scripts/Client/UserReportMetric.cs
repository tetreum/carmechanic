using System;

namespace Unity.Cloud.UserReporting
{
    /// <summary>
    ///     Represents a user report metrics.
    /// </summary>
    public struct UserReportMetric
    {
        #region Properties

        /// <summary>
        ///     Gets the average.
        /// </summary>
        public double Average => Sum / Count;

        /// <summary>
        ///     Gets the count.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        ///     Gets the maximum.
        /// </summary>
        public double Maximum { get; set; }

        /// <summary>
        ///     Gets the minimum.
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        ///     Gets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Gets the sum.
        /// </summary>
        public double Sum { get; set; }

        #endregion

        #region Methods

        /// <summary>
        ///     Samples a value.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Sample(double value)
        {
            if (Count == 0)
            {
                Minimum = double.MaxValue;
                Maximum = double.MinValue;
            }

            Count++;
            Sum += value;
            Minimum = Math.Min(Minimum, value);
            Maximum = Math.Max(Maximum, value);
        }

        #endregion
    }
}