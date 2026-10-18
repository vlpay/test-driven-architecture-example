using System;
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
        private CoinPickup _pickup;
        private Wallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet();
            _pickupObject = new GameObject("Workshop CoinPickup");
            _pickup = _pickupObject.AddComponent<CoinPickup>();
        }

        [Test]
        public void TryCollect_BeforeInitialization_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _pickup.TryCollect());
        }

        [Test]
        public void Initialize_NullWallet_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _pickup.Initialize(null));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Initialize_NonPositiveAmount_Throws(int amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _pickup.Initialize(_wallet, amount));
        }

        [Test]
        public void Initialize_CalledTwice_Throws()
        {
            _pickup.Initialize(_wallet, 5);

            Assert.Throws<InvalidOperationException>(() => _pickup.Initialize(_wallet, 5));
        }

        [UnityTest]
        public IEnumerator TryCollect_CalledTwiceInSameFrame_CreditsOnceAndDestroysPickup()
        {
            _pickup.Initialize(_wallet, 5);

            bool firstCollection = _pickup.TryCollect();
            bool secondCollection = _pickup.TryCollect();

            Assert.That(firstCollection, Is.True);
            Assert.That(secondCollection, Is.False);
            Assert.That(_wallet.Balance, Is.EqualTo(5));

            // Ждём следующий кадр, чтобы Unity выполнила отложенный Destroy.
            yield return null;

            Assert.That(_pickup == null, Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator TryCollect_CustomAmount_CreditsWalletAndDestroysPickup()
        {
            _pickup.Initialize(_wallet, 7);

            bool success = _pickup.TryCollect();

            Assert.That(success, Is.True);
            Assert.That(_wallet.Balance, Is.EqualTo(7));

            yield return null;

            Assert.That(_pickup == null, Is.True);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Удаляем объект, если тест завершился до отложенного Destroy.
            if (_pickupObject != null)
                UnityEngine.Object.Destroy(_pickupObject);

            yield return null;
            _pickupObject = null;
            _pickup = null;
            _wallet = null;
        }
    }
}
