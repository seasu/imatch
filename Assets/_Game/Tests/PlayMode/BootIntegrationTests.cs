using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace Game.Tests
{
    /// PlayMode integration tests – require scenes to be in Build Settings.
    [TestFixture]
    public class BootIntegrationTests
    {
        [UnityTest]
        public IEnumerator Boot_LoadsConfigs_AndRoutesToKingdom()
        {
            SceneManager.LoadScene("Boot");
            yield return new WaitForSeconds(5f); // Allow boot sequence to complete

            // After boot, we should be in Kingdom scene
            Assert.AreEqual("Kingdom", SceneManager.GetActiveScene().name,
                "Boot should route to Kingdom on first launch");
        }

        [UnityTest]
        public IEnumerator Kingdom_StartLevel_RoutesToMatch3()
        {
            SceneManager.LoadScene("Kingdom");
            yield return new WaitForSeconds(1f);

            // Simulate routing to match3
            if (Game.Unity.ServiceLocator.TryGet<Game.Core.IGameRouter>(out var router))
            {
                router.GoToMatch3("0001");
                yield return new WaitForSeconds(2f);
                Assert.AreEqual("Match3", SceneManager.GetActiveScene().name,
                    "GoToMatch3 should load Match3 scene");
            }
            else
            {
                Assert.Inconclusive("ServiceLocator not initialized in test context");
            }
        }
    }
}
