using System;
using System.Linq.Expressions;

namespace WhaleES.Configuration
{
    public class ExampleArConfigurationBuilder<ExampleAggrigateRoot>
    {
        private readonly Configuration _configuration;
        private ReflectionAggrigateRootConfiguration _reflectionConfigurationBuilder;

        public ExampleArConfigurationBuilder(Configuration configuration)
        {
            _configuration = configuration;
            _reflectionConfigurationBuilder = new ReflectionAggrigateRootConfiguration(_configuration);
        }

        public ExampleArConfigurationBuilder<ExampleAggrigateRoot> ApplyMethodIs<TEvent>(Expression<Action<ExampleAggrigateRoot, TEvent>> applyMethod)
        {
            var name = GetMethodCallName(applyMethod);
            
            _reflectionConfigurationBuilder.ApplyMethodNameIs(name);
            return this;
        }

        private string GetMethodCallName(LambdaExpression method)
        {
            var methodCall = method.Body as MethodCallExpression;
            //if(methodCall == null) throw new ConfigurationErrorsExpression()
            return methodCall.Method.Name;
            
        }

        public ExampleArConfigurationBuilder<ExampleAggrigateRoot> GetUnCommittedEventsBy<TEventsCollection>(Expression<Func<ExampleAggrigateRoot, TEventsCollection>> getEvents)
        {
            var lambda = getEvents as LambdaExpression;
            var memberExpression = lambda.Body as MemberExpression;
            //
            var body = memberExpression.Member.Name;
            _reflectionConfigurationBuilder.UncommitedEventsGetMethodNameIs(body);
            return this;
        } 
        public ExampleArConfigurationBuilder<ExampleAggrigateRoot> StartReplayBy(Expression<Action<ExampleAggrigateRoot>> startReplay)
        {
            var name = GetMethodCallName(startReplay);
            _reflectionConfigurationBuilder.CallMethodToStartReplay(name);
            return this;
        }
        public ExampleArConfigurationBuilder<ExampleAggrigateRoot> StopReplayBy(Expression<Action<ExampleAggrigateRoot>> stopReplay )
        {
            var name = GetMethodCallName(stopReplay);
            _reflectionConfigurationBuilder.CallMethodToEndReplay(name);
            return this;
        } 
        public Configuration And()
        {
            return _configuration;
        }
    }
}