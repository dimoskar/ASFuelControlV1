using System;
using System.IO;
using System.Threading;
using ArbitransMyData; // arbitrans_myDATA_framework.dll

namespace Exedron.ProviderInvoicing
{
    public sealed class LicenseHandler
    {
        // Same across BOTH apps so they coordinate.
        private const string GlobalMutexName = @"Global\Exedron.ProviderInvoice.ActivationMutex";

        private readonly string _statePath;

        public LicenseHandler(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName)) throw new ArgumentException("companyName");

            _statePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                companyName,
                "license_state.json");

            EnsureDirectoryExists(_statePath);
        }

        /// <summary>
        /// Call on startup from BOTH apps.
        /// If already activated (and keys haven't changed), it just verifies by getSubscriptionInfo().
        /// If not activated yet, it performs:
        ///   DeactivateLicense() ONCE (only if not done before)
        ///   ActivateSubscription(user,key)
        ///   getSubscriptionInfo()
        /// </summary>
        public ActivationResult EnsureActivated(string arbitransUser, string arbitransKey)
        {
            if (string.IsNullOrWhiteSpace(arbitransUser)) throw new ArgumentException("arbitransUser");
            if (string.IsNullOrWhiteSpace(arbitransKey)) throw new ArgumentException("arbitransKey");

            using (var mutex = new Mutex(false, GlobalMutexName))
            {
                // Wait up to 30s to avoid deadlock if the other app is activating.
                if (!mutex.WaitOne(TimeSpan.FromSeconds(30)))
                    throw new TimeoutException("Could not obtain activation mutex. Another instance may be activating.");

                try
                {
                    var state = LoadState();

                    // If the same key was already used successfully, we can just verify quickly.
                    if (state != null && state.LastActivatedKey == arbitransKey && state.LastActivatedUser == arbitransUser && state.LastActivationSucceeded)
                    {
                        var act = new Activation();
                        var info = SafeGetInfo(act);
                        return new ActivationResult
                        {
                            ActivateMessage = "(skipped - already activated with same key)",
                            SubscriptionInfo = info
                        };
                    }

                    // Otherwise, do activation flow.
                    var activation = new Activation();

                    string deactivateMsg = null;
                    if (state == null || !state.DeactivatedOnce)
                    {
                        // WARNING: should happen only once on the machine.
                        object res = activation.DeactivateLicense();
                        deactivateMsg = res == null ? null : res.ToString();

                        if (state == null) state = new LicenseState();
                        state.DeactivatedOnce = true;
                    }

                    string activateMsg = activation.ActivateSubscription(arbitransUser, arbitransKey);

                    // Verify activation
                    var info2 = SafeGetInfo(activation);

                    // Save success
                    state.LastActivatedUser = arbitransUser;
                    state.LastActivatedKey = arbitransKey;
                    state.LastActivationSucceeded = true;
                    state.LastActivationUtc = DateTime.UtcNow.ToString("o");
                    SaveState(state);

                    return new ActivationResult
                    {
                        DeactivateMessage = deactivateMsg,
                        ActivateMessage = activateMsg,
                        SubscriptionInfo = info2
                    };
                }
                catch
                {
                    // If anything fails, record it so next run can attempt again.
                    var state = LoadState() ?? new LicenseState();
                    state.LastActivationSucceeded = false;
                    state.LastActivationUtc = DateTime.UtcNow.ToString("o");
                    SaveState(state);
                    throw;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// Call when you receive a NEW yearly ArbitransKey and need to apply it.
        /// Per your instructions: if you must run ActivateSubscription again, call Refresh() first.
        /// </summary>
        public ActivationResult RenewKey(string arbitransUser, string newArbitransKey)
        {
            if (string.IsNullOrWhiteSpace(arbitransUser)) throw new ArgumentException("arbitransUser");
            if (string.IsNullOrWhiteSpace(newArbitransKey)) throw new ArgumentException("newArbitransKey");

            using (var mutex = new Mutex(false, GlobalMutexName))
            {
                if (!mutex.WaitOne(TimeSpan.FromSeconds(30)))
                    throw new TimeoutException("Could not obtain activation mutex. Another instance may be activating.");

                try
                {
                    var state = LoadState() ?? new LicenseState();

                    var activation = new Activation();

                    // Important routine for renewals / reactivation
                    activation.Refresh();

                    // DO NOT call DeactivateLicense again if already done once.
                    string deactivateMsg = null;
                    if (!state.DeactivatedOnce)
                    {
                        object res = activation.DeactivateLicense();
                        deactivateMsg = res == null ? null : res.ToString();
                        state.DeactivatedOnce = true;
                    }

                    string activateMsg = activation.ActivateSubscription(arbitransUser, newArbitransKey);
                    var info = SafeGetInfo(activation);

                    state.LastActivatedUser = arbitransUser;
                    state.LastActivatedKey = newArbitransKey;
                    state.LastActivationSucceeded = true;
                    state.LastActivationUtc = DateTime.UtcNow.ToString("o");
                    SaveState(state);

                    return new ActivationResult
                    {
                        DeactivateMessage = deactivateMsg,
                        ActivateMessage = activateMsg,
                        SubscriptionInfo = info
                    };
                }
                catch
                {
                    var state = LoadState() ?? new LicenseState();
                    state.LastActivationSucceeded = false;
                    state.LastActivationUtc = DateTime.UtcNow.ToString("o");
                    SaveState(state);
                    throw;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        public ActivationResult ReadCurrentInfo()
        {
            var act = new Activation();
            return new ActivationResult { SubscriptionInfo = SafeGetInfo(act) };
        }

        private static SubscriptionData SafeGetInfo(Activation act)
        {
            // If the library throws before first activation on a brand new machine,
            // you’ll see it here; caller should handle and trigger EnsureActivated.
            Array res = act.getSubscriptionInfo();
            var user = res.GetValue(0, 1);
            var key = res.GetValue(1, 1);
            var expDate = res.GetValue(2, 1);
            var usedLocations = res.GetValue(3, 1);
            var sd = new SubscriptionData();
            sd.User = user.ToString();
            sd.Key = key.ToString();
            sd.ExirationDate = DateTime.Parse(expDate.ToString());
            if (usedLocations.ToString().StartsWith("<<"))
                sd.RemainingLicenses = 1000;
            else
            {
                var ulParams = usedLocations.ToString().Split('/');
                sd.RemainingLicenses = int.Parse(ulParams[1]) - int.Parse(ulParams[0]);
            }

            return sd;
        }

        // ---------- State persistence (shared by both apps) ----------

        private LicenseState LoadState()
        {
            if (!File.Exists(_statePath)) return null;
            string json = File.ReadAllText(_statePath);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<LicenseState>(json);
        }

        private void SaveState(LicenseState state)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(state);
            File.WriteAllText(_statePath, json);
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private sealed class LicenseState
        {
            public bool DeactivatedOnce { get; set; }
            public string LastActivatedUser { get; set; }
            public string LastActivatedKey { get; set; }
            public bool LastActivationSucceeded { get; set; }
            public string LastActivationUtc { get; set; }
        }
    }

    public sealed class ActivationResult
    {
        public string DeactivateMessage { get; set; }
        public string ActivateMessage { get; set; }
        public SubscriptionData SubscriptionInfo { get; set; }
        public bool HasError { set; get; }
        public bool Override { set; get; }
    }
    public sealed class SubscriptionData
    {
        public string User { set; get; }
        public string Key { set; get; }
        public DateTime ExirationDate { set; get; }
        public int RemainingLicenses { set; get; }
    }

}
