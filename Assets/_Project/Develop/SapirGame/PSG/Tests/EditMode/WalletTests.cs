using NUnit.Framework;
using SapirGame.PSG.Runtime;

namespace _Project.Develop.SapirGame.PSG.Tests.EditMode
{
    public sealed class WalletTests
    {
        [Test]
        public void CanSpendCoins([Range(1, 100)] int price)
        {
            // Arrange: prepare a wallet with 10 coins.
            var wallet = new Wallet(10);

            // Act: try to spend all 10 coins.
            bool success = wallet.TrySpend(price);

            // Assert: the purchase succeeds and leaves no coins.
            Assert.That(success, Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(10 - price));
        }
    }
}
