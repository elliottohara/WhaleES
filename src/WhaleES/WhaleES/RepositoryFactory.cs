using WhaleES.Configuration;

namespace WhaleES
{
    /// <summary>
    /// Factory to get instances of IStreamOfEventsFor
    /// </summary>
    public static class EventSteamFactory
    {
        /// <summary>
        /// Returns the events stream for <see cref="TAggrigateRoot"/>
        /// </summary>
        /// <typeparam name="TAggrigateRoot">The type Aggrigate that the stream applies to</typeparam>
        /// <returns></returns>
        public static IStreamOfEventsFor<TAggrigateRoot>  StreamOfEventsFor<TAggrigateRoot>() where TAggrigateRoot : new()
        {
            ConfigureWhaleEs.AssertConfigurationIsValid();
            var amazonClient = ConfigureWhaleEs.CurrentConfig.AmazonClient;
            return new CachingEventStream<TAggrigateRoot>(amazonClient, ConfigureWhaleEs.CurrentConfig.BucketName, ConfigureWhaleEs.CurrentConfig.Serializer,ConfigureWhaleEs.CurrentConfig.Cache);
        } 
    } 
    /// <summary>
    /// Factory for getting instances of <see cref="Repository{T}"/>
    /// </summary>
    public static class RepositoryFactory
    {
        /// <summary>
        /// Returns Repository for <see cref="T"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Repository<T> CreateRepositoryFor<T>() where T:class,new()
        {
            var eventSource = EventSteamFactory.StreamOfEventsFor<T>();
            return new Repository<T>(eventSource);
        } 
    }
}