using System.Collections;
using NUnit.Framework;
using SapirGame.PSG.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace _Project.Develop.SapirGame.PSG.Tests.PlayMode
{
    public sealed class CoinPickupTests
    {
        private GameObject _pickupObject;

        [UnityTest]
        public IEnumerator TryCollect_CalledTwiceInSameFrame_CreditsOnceAndDestroysPickup()
        {
            var wallet = new Wallet();
            _pickupObject = new GameObject("Workshop CoinPickup");
            var pickup = _pickupObject.AddComponent<CoinPickup>();
            pickup.Initialize(wallet, 5);

            bool firstCollection = pickup.TryCollect();
            bool secondCollection = pickup.TryCollect();

            Assert.That(firstCollection, Is.True);
            Assert.That(secondCollection, Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(5));

            yield return null;

            // Unity overloads ==: a destroyed UnityEngine.Object compares to null.
            Assert.That(pickup == null, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(5));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Also cleans up when an assertion fails before the first yield.
            if (_pickupObject != null)
                Object.Destroy(_pickupObject);

            yield return null;
            _pickupObject = null;
        }
    }
}
