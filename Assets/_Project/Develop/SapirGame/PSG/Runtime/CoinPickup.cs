using System;
using UnityEngine;

namespace SapirGame.PSG.Runtime
{
    public sealed class CoinPickup : MonoBehaviour
    {
        private Wallet _wallet;
        private int _amount;
        private bool _collected;

        public void Initialize(Wallet wallet, int amount = 5)
        {
            if (wallet == null)
                throw new ArgumentNullException(nameof(wallet));
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (_wallet != null)
                throw new InvalidOperationException("CoinPickup is already initialized.");

            _wallet = wallet;
            _amount = amount;
        }

        public bool TryCollect()
        {
            if (_wallet == null)
                throw new InvalidOperationException("Initialize CoinPickup before collecting it.");
            if (_collected)
                return false;

            _wallet.Add(_amount);
            _collected = true;
            // Destroy is deferred; a second call in the same frame must be safe.
            Destroy(gameObject);
            return true;
        }
    }
}
