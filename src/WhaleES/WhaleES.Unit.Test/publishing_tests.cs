using NUnit.Framework;
using Rhino.Mocks;
using WhaleES.Configuration;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class publishing_tests
    {
         public void if_configured_publish_method_executes()
         {
             var published = false;
             ConfigureWhaleEs.With().PublishEventsWith(events => published = true);
             var stream = MockRepository.GenerateMock<IStreamOfEventsFor<StandardARWithPublicMethods>>();
             var repo = new Repository<StandardARWithPublicMethods>(stream);
             
             var ar = new StandardARWithPublicMethods();
             ar.AddEvent(new StandardEvent());
             repo.Put("someid",ar);

             Assert.True(published);
         }
    }
}