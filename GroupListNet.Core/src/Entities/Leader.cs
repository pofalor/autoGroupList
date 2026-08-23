namespace GroupListNet.Core.src.Entities
{
    public class Leader : PersistentEntity
    {
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
    }
}
