namespace WhaleES.Integration.Test
{
    public class StandardEvent
    {
    }
    public interface IHaveAnId
    {
        string Id { get; set; }
    }
    public class EventWithId:IHaveAnId{
        public string Id { get; set; }
    }
    public class SpecialEvent:IHaveAnId{
        public string Id { get; set; }
        public string OtherThing { get; set; }
    }
}