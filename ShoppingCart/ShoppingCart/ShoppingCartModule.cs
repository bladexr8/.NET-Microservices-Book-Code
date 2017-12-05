using System;
namespace ShoppingCart.ShoppingCart
{
    using EventFeed;
    using Nancy;
    using Nancy.ModelBinding;

    public class ShoppingCartModule : NancyModule
    {
        public ShoppingCartModule(IShoppingCartStore shoppingCartStore,
                                 IProductCatalogueClient productCatalog,
                                 IEventStore eventStore)
            : base("/shoppingcart")
        {
            // route declaration
            Get("/{userid:int}", parameters =>
            {
                // route handler
                var userId = (int)parameters.userid;
                return shoppingCartStore.Get(userId);
            });

            // route declaration
            Post("/{userid:int}/items",
                 // use async to handle call to Product Catalog microservice
                 async (parameters, _) =>
                 {
                     // read and de-serialise array of product id's in http request body
                     var productCatalogIds = this.Bind<int[]>();
                     var userId = (int)parameters.userid;

                     // fetch product information from Product Catalog microservice
                     var shoppingCart = shoppingCartStore.Get(userId);
                     // async call to ProductCatalog microservice
                     var shoppingCartItems = await
                         productCatalog
                         .GetShoppingCartItems(productCatalogIds)
                         .ConfigureAwait(false);
                     // code resumes here after async call
                     // add items to cart     
                     shoppingCart.AddItems(shoppingCartItems, eventStore);
                     shoppingCartStore.Save(shoppingCart);
                     return shoppingCart;
                 });

            // route declaration
            Delete("/{userid:int}/items", parameters =>
            {
                // read and de-serialise array of product id's in http request body
                var productCatalogIds = this.Bind<int[]>();
                var userId = (int)parameters.userid;
                var shoppingCart = shoppingCartStore.Get(userId);
                shoppingCart.RemoveItems(productCatalogIds, eventStore);
                shoppingCartStore.Save(shoppingCart);
                return shoppingCart;
            });
        }
    }
}
