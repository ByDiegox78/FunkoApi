using System.Text.Json;
using FluentAssertions;
using FunkoApiV._2.Converters;
using MongoDB.Bson;

namespace FunkoApiV2Test.Unit.Mappers;

[TestFixture]
[Category("Unit")]
public class ObjectIdJsonConverterTests {
    private static JsonSerializerOptions Options {
        get {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ObjectIdJsonConverter());
            return options;
        }
    }

    [Test]
    public void Read_valorHexadecimal_deserializaObjectId() {
        // Arrange
        const string value = "672f1a8b3c9d4e5f6a7b8c9d";

        // Act
        var result = JsonSerializer.Deserialize<ObjectId>($"\"{value}\"", Options);

        // Assert
        result.Should().Be(ObjectId.Parse(value));
    }

    [Test]
    public void Read_valorVacio_devuelveObjectIdVacio() {
        // Act
        var result = JsonSerializer.Deserialize<ObjectId>("\"\"", Options);

        // Assert
        result.Should().Be(ObjectId.Empty);
    }

    [Test]
    public void Write_serializaObjectIdComoCadenaHexadecimal() {
        // Arrange
        var objectId = ObjectId.Parse("672f1a8b3c9d4e5f6a7b8c9d");

        // Act
        var result = JsonSerializer.Serialize(objectId, Options);

        // Assert
        result.Should().Be($"\"{objectId}\"");
    }
}
