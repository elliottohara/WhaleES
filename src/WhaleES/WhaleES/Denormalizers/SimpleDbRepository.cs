using System;
using System.Collections.Generic;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Attribute = Amazon.SimpleDB.Model.Attribute;

namespace WhaleES.Denormalizers
{
    public class SimpleDbRepository<TViewModel> where TViewModel : new()
    {
        private readonly AmazonSimpleDB _client;

        public SimpleDbRepository(AmazonSimpleDB client)
        {
            _client = client;
            _client.EnsureDomain<TViewModel>();
        }

        public virtual TViewModel New()
        {
            return new TViewModel();
        }
        public IEnumerable<TViewModel> Get()
        {
            return Get("select * from `" + _client.DomainName<TViewModel>() + "`");
        }
        public IEnumerable<TViewModel>  Get(String query)
        {
            var request =
                new SelectRequest().WithSelectExpression(query);
            var results = _client.Select(request).SelectResult;
            foreach (var item in results.Item)
            {
                var thing = New();
                Map(thing, item.Attribute);
                yield return thing;

            }
        }
        //public IEnumerable<TViewModel> Get(Expression<Func<TViewModel,bool>> predicate )
        //{
        //    var query = new QueryTranslator<TViewModel>().Translate(predicate);
        //    return Get(query);
        //}

        public TViewModel Get(object id)
        {
            var request =
                new GetAttributesRequest().WithDomainName(_client.DomainName<TViewModel>()).WithItemName(id.ToString());
            var result = _client.GetAttributes(request);
            var viewModel = New();
            Map(viewModel, result.GetAttributesResult.Attribute);
            return viewModel;
        }

        private static void Map(TViewModel viewModel, List<Attribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                try
                {
                    var property = viewModel.GetType().GetProperty(attribute.Name);
                    var expectedType = property.PropertyType;
                    var valueToSet = TypeConverter.ChangeType(attribute.Value, expectedType);
                    property.SetValue(viewModel, valueToSet, null);
                }
                catch
                {
                    continue;
                }
            }
        }
    }
    public static class TypeConverter
    {
        public static object ChangeType(object original,Type expected)
        {
            if (expected == typeof(Guid)) return ConvertGuid(original);
            return Convert.ChangeType(original, expected);
        }
        private static Guid ConvertGuid(object original)
        {
            return Guid.Parse(original.ToString());
        }
    }
}