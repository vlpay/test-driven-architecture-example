using NUnit.Framework;
using SapirGame.PSG.Runtime;

namespace _Project.Develop.SapirGame.PSG.Tests.EditMode
{
    [TestFixture(10)]
    [TestFixture(100)]
    public sealed class WalletFixtureTests
    {
        private readonly int _initialBalance;
        private Wallet _fixtureBaseline;
        private Wallet _wallet;

        public WalletFixtureTests(int initialBalance)
        {
            _initialBalance = initialBalance;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Один базовый объект создаётся для каждого набора параметров фикстуры.
            _fixtureBaseline = new Wallet(_initialBalance);
        }

        [SetUp]
        public void SetUp()
        {
            // Новый объект изолирует состояние каждого тестового кейса.
            _wallet = new Wallet(_fixtureBaseline.Balance);
        }

        [Test]
        public void StartsWithFixtureBalance()
        {
            Assert.That(_wallet.Balance, Is.EqualTo(_initialBalance));
        }

        [Test]
        public void SpendingInCaseDoesNotChangeFixtureBaseline()
        {
            _wallet.TrySpend(1);

            Assert.That(_wallet.Balance, Is.EqualTo(_initialBalance - 1));
            Assert.That(_fixtureBaseline.Balance, Is.EqualTo(_initialBalance));
        }

        [TearDown]
        public void TearDown()
        {
            _wallet = null;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _fixtureBaseline = null;
        }
    }
}
