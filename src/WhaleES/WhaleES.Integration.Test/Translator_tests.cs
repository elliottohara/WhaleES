using System;
using System.Linq.Expressions;
using NUnit.Framework;
using WhaleES.Denormalizers;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class Translator_tests
    {
        [Test]
        public void test()
        {
            var guid = Guid.NewGuid();
            Expression<Func<SimpleDbTestEntity, bool>> predicate = te => te.SomeProperty == "blah" && te.Id == guid;
            Console.WriteLine(new QueryTranslator<SimpleDbTestEntity>().Translate(predicate));

        }
    }
}