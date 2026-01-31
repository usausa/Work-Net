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

// Phase 2 Test Models
public class ConstantSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ConstantDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BeforeAfterSource
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class BeforeAfterDestination
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool BeforeMapCalled { get; set; }
    public bool AfterMapCalled { get; set; }
}

public class ExtendedTypeConversionSource
{
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
    public decimal DecimalValue { get; set; }
    public bool BoolValue { get; set; }
    public DateTime DateTimeValue { get; set; }
    public string GuidString { get; set; } = string.Empty;
}

public class ExtendedTypeConversionDestination
{
    public string LongValue { get; set; } = string.Empty;
    public string DoubleValue { get; set; } = string.Empty;
    public string DecimalValue { get; set; } = string.Empty;
    public string BoolValue { get; set; } = string.Empty;
    public string DateTimeValue { get; set; } = string.Empty;
    public Guid GuidString { get; set; }
}

public class NumericConversionSource
{
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
}

public class NumericConversionDestination
{
    public long IntValue { get; set; }
    public int LongValue { get; set; }
    public decimal DoubleValue { get; set; }
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

    // Phase 2: Constant value mapping
    [Mapper]
    [MapConstant("Status", "Active")]
    [MapConstant("Version", 1)]
    [MapConstant("CreatedAt", null, Expression = "System.DateTime.Now")]
    public static partial void Map(ConstantSource source, ConstantDestination destination);

    // Phase 2: BeforeMap and AfterMap
    [Mapper]
    [BeforeMap(nameof(OnBeforeMap))]
    [AfterMap(nameof(OnAfterMap))]
    public static partial void Map(BeforeAfterSource source, BeforeAfterDestination destination);

    private static void OnBeforeMap(BeforeAfterSource source, BeforeAfterDestination destination)
    {
        destination.BeforeMapCalled = true;
    }

    private static void OnAfterMap(BeforeAfterSource source, BeforeAfterDestination destination)
    {
        destination.AfterMapCalled = true;
    }

    // Phase 2: Extended type conversions
    [Mapper]
    public static partial void Map(ExtendedTypeConversionSource source, ExtendedTypeConversionDestination destination);

    // Phase 2: Numeric conversions
    [Mapper]
    public static partial void Map(NumericConversionSource source, NumericConversionDestination destination);
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

// Phase 2 Tests
public class ConstantMappingTests
{
    [Fact]
    public void Map_ConstantValues_SetsConstantsCorrectly()
    {
        // Arrange
        var source = new ConstantSource
        {
            Id = 1,
            Name = "Test"
        };
        var destination = new ConstantDestination();
        var beforeMap = DateTime.Now;

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(1, destination.Id);
        Assert.Equal("Test", destination.Name);
        Assert.Equal("Active", destination.Status);
        Assert.Equal(1, destination.Version);
        Assert.True(destination.CreatedAt >= beforeMap);
        Assert.True(destination.CreatedAt <= DateTime.Now);
    }
}

public class BeforeAfterMapTests
{
    [Fact]
    public void Map_BeforeAfterMap_CallsBothMethods()
    {
        // Arrange
        var source = new BeforeAfterSource
        {
            Value = 42,
            Text = "Hello"
        };
        var destination = new BeforeAfterDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(42, destination.Value);
        Assert.Equal("Hello", destination.Text);
        Assert.True(destination.BeforeMapCalled);
        Assert.True(destination.AfterMapCalled);
    }
}

public class ExtendedTypeConversionTests
{
    [Fact]
    public void Map_ExtendedTypeConversions_ConvertsCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var dateTime = new DateTime(2024, 1, 15, 10, 30, 0);
        var source = new ExtendedTypeConversionSource
        {
            LongValue = 1234567890L,
            DoubleValue = 3.14159,
            DecimalValue = 99.99m,
            BoolValue = true,
            DateTimeValue = dateTime,
            GuidString = guid.ToString()
        };
        var destination = new ExtendedTypeConversionDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("1234567890", destination.LongValue);
        Assert.Contains("3.14159", destination.DoubleValue);
        Assert.Contains("99.99", destination.DecimalValue);
        Assert.Equal("True", destination.BoolValue);
        Assert.Equal(dateTime.ToString(), destination.DateTimeValue);
        Assert.Equal(guid, destination.GuidString);
    }
}

public class NumericConversionTests
{
    [Fact]
    public void Map_NumericConversions_ConvertsCorrectly()
    {
        // Arrange
        var source = new NumericConversionSource
        {
            IntValue = 100,
            LongValue = 200L,
            DoubleValue = 3.5
        };
        var destination = new NumericConversionDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(100L, destination.IntValue);
        Assert.Equal(200, destination.LongValue);
        Assert.Equal(3.5m, destination.DoubleValue);
    }
}

#endregion
