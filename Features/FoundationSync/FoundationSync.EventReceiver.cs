using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace Nauplius.SP.UserSync.Features.UserSync
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("e8a0b413-ac7b-4618-9173-863a01eefad6")]
    public class UserSyncEventReceiver : SPFeatureReceiver
    {
        private const string tJobName = "Nauplius.SharePoint.FoundationSync";

        // Uncomment the method below to handle the event raised after a feature has been activated.

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            var local = SPFarm.Local;

            var services = from s in local.Services
                           where s.Name == "SPTimerV4"
                           select s;

            var service = services.First();

            foreach (var job in service.JobDefinitions.Where(job => job.Name == tJobName))
            {
                job.Delete();
            }

            var schedule = new SPDailySchedule { BeginHour = 0, EndHour = 4 };
            var timerJob = new AttributePush(tJobName, service) { Schedule = schedule };
            timerJob.Update();

            RegisterLogging(properties, true);
        }


        // Uncomment the method below to handle the event raised before a feature is deactivated.

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            DeleteJob();

            RegisterLogging(properties, false);
        }


        // Uncomment the method below to handle the event raised after a feature has been installed.

        //public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised before a feature is uninstalled.

        public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        {
            DeleteJob();

            RegisterLogging(properties, false);
        }

        // Uncomment the method below to handle the event raised when a feature is upgrading.

        //public override void FeatureUpgrading(SPFeatureReceiverProperties properties, string upgradeActionName, System.Collections.Generic.IDictionary<string, string> parameters)
        //{
        //}

        private static void DeleteJob()
        {
            var local = SPFarm.Local;

            var services = from s in local.Services
                           where s.Name == "SPTimerV4"
                           select s;

            var service = services.First();

            foreach (SPJobDefinition job in service.JobDefinitions.Where(job => job.Name == tJobName))
            {
                job.Delete();
            }
        }

        private static void RegisterLogging(SPFeatureReceiverProperties properties, bool register)
        {
            var farm = properties.Definition.Farm;

            if (farm == null) return;
            var log = FoudationSync.Local;

            if (register)
            {
                if (log != null) return;
                log = new FoudationSync();
                log.Update();

                if (log.Status != SPObjectStatus.Offline)
                {
                    log.Status = SPObjectStatus.Offline;
                }

                if (log.Status != SPObjectStatus.Online)
                {
                    log.Provision();
                }
            }
            else
            {
                if (log == null) return;
                try
                {
                    log.Unprovision();
                }
                catch
                {
                }
                finally
                {
                    log.Delete();
                }
            }
        }
    }
}