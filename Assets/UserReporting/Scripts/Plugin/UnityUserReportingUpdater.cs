using System.Collections;
using UnityEngine;

namespace Unity.Cloud.UserReporting.Plugin
{
    /// <summary>
    ///     Helps with updating the Unity User Reporting client.
    /// </summary>
    public class UnityUserReportingUpdater : IEnumerator
    {
        #region Constructors

        /// <summary>
        ///     Creates a new instance of the <see cref="UnityUserReportingUpdater" /> class.
        /// </summary>
        public UnityUserReportingUpdater()
        {
            waitForEndOfFrame = new WaitForEndOfFrame();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the current item.
        /// </summary>
        public object Current { get; private set; }

        #endregion

        #region Fields

        private int step;

        private readonly WaitForEndOfFrame waitForEndOfFrame;

        #endregion

        #region Methods

        /// <summary>
        ///     Moves to the next item.
        /// </summary>
        /// <returns>A value indicating whether the move was successful.</returns>
        public bool MoveNext()
        {
            if (step == 0)
            {
                UnityUserReporting.CurrentClient.Update();
                Current = null;
                step = 1;
                return true;
            }

            if (step == 1)
            {
                Current = waitForEndOfFrame;
                step = 2;
                return true;
            }

            if (step == 2)
            {
                UnityUserReporting.CurrentClient.UpdateOnEndOfFrame();
                Current = null;
                step = 3;
                return false;
            }

            return false;
        }

        /// <summary>
        ///     Resets the updater.
        /// </summary>
        public void Reset()
        {
            step = 0;
        }

        #endregion
    }
}