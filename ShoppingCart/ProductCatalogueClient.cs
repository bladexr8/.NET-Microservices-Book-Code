namespace ShoppingCart
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Threading;
    using Newtonsoft.Json;
    using Polly;
    using ShoppingCart;

    public class ProductCatalogueClient : IProductCatalogueClient
    {
        // URL information for Products Microservice calls
        private static string productCatalogBaseUrl = 
            @"http://private-05cc8-chapter2productcataloguemicroservice.apiary-mock.com";
        private static string getProductPathTemplate = "/products?productIds=[{0}]";

        // Fault Handling Policy
        private static Policy exponentialRetryPolicy =
        Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
          3,
          attempt => TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)), (ex, _) => Console.WriteLine(ex.ToString()));


        // Call Products Microservice to get a list of Products
        private static async Task<HttpResponseMessage>
            RequestProductFromProductCatalogue(int[] productCatalogueIds) {
            // add ProductIds as query string parameter
            var productsResource = string.Format(
                getProductPathTemplate, string.Join(",", productCatalogueIds));
            // create client for making http GET request
            using (var httpClient = new HttpClient()) {
                httpClient.BaseAddress = new Uri(productCatalogBaseUrl);
                // perform the GET asynchronously
                return await
                    httpClient.GetAsync(productsResource).ConfigureAwait(false);
            }
        }

        // Parse response from Products Microservice
        private static async Task<IEnumerable<ShoppingCartItem>> ConvertToShoppingCartItems(HttpResponseMessage response) {
            response.EnsureSuccessStatusCode();
            // use Json.NET to deserialize the JSON returned from
            // Product Catalogue microservice
            var products =
                JsonConvert.DeserializeObject<List<ProductCatalogueProduct>>(
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            return
                products
                    .Select(p => new ShoppingCartItem(
                        int.Parse(p.ProductId),
                        p.ProductName,
                        p.ProductDescription,
                        p.Price
                    ));
        }

        private class ProductCatalogueProduct {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductDescription { get; set; }
            public Money Price { get; set; }
        }

        // Request Product Information and parse the response
        private async Task<IEnumerable<ShoppingCartItem>>
        GetItemsFromCatalogueService(int[] productCatalogueIds) {
            var response = await
                RequestProductFromProductCatalogue(productCatalogueIds)
                .ConfigureAwait(false);
            return await ConvertToShoppingCartItems(response)
                .ConfigureAwait(false);
        }

        // public facing method
        public Task<IEnumerable<ShoppingCartItem>>
            GetShoppingCartItems(int[] productCatalogueIds) =>
                exponentialRetryPolicy
                .ExecuteAsync(async () => await GetItemsFromCatalogueService(productCatalogueIds).ConfigureAwait(false));
    }
}
