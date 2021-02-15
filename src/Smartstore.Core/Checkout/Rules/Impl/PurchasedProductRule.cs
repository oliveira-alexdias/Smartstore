﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Rules;

namespace Smartstore.Core.Checkout.Rules.Impl
{
    public class PurchasedProductRule : IRule
    {
        private readonly SmartDbContext _db;

        public PurchasedProductRule(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
        {
            var query = _db.Orders
                .AsNoTracking()
                .Include(x => x.OrderItems)
                .ApplyStandardFilter(context.Customer.Id, context.Store.Id)
                .SelectMany(x => x.OrderItems);

            if (expression.Operator == RuleOperator.In || expression.Operator == RuleOperator.NotIn)
            {
                // Find match using LINQ to Entities.
                var productIds = expression.Value as List<int>;
                if (!(productIds?.Any() ?? false))
                {
                    return true;
                }

                if (expression.Operator == RuleOperator.In)
                {
                    return await query.Where(oi => productIds.Contains(oi.ProductId)).AnyAsync();
                }

                return await query.Where(oi => !productIds.Contains(oi.ProductId)).AnyAsync();
            }
            else
            {
                // Find match using LINQ to Objects.
                var productIds = new HashSet<int>();
                var pager = query.ToFastPager(4000);

                while ((await pager.ReadNextPageAsync(x => new { x.Id, x.ProductId }, x => x.Id)).Out(out var orderItems))
                {
                    productIds.AddRange(orderItems.Select(x => x.ProductId));
                }

                var match = expression.HasListsMatch(productIds);
                return match;
            }
        }
    }
}