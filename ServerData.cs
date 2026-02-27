using GorillaNetworking;
using HarmonyLib;
using MonoMod.Utils;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace CXS
{
    public class ServerData : MonoBehaviour
    {
        #region Configuration
        public static readonly bool ServerDataEnabled = true;  // Disables CXS and admin panel
        public static bool DisableTelemetry = true; // Telemetry disabled - no longer using Raspberry Pi server

        public const string GitHubUsername = "Cha554";
        public const string GitHubRepo = "mist.online";
        public const string GitHubBranch = "main";
        public static readonly string ServerDataEndpoint = $"https://raw.githubusercontent.com/Cha554/mist.online/refs/heads/main/serverdata.json";

        // The dictionary used to assign the admins only seen in your mod.
        public static readonly Dictionary<string, string> LocalAdmins = new Dictionary<string, string>()
        {
                // { "Placeholder Admin UserID", "Placeholder Admin Name" },
        };

        public static void SetupAdminPanel(string playerName) { } // Method used to spawn admin panel
        #endregion

        #region Server Data Code
        private static ServerData instance;

        private static readonly List<string> DetectedModsLabelled = new List<string>();

        private static float DataLoadTime = -1f;
        private static float ReloadTime = -1f;

        private static int LoadAttempts;

        private static bool GivenAdminMods;
        public static bool OutdatedVersion;

        public void Awake()
        {
            instance = this;
            DataLoadTime = Time.time + 5f;

            NetworkSystem.Instance.OnJoinedRoomEvent += OnJoinRoom;

            NetworkSystem.Instance.OnPlayerJoined += UpdatePlayerCount;
            NetworkSystem.Instance.OnPlayerLeft += UpdatePlayerCount;
        }

        public void Update()
        {
            if (DataLoadTime > 0f && Time.time > DataLoadTime && GorillaComputer.instance.isConnectedToMaster)
            {
                DataLoadTime = Time.time + 5f;
                CXS.Log("Attempting to load web data");
                instance.StartCoroutine(LoadServerData());
            }

            if (ReloadTime > 0f)
            {
                if (Time.time > ReloadTime)
                {
                    ReloadTime = Time.time + 60f;
                    instance.StartCoroutine(LoadServerData());
                }
            }
            else
            {
                if (GorillaComputer.instance.isConnectedToMaster)
                    ReloadTime = Time.time + 5f;
            }

            if (Time.time > DataSyncDelay || !PhotonNetwork.InRoom)
            {
                if (PhotonNetwork.InRoom && PhotonNetwork.PlayerList.Length != PlayerCount)
                    instance.StartCoroutine(PlayerDataSync(PhotonNetwork.CurrentRoom.Name, PhotonNetwork.CloudRegion));

                PlayerCount = PhotonNetwork.InRoom ? PhotonNetwork.PlayerList.Length : -1;
            }
        }

        public static void OnJoinRoom() =>
            instance.StartCoroutine(TelementryRequest(PhotonNetwork.CurrentRoom.Name, PhotonNetwork.NickName, PhotonNetwork.CloudRegion, PhotonNetwork.LocalPlayer.UserId, PhotonNetwork.CurrentRoom.IsVisible, PhotonNetwork.PlayerList.Length, NetworkSystem.Instance.GameModeString));

        public static string CleanString(string input, int maxLength = 12)
        {
            input = new string(Array.FindAll(input.ToCharArray(), c => Utils.IsASCIILetterOrDigit(c)));

            if (input.Length > maxLength)
                input = input[..(maxLength - 1)];

            input = input.ToUpper();
            return input;
        }

        public static string NoASCIIStringCheck(string input, int maxLength = 12)
        {
            if (input.Length > maxLength)
                input = input[..(maxLength - 1)];

            input = input.ToUpper();
            return input;
        }

        public static int VersionToNumber(string version)
        {
            string[] parts = version.Split('.');
            if (parts.Length != 3)
                return -1; // Version must be in 'major.minor.patch' format

            return int.Parse(parts[0]) * 100 + int.Parse(parts[1]) * 10 + int.Parse(parts[2]);
        }

        public static readonly Dictionary<string, string> Administrators = new Dictionary<string, string>();
        public static readonly List<string> SuperAdministrators = new List<string>();
        public static string Menu = "None";
        public static IEnumerator LoadServerData()
        {
            using (UnityWebRequest request = UnityWebRequest.Get(ServerDataEndpoint))
            {
                yield return request.SendWebRequest();

                string json = request.downloadHandler.text;
                Debug.Log(json);
                DataLoadTime = -1f;

                JObject data = JObject.Parse(json);

                string minConsoleVersion = (string)data["min-CXS-version"];

                    // Admin dictionary
                    Administrators.Clear();

                    JArray admins = (JArray)data["admins"];
                    foreach (var admin in admins)
                    {
                        string name = admin["name"].ToString();
                        string userId = admin["user-id"].ToString();
                        Menu = admin["menu"].ToString();
                        Administrators[userId] = name;
                    }
                    
                    Administrators.AddRange(LocalAdmins);

                    SuperAdministrators.Clear();

                    JArray superAdmins = (JArray)data["super-admins"];
                    foreach (var superAdmin in superAdmins)
                        SuperAdministrators.Add(superAdmin.ToString());

                    // Give admin panel if on list
                    if (!GivenAdminMods && PhotonNetwork.LocalPlayer.UserId != null && Administrators.TryGetValue(PhotonNetwork.LocalPlayer.UserId, out var administrator))
                    {
                        GivenAdminMods = true;
                        SetupAdminPanel(administrator);
                    }
            }

            yield return null;
        }

        public static IEnumerator TelementryRequest(string directory, string identity, string region, string userid, bool isPrivate, int playerCount, string gameMode)
        {
            // Telemetry disabled - no Raspberry Pi server needed
            yield break;
        }

        private static float DataSyncDelay;
        public static int PlayerCount;

        public static void UpdatePlayerCount(NetPlayer Player) =>
            PlayerCount = -1;

        public static bool IsPlayerSteam(VRRig Player)
        {
            string concat = (string)AccessTools.Field(typeof(VRRig), "rawCosmeticString").GetValue(Player);
            int customPropsCount = Player.Creator.GetPlayerRef().CustomProperties.Count;

            if (concat.Contains("S. FIRST LOGIN")) return true;
            if (concat.Contains("FIRST LOGIN") || customPropsCount >= 2) return true;
            if (concat.Contains("LMAKT.")) return false;

            return false;
        }

        public static IEnumerator PlayerDataSync(string directory, string region)
        {
            // Player data sync disabled - no Raspberry Pi server needed
            yield break;
        }
        #endregion
    }
}
