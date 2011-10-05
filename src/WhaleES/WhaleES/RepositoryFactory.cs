using WhaleES.Configuration;

namespace WhaleES
{
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
        public static Repository<T> CreateRepositoryFor<T>() where T:new()
        {
            ConfigureWhaleEs.AssertConfigurationIsValid();
            var amazonClient = ConfigureWhaleEs.CurrentConfig.AmazonClient;
            var eventSource = new StreamOfEventsFor<T>(amazonClient, ConfigureWhaleEs.CurrentConfig.BucketName,ConfigureWhaleEs.CurrentConfig.Serializer);
            return new Repository<T>(eventSource);
        } 
    }
}