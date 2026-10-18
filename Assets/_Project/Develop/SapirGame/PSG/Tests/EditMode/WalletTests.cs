using System;
using System.Collections.Generic;
using NUnit.Framework;
using SapirGame.PSG.Runtime;

namespace _Project.Develop.SapirGame.PSG.Tests.EditMode
{
    public sealed class WalletTests
    {
        private Wallet _wallet;

        [SetUp]
        public void SetUp()
        {
            // Каждый кейс получает кошелёк с исходным балансом.
            _wallet = new Wallet(10);
        }

        [TearDown]
        public void TearDown()
        {
            _wallet = null;
        }

        [Test]
        public void TrySpend_ExactBalance_SucceedsAndLeavesZero()
        {
            bool success = _wallet.TrySpend(10);

            Assert.That(success, Is.True);
            Assert.That(_wallet.Balance, Is.Zero);
        }

        [TestCase(3, true, 7)]
        [TestCase(11, false, 10)]
        public void TrySpend_ReturnsExpectedResultAndBalance(int amount, bool expectedSuccess, int expectedBalance)
        {
            bool success = _wallet.TrySpend(amount);

            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(_wallet.Balance, Is.EqualTo(expectedBalance));
        }

        [Test]
        public void TrySpend_RangeChecksValuesAroundBalance([Range(9, 11)] int amount)
        {
            bool expectedSuccess = amount <= 10;

            bool success = _wallet.TrySpend(amount);

            Assert.That(success, Is.EqualTo(expectedSuccess));
            Assert.That(_wallet.Balance, Is.EqualTo(expectedSuccess ? 10 - amount : 10));
        }

        [TestCaseSource(nameof(InvalidSpendAmounts))]
        public void TrySpend_NonPositiveAmount_Throws(int amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.TrySpend(amount));
        }

        public static IEnumerable<TestCaseData> InvalidSpendAmounts()
        {
            yield return new TestCaseData(0).SetName("TrySpend_ZeroAmount_Throws");
            yield return new TestCaseData(-1).SetName("TrySpend_NegativeAmount_Throws");
        }

        [TestCase(5, 15)]
        [TestCase(1, 11)]
        public void Add_PositiveAmount_IncreasesBalance(int amount, int expectedBalance)
        {
            _wallet.Add(amount);

            Assert.That(_wallet.Balance, Is.EqualTo(expectedBalance));
        }
    }
}
