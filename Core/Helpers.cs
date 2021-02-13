using Il2CppInspector.Cpp.UnityHeaders;
using System;
using System.Collections.Generic;
using System.IO;
using Unitor.Core.Reflection;

namespace Unitor.Core
{
    public static class Helpers
    {
        public static UnityVersion FromAssetFile(string filePath)
        {
            using var file = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(file, System.Text.Encoding.UTF8);
            var possibleOffsets = new List<int> { 0x14, 0x30 };

            foreach (var offset in possibleOffsets)
            {
                file.Position = offset;
                var bytes = new List<byte>();
                var maxLength = 15;
                byte b;
                while ((b = reader.ReadByte()) != 0 && bytes.Count < maxLength)
                    bytes.Add(b);

                var unityString = System.Text.Encoding.UTF8.GetString(bytes.ToArray());

                try
                {
                    return new UnityVersion(unityString);
                }
                catch (Exception)
                {

                }
            }
            return null;
        }

        public static bool IsUnityMonobehaviourMessage(UnitorMethod m)
        {
            return new List<string>() {
                "Awake",
                "FixedUpdate",
                "LateUpdate",
                "OnAnimatorIK",
                "OnAnimatorMove",
                "OnApplicationFocus",
                "OnApplicationPause",
                "OnApplicationQuit",
                "OnAudioFilterRead",
                "OnBecameInvisible",
                "OnBecameVisible",
                "OnCollisionEnter",
                "OnCollisionEnter2D",
                "OnCollisionExit",
                "OnCollisionExit2D",
                "OnCollisionStay",
                "OnCollisionStay2D",
                "OnConnectedToServer",
                "OnControllerColliderHit",
                "OnDestroy",
                "OnDisable",
                "OnDisconnectedFromServer",
                "OnDrawGizmos",
                "OnDrawGizmosSelected",
                "OnEnable",
                "OnFailedToConnect",
                "OnFailedToConnectToMasterServer",
                "OnGUI",
                "OnJointBreak",
                "OnJointBreak2D",
                "OnMasterServerEvent",
                "OnMouseDown",
                "OnMouseDrag",
                "OnMouseEnter",
                "OnMouseExit",
                "OnMouseOver",
                "OnMouseUp",
                "OnMouseUpAsButton",
                "OnNetworkInstantiate",
                "OnParticleCollision",
                "OnParticleSystemStopped",
                "OnParticleTrigger",
                "OnParticleUpdateJobScheduled",
                "OnPlayerConnected",
                "OnPlayerDisconnected",
                "OnPostRender",
                "OnPreCull",
                "OnPreRender",
                "OnRenderImage",
                "OnRenderObject",
                "OnSerializeNetworkView",
                "OnServerInitialized",
                "OnTransformChildrenChanged",
                "OnTransformParentChanged",
                "OnTriggerEnter",
                "OnTriggerEnter2D",
                "OnTriggerExit",
                "OnTriggerExit2D",
                "OnTriggerStay",
                "OnTriggerStay2D",
                "OnValidate",
                "OnWillRenderObject",
                "Reset",
                "Start",
                "Update",
                "ctor"
            }.Contains(m.Name);
        }
    }
}
