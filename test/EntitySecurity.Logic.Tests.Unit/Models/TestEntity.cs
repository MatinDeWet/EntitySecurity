namespace EntitySecurity.Logic.Tests.Unit.Models
{
    public class TestEntity : ITestInterface
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
