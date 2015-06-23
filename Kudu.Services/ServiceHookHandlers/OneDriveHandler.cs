using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web;
using Kudu.Contracts.Settings;
using Kudu.Contracts.Tracing;
using Kudu.Core;
using Kudu.Core.Deployment;
using Kudu.Core.SourceControl;
using Kudu.Services.FetchHelpers;
using Newtonsoft.Json.Linq;

namespace Kudu.Services.ServiceHookHandlers
{
    public class OneDriveHandler : IServiceHookHandler
    {
        private readonly OneDriveHelper _oneDriveHelper;

        public OneDriveHandler(ITracer tracer,
                               IDeploymentStatusManager status,
                               IDeploymentSettingsManager settings,
                               IEnvironment environment)
        {
            _oneDriveHelper = new OneDriveHelper(tracer, status, settings, environment);
        }

        public DeployAction TryParseDeploymentInfo(HttpRequestBase request, JObject payload, string targetBranch, out DeploymentInfo deploymentInfo)
        {
            deploymentInfo = null;
            string url = payload.Value<string>("RepositoryUrl");
            if (string.IsNullOrWhiteSpace(url) || !url.ToLowerInvariant().Contains("api.onedrive.com"))
            {
                return DeployAction.UnknownPayload;
            }

            // TODO, suwatch: either pass author and email with payload or we figure it out from Kudu
            /*
                 Expecting payload to be:
                 {
                    "RepositoryUrl": "xxx",
                    "AccessToken": "xxx"
                 }
             */
            deploymentInfo = new OneDriveInfo()
            {
                Deployer = "OneDrive",
                RepositoryUrl = url,
                AccessToken = payload.Value<string>("AccessToken")
            };

            // TODO, suwatch: proper username and password
            deploymentInfo.TargetChangeset = DeploymentManager.CreateTemporaryChangeSet(
                authorName: "Unknown",
                authorEmail: "Unknown",
                message: String.Format(CultureInfo.CurrentUICulture, Resources.OneDrive_Synchronizing)
            );

            return DeployAction.ProcessDeployment;
        }

        public async Task Fetch(IRepository repository, DeploymentInfo deploymentInfo, string targetBranch, ILogger logger)
        {
            var oneDriveInfo = (OneDriveInfo)deploymentInfo;
            _oneDriveHelper.Logger = logger;
            oneDriveInfo.TargetChangeset =  await _oneDriveHelper.Sync(oneDriveInfo, repository);
        }
    }
}
