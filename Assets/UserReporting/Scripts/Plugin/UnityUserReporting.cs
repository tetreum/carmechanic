using Unity.Cloud.UserReporting.Client;
using UnityEngine;

namespace Unity.Cloud.UserReporting.Plugin
{
    /// <summary>
    ///     Provides a starting point for Unity User Reporting.
    /// </summary>
    public static class UnityUserReporting
    {
        #region Static Fields

        private static UserReportingClient currentClient;

        #endregion

        #region Static Properties

        /// <summary>
        ///     Gets the current client.
        /// </summary>
        public static UserReportingClient CurrentClient
        {
            get
            {
                if (currentClient == null) Configure();
                return currentClient;
            }
            private set => currentClient = value;
        }

        #endregion

        #region Static Methods

        /// <summary>
        ///     Configures Unity User Reporting.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="projectIdentifier">The project identifier.</param>
        /// <param name="platform">The plaform.</param>
        /// <param name="configuration">The configuration.</param>
        public static void Configure(string endpoint, string projectIdentifier, IUserReportingPlatform platform,
            UserReportingClientConfiguration configuration)
        {
            CurrentClient = new UserReportingClient(endpoint, projectIdentifier, platform, configuration);
        }

        /// <summary>
        ///     Configures Unity User Reporting.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="projectIdentifier"></param>
        /// <param name="configuration"></param>
        public static void Configure(string endpoint, string projectIdentifier,
            UserReportingClientConfiguration configuration)
        {
            CurrentClient = new UserReportingClient(endpoint, projectIdentifier, GetPlatform(), configuration);
        }

        /// <summary>
        ///     Configures Unity User Reporting.
        /// </summary>
        /// <param name="projectIdentifier"></param>
        /// <param name="configuration"></param>
        public static void Configure(string projectIdentifier, UserReportingClientConfiguration configuration)
        {
            Configure("https://userreporting.cloud.unity3d.com", projectIdentifier, GetPlatform(), configuration);
        }

        /// <summary>
        ///     Configures Unity User Reporting.
        /// </summary>
        /// <param name="projectIdentifier"></param>
        public static void Configure(string projectIdentifier)
        {
            Configure("https://userreporting.cloud.unity3d.com", projectIdentifier, GetPlatform(),
                new UserReportingClientConfiguration());
        }

        /// <summary>
        ///     Configures Unity User Reporting.
        /// </summary>
        public static void Configure()
        {
            Configure("https://userreporting.cloud.unity3d.com", Application.cloudProjectId, GetPlatform(),
                new UserReportingClientConfiguration());
        }

        /// <summary>
        ///     Configures Unity User Reporting.
        /// </summary>
        /// <param name="configuration"></param>
        public static void Configure(UserReportingClientConfiguration configuration)
        {
            Configure("https://userreporting.cloud.unity3d.com", Application.cloudProjectId, GetPlatform(),
                configuration);
        }

        /// <summary>
        ///     Configures Unity User Reporting.
        /// </summary>
        /// <param name="projectIdentifier"></param>
        /// <param name="platform"></param>
        /// <param name="configuration"></param>
        public static void Configure(string projectIdentifier, IUserReportingPlatform platform,
            UserReportingClientConfiguration configuration)
        {
            Configure("https://userreporting.cloud.unity3d.com", projectIdentifier, platform, configuration);
        }

        /// <summary>
        ///     Configures Unity User Reporting.
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="configuration"></param>
        public static void Configure(IUserReportingPlatform platform, UserReportingClientConfiguration configuration)
        {
            Configure("https://userreporting.cloud.unity3d.com", Application.cloudProjectId, platform, configuration);
        }

        /// <summary>
        ///     Configures Unity User Reporting.
        /// </summary>
        /// <param name="platform"></param>
        public static void Configure(IUserReportingPlatform platform)
        {
            Configure("https://userreporting.cloud.unity3d.com", Application.cloudProjectId, platform,
                new UserReportingClientConfiguration());
        }

        /// <summary>
        ///     Gets the platform.
        /// </summary>
        /// <returns>The platform.</returns>
        private static IUserReportingPlatform GetPlatform()
        {
            return new UnityUserReportingPlatform();
        }

        /// <summary>
        ///     Uses an existing client.
        /// </summary>
        /// <param name="client">The client.</param>
        public static void Use(UserReportingClient client)
        {
            if (client != null) CurrentClient = client;
        }

        #endregion
    }
}