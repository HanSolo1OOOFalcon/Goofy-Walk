using BepInEx;
using UnityEngine;
using GorillaLocomotion;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;

namespace GoofyWalk
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        bool inRoom = false;
        bool overrideChoice = false;

        void OnEnable()
        {
            overrideChoice = false;
        }

        void OnDisable()
        {
            overrideChoice = true;
        }

        void Start()
        {
            HarmonyPatches.ApplyHarmonyPatches();
            GorillaTagger.OnPlayerSpawned(Init);

            Hashtable table = new Hashtable();
            table.Add("GoofyWalkVersion", PluginInfo.Version);
            PhotonNetwork.LocalPlayer.SetCustomProperties(table);
        }

        void Init()
        {
            NetworkSystem.Instance.OnJoinedRoomEvent += OnJoinRoom;
            NetworkSystem.Instance.OnReturnedToSinglePlayer += OnLeftRoom;
        }

        void OnJoinRoom()
        {
            inRoom = NetworkSystem.Instance.GameModeString.Contains("MODDED");
        }

        void OnLeftRoom()
        {
            inRoom = false;
        }

        void Update()
        {
            if (!overrideChoice && inRoom)
            {
                Vector3 eulerAngles = GTPlayer.Instance.headCollider.transform.rotation.eulerAngles;
                float x = Mathf.Clamp((eulerAngles.x > 180f) ? (eulerAngles.x - 360f) : eulerAngles.x, -90f, 90f);
                float z = Mathf.Clamp((eulerAngles.z > 180f) ? (eulerAngles.z - 360f) : eulerAngles.z, -90f, 90f);
                GTPlayer.Instance.headCollider.transform.rotation = Quaternion.Euler(x, CalculateImprovedTorsoYaw(GTPlayer.Instance.headCollider.transform, GTPlayer.Instance.leftControllerTransform, GTPlayer.Instance.rightControllerTransform) + 180f, z);
            }
        }

        private static Vector3 smoothedTorsoForward = Vector3.forward;
        private static float CalculateImprovedTorsoYaw(Transform head, Transform leftHand, Transform rightHand)
        {
            Vector3 handMid = (leftHand.position + rightHand.position) * 0.5f;
            Vector3 headToMid = handMid - head.position;
            headToMid.y = 0f;
            headToMid.Normalize();

            Vector3 handOffset = rightHand.position - leftHand.position;
            Vector3 torsoForwardGuess = Vector3.Cross(Vector3.up, handOffset);
            torsoForwardGuess.y = 0f;
            torsoForwardGuess.Normalize();

            Vector3 headForward = head.forward;
            headForward.y = 0f;
            headForward.Normalize();

            Vector3 blended = (torsoForwardGuess * 0.6f + headForward * 0.3f + headToMid * 0.1f).normalized;
            smoothedTorsoForward = Vector3.Slerp(smoothedTorsoForward, blended, 0.4f);
            float yaw = Mathf.Atan2(smoothedTorsoForward.x, smoothedTorsoForward.z) * Mathf.Rad2Deg;
            return yaw;
        }
    }
}
