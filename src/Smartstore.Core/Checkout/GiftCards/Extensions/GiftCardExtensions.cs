﻿using System.Linq;

namespace Smartstore.Core.Checkout.GiftCards.Extensions
{
    public static class GiftCardExtensions
    {
        /// <summary>
        /// Gets the gift cards remaining value
        /// </summary>
        public static decimal GetRemainingValue(this GiftCard giftCard)
        {
            var result = giftCard.Amount - giftCard.GiftCardUsageHistory.Sum(x => x.UsedValue);
            return result < decimal.Zero
                ? decimal.Zero
                : result;
        }
    }
}