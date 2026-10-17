using System;

namespace SapirGame.PSG.Runtime
{
    public sealed class Wallet
    {
        public int Balance { get; private set; }

        public Wallet(int initialBalance = 0)
        {
            if (initialBalance < 0)
                throw new ArgumentOutOfRangeException(nameof(initialBalance));

            Balance = initialBalance;
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (amount > Balance)
                return false;

            Balance -= amount;
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Balance = checked(Balance + amount);
        }
    }
}
