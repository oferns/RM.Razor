namespace RM.Razor.Tests {

    using Xunit;

    public class PublicHashCodeCombinerTests {

        [Fact]
        public void GivenTheSameInputs_ItProducesTheSameOutput() {
            var hashCode1 = new PublicHashCodeCombiner();
            var hashCode2 = new PublicHashCodeCombiner();

            hashCode1.Add(42);
            hashCode1.Add("foo");
            hashCode2.Add(42);
            hashCode2.Add("foo");

            Assert.Equal(hashCode1.CombinedHash, hashCode2.CombinedHash);
        }

        [Fact]
        public void HashCode_Is_OrderSensitive() {
            var hashCode1 = PublicHashCodeCombiner.Start();
            var hashCode2 = PublicHashCodeCombiner.Start();

            hashCode1.Add(42);
            hashCode1.Add("foo");

            hashCode2.Add("foo");
            hashCode2.Add(42);

            Assert.NotEqual(hashCode1.CombinedHash, hashCode2.CombinedHash);
        }
    }
}