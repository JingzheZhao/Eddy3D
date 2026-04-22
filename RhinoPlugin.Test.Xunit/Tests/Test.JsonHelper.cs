using EddyLib.Helpers;
using Xunit;
using Newtonsoft.Json;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "Unit")]
    public class JsonHelperTests
    {
        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void Serialize_SimpleObject_ReturnsValidJson()
        {
            var obj = new TestObject { Id = 1, Name = "Test" };
            var json = JsonHelper.Serialize(obj);
            Assert.Contains("\"Id\": 1", json);
            Assert.Contains("\"Name\": \"Test\"", json);
        }

        [Fact]
        public void Serialize_Null_ReturnsNullString()
        {
            var json = JsonHelper.Serialize<TestObject>(null);
            Assert.Equal("null", json);
        }

        [Fact]
        public void Deserialize_ValidJson_ReturnsObject()
        {
            var json = "{ \"Id\": 1, \"Name\": \"Test\" }";
            var obj = JsonHelper.Deserialize<TestObject>(json);
            Assert.NotNull(obj);
            Assert.Equal(1, obj.Id);
            Assert.Equal("Test", obj.Name);
        }

        [Fact]
        public void Deserialize_NullOrWhitespace_ReturnsDefault()
        {
            Assert.Null(JsonHelper.Deserialize<TestObject>(null));
            Assert.Null(JsonHelper.Deserialize<TestObject>(""));
            Assert.Null(JsonHelper.Deserialize<TestObject>("   "));
        }

        [Fact]
        public void Deserialize_InvalidJsonStructure_ReturnsDefault()
        {
            // JsonHelper.Deserialize expects { or [ at start/end
            Assert.Null(JsonHelper.Deserialize<TestObject>("invalid"));
            Assert.Null(JsonHelper.Deserialize<TestObject>("123"));
        }

        [Fact]
        public void Deserialize_MalformedJson_Throws()
        {
            // Starts with { and ends with }, but content is invalid
            var json = "{ invalid: json }";
            Assert.Throws<JsonReaderException>(() => JsonHelper.Deserialize<TestObject>(json));
        }

        [Fact]
        public void Deserialize_SurroundedByWhitespace_TrimsAndDeserializes()
        {
            var json = "  { \"Id\": 1, \"Name\": \"Test\" }  ";
            var obj = JsonHelper.Deserialize<TestObject>(json);
            Assert.NotNull(obj);
            Assert.Equal(1, obj.Id);
        }

        [Fact]
        public void TryDeserialize_ValidJson_ReturnsTrueAndObject()
        {
            var json = "{ \"Id\": 1, \"Name\": \"Test\" }";
            var success = JsonHelper.TryDeserialize<TestObject>(json, out var obj);
            Assert.True(success);
            Assert.NotNull(obj);
            Assert.Equal(1, obj.Id);
        }

        [Fact]
        public void TryDeserialize_InvalidStructure_ReturnsFalse()
        {
            var json = "invalid";
            var success = JsonHelper.TryDeserialize<TestObject>(json, out var obj);
            Assert.False(success);
            Assert.Null(obj);
        }

        [Fact]
        public void TryDeserialize_MalformedJson_ReturnsFalse()
        {
            // Although it starts with { and ends with }, the content is invalid
            var json = "{ invalid: json }";
            var success = JsonHelper.TryDeserialize<TestObject>(json, out var obj);
            Assert.False(success);
        }
    }
}
