using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;

namespace WhaleES.Denormalizers
{
    public static class SimpleDbExtensionMethods
    {
        private static List<String> existingDomains = new List<string>();

        public static string DomainName<T>(this AmazonSimpleDB client)
        {
            return typeof (T).FullName;
        }

        public static void EnsureDomain<T>(this AmazonSimpleDB client)
        {
            if (existingDomains.Contains(client.DomainName<T>())) return;
            var createDomainRequest = new CreateDomainRequest()
                .WithDomainName(client.DomainName<T>());
            //TODO: add error handling
            client.CreateDomain(createDomainRequest);
            existingDomains.Add(client.DomainName<T>());

        }

    }

    /// <summary>
    /// A nice little class to denormalize to SimpleDb, just inherit it and mark up with your IHandle interface. Call Put and Delete as appropriate
    /// </summary>
    /// <typeparam name="TReadModel"></typeparam>
    public abstract class SimpleDbDenormalizer<TReadModel>
    {
        private readonly AmazonSimpleDB _client;
        // I know how static fields in Generic Types work...

        protected SimpleDbDenormalizer(AmazonSimpleDB client)
        {
            _client = client;
            _client.EnsureDomain<TReadModel>();

        }

        protected abstract Func<TReadModel, object> IdProperty { get; }

        public void Delete(object id)
        {
            var deleteRequest =
                new DeleteAttributesRequest().WithDomainName(_client.DomainName<TReadModel>()).WithItemName(
                    id.ToString());
            _client.DeleteAttributes(deleteRequest);
        }

        public void Put(TReadModel readModel)
        {
            var properties = typeof (TReadModel).GetProperties();

            var putRequest = new PutAttributesRequest()
                .WithDomainName(_client.DomainName<TReadModel>())
                .WithItemName(IdProperty(readModel).ToString());

            foreach (var propertyInfo in properties)
            {
                var replaceableAttibute = new ReplaceableAttribute()
                    .WithName(propertyInfo.Name)
                    .WithValue(propertyInfo.GetValue(readModel, null).ToString())
                    .WithReplace(true);

                putRequest.WithAttribute(replaceableAttibute);
            }
            _client.PutAttributes(putRequest);
        }


    }
}    
    
