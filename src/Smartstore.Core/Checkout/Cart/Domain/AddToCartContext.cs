﻿using System.Collections.Generic;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Customers;

namespace Smartstore.Core.Checkout.Cart
{
    public class AddToCartContext
    {
        /// <summary>
        /// Gets or sets warnings
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets or sets shopping cart item
        /// </summary>
        public ShoppingCartItem Item { get; set; }

        /// <summary>
        /// Gets or sets child shopping cart items
        /// </summary>
        public List<ShoppingCartItem> ChildItems { get; set; } = new();

        /// <summary>
        /// Gets or sets product bundle item
        /// </summary>
        public ProductBundleItem BundleItem { get; set; }

        /// <summary>
        /// Gets or sets the customer
        /// </summary>
        public Customer Customer { get; set; }

        /// <summary>
        /// Gets or sets the product
        /// </summary>
        public Product Product { get; set; }

        /// <summary>
        /// Gets or sets the shopping cart type
        /// </summary>
        public ShoppingCartType CartType { get; set; }

        // TODO: (ms) (core) implement this. Needs ProductVariantQuery
        //public ProductVariantQuery VariantQuery { get; set; }

        /// <summary>
        /// Gets or sets the raw attributes string
        /// </summary>
        public string RawAttributes { get; set; }

        /// <summary>
        /// Gets or sets the price entered by customer
        /// </summary>
        public decimal CustomerEnteredPrice { get; set; }

        /// <summary>
        /// Gets or sets the quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to add required products
        /// </summary>
        public bool AddRequiredProducts { get; set; }

        /// <summary>
        /// Gets or sets store identifier
        /// </summary>
        public int? StoreId { get; set; }

        /// <summary>
        /// Gets bundle item id
        /// </summary>
        public int BundleItemId 
            => BundleItem is null ? 0 : BundleItem.Id;
    }
}