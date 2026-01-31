namespace MapperLibrary;

#region Test Models

public class BasicSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BasicDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class DifferentPropertySource
{
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
}

public class DifferentPropertyDestination
{
    public int DestId { get; set; }
    public string DestName { get; set; } = string.Empty;
}

public class IgnoreSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

public class IgnoreDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

public class TypeConversionSource
{
    public int IntValue { get; set; }
    public string StringValue { get; set; } = string.Empty;
}

public class TypeConversionDestination
{
    public string IntValue { get; set; } = string.Empty;
    public int StringValue { get; set; }
}

#endregion

#region Mappers

internal static partial class TestMappers
{
    // Basic mapping: same property names
    [Mapper]
    public static partial void Map(BasicSource source, BasicDestination destination);

    // Basic mapping with return type
    [Mapper]
    public static partial BasicDestination MapToNew(BasicSource source);

    // Different property names mapping
    [Mapper]
    [MapProperty("SourceId", "DestId")]
    [MapProperty("SourceName", "DestName")]
    public static partial void Map(DifferentPropertySource source, DifferentPropertyDestination destination);

    // Different property names mapping with return type
    [Mapper]
    [MapProperty("SourceId", "DestId")]
    [MapProperty("SourceName", "DestName")]
    public static partial DifferentPropertyDestination MapToNew(DifferentPropertySource source);

    // Ignore property mapping
    [Mapper]
    [MapIgnore("Secret")]
    public static partial void Map(IgnoreSource source, IgnoreDestination destination);

    // Type conversion mapping
    [Mapper]
    public static partial void Map(TypeConversionSource source, TypeConversionDestination destination);
}

#endregion

#region Tests

public class BasicMappingTests
{
    [Fact]
    public void Map_BasicProperties_CopiesAllProperties()
    {
        // Arrange
        var source = new BasicSource
        {
            Id = 42,
            Name = "Test Name",
            Description = "Test Description"
        };
        var destination = new BasicDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(42, destination.Id);
        Assert.Equal("Test Name", destination.Name);
        Assert.Equal("Test Description", destination.Description);
    }

    [Fact]
    public void MapToNew_BasicProperties_ReturnsNewObjectWithCopiedProperties()
    {
        // Arrange
        var source = new BasicSource
        {
            Id = 100,
            Name = "New Object",
            Description = "Created via MapToNew"
        };

        // Act
        var destination = TestMappers.MapToNew(source);

        // Assert
        Assert.NotNull(destination);
        Assert.Equal(100, destination.Id);
        Assert.Equal("New Object", destination.Name);
        Assert.Equal("Created via MapToNew", destination.Description);
    }
}

public class DifferentPropertyMappingTests
{
    [Fact]
    public void Map_DifferentPropertyNames_MapsCorrectly()
    {
        // Arrange
        var source = new DifferentPropertySource
        {
            SourceId = 123,
            SourceName = "Different Name"
        };
        var destination = new DifferentPropertyDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(123, destination.DestId);
        Assert.Equal("Different Name", destination.DestName);
    }

    [Fact]
    public void MapToNew_DifferentPropertyNames_ReturnsCorrectlyMappedObject()
    {
        // Arrange
        var source = new DifferentPropertySource
        {
            SourceId = 456,
            SourceName = "Another Name"
        };

        // Act
        var destination = TestMappers.MapToNew(source);

        // Assert
        Assert.NotNull(destination);
        Assert.Equal(456, destination.DestId);
        Assert.Equal("Another Name", destination.DestName);
    }
}

public class IgnorePropertyMappingTests
{
    [Fact]
    public void Map_IgnoredProperty_DoesNotCopyIgnoredProperty()
    {
        // Arrange
        var source = new IgnoreSource
        {
            Id = 1,
            Name = "Public",
            Secret = "TopSecret"
        };
        var destination = new IgnoreDestination
        {
            Secret = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(1, destination.Id);
        Assert.Equal("Public", destination.Name);
        Assert.Equal("Original", destination.Secret); // Should not be overwritten
    }
}

public class TypeConversionMappingTests
{
    [Fact]
    public void Map_TypeConversion_ConvertsTypes()
    {
        // Arrange
        var source = new TypeConversionSource
        {
            IntValue = 999,
            StringValue = "123"
        };
        var destination = new TypeConversionDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("999", destination.IntValue);
        Assert.Equal(123, destination.StringValue);
    }
}

#endregion
