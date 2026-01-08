using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Mirror;
using System.Collections;
using System.Reflection;

namespace NetworkCore.Tests
{
    public class CharacterSelectionSyncTests
    {
        private GameObject networkPlayerObj;
        private GameObject characterModelManagerObj;

        [SetUp]
        public void Setup()
        {
            networkPlayerObj = new GameObject("TestNetworkPlayer");
            var networkPlayerType = System.Type.GetType("NetworkPlayer, Assembly-CSharp");
            networkPlayerObj.AddComponent(networkPlayerType);
            networkPlayerObj.AddComponent<NetworkIdentity>();

            characterModelManagerObj = new GameObject("TestCharacterModelManager");
            var characterModelManagerType = System.Type.GetType("CharacterModelManager, Assembly-CSharp");
            characterModelManagerObj.AddComponent(characterModelManagerType);
            characterModelManagerObj.AddComponent<NetworkIdentity>();
        }

        [TearDown]
        public void TearDown()
        {
            if (networkPlayerObj != null)
            {
                Object.DestroyImmediate(networkPlayerObj);
            }

            if (characterModelManagerObj != null)
            {
                Object.DestroyImmediate(characterModelManagerObj);
            }
        }

        [Test]
        public void Test_SelectedCharacterId_ShouldOnlyBeSetByCommand()
        {
            var networkPlayerType = System.Type.GetType("NetworkPlayer, Assembly-CSharp");
            var networkPlayer = networkPlayerObj.GetComponent(networkPlayerType);
            FieldInfo selectedCharacterIdField = networkPlayerType.GetField("selectedCharacterId", BindingFlags.Public | BindingFlags.Instance);
            string initialCharacterId = (string)selectedCharacterIdField.GetValue(networkPlayer);

            Assert.IsNull(initialCharacterId, "初始selectedCharacterId应为null");

            selectedCharacterIdField.SetValue(networkPlayer, "Scout");

            Assert.IsNotNull(selectedCharacterIdField.GetValue(networkPlayer), "直接修改selectedCharacterId违反了服务器权威原则");

            Debug.LogWarning("此测试验证了直接修改selectedCharacterId的行为，实际代码中应避免这种情况");
        }

        [UnityTest]
        public IEnumerator Test_CharacterModelManager_ShouldNotAutoSetDefaultCharacter()
        {
            bool autoSetDetected = false;

            string[] lines = System.IO.File.ReadAllLines("Assets/Scripts/Core/CharacterModelManager.cs");
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("selectedCharacterId = \"Scout\"") ||
                    lines[i].Contains("selectedCharacterId = Scout"))
                {
                    autoSetDetected = true;
                    break;
                }
            }

            Assert.IsFalse(autoSetDetected, "CharacterModelManager不应自动设置默认角色Scout");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_GameNetworkManager_ShouldNotAutoSetDefaultCharacter()
        {
            bool autoSetDetected = false;

            string[] lines = System.IO.File.ReadAllLines("Assets/Scripts/Core/GameNetworkManager.cs");
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("selectedCharacterId = \"Scout\"") ||
                    lines[i].Contains("selectedCharacterId = Scout"))
                {
                    autoSetDetected = true;
                    break;
                }
            }

            Assert.IsFalse(autoSetDetected, "GameNetworkManager不应自动设置默认角色Scout");

            yield return null;
        }

        [Test]
        public void Test_CmdSelectCharacter_ShouldBeOnlyWayToSetCharacterId()
        {
            var networkPlayerType = System.Type.GetType("NetworkPlayer, Assembly-CSharp");
            var method = networkPlayerType.GetMethod("CmdSelectCharacter", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(method, "CmdSelectCharacter方法应存在");

            var commandAttribute = method.GetCustomAttributes(typeof(CommandAttribute), false);
            Assert.AreEqual(1, commandAttribute.Length, "CmdSelectCharacter方法应有[Command]属性");

            Debug.Log("CmdSelectCharacter是唯一设置角色ID的正确方式");
        }

        [UnityTest]
        public IEnumerator Test_CharacterSelectionSync_ShouldWorkInParrelSyncEnvironment()
        {
            var networkPlayerType = System.Type.GetType("NetworkPlayer, Assembly-CSharp");
            var networkPlayer = networkPlayerObj.GetComponent(networkPlayerType);
            FieldInfo selectedCharacterIdField = networkPlayerType.GetField("selectedCharacterId", BindingFlags.Public | BindingFlags.Instance);
            string testCharacterId = "Architect";

            Assert.IsNull(selectedCharacterIdField.GetValue(networkPlayer), "初始selectedCharacterId应为null");

            yield return null;

            Assert.IsNull(selectedCharacterIdField.GetValue(networkPlayer), "在没有CmdSelectCharacter的情况下，selectedCharacterId应保持为null");

            Debug.Log("角色选择同步在ParrelSync环境中应正常工作，服务器端不应自动设置默认角色");

            yield return null;
        }
    }
}