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

// Nested mapping test models
public class FlatSource
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public int Value3 { get; set; }
}

public class DestinationChild
{
    public int Value { get; set; }
}

public class NestedDestination
{
    public DestinationChild? Child1 { get; set; }
    public DestinationChild? Child2 { get; set; }
    public DestinationChild? Child3 { get; set; }
}

// Source with nested properties (for flatten test)
public class NestedSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NestedSource
{
    public NestedSourceChild? Child { get; set; }
    public int DirectValue { get; set; }
}

public class FlatDestination
{
    public int ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public int DirectValue { get; set; }
}


// Deep nested test models
public class DeepNestedChild
{
    public int Value { get; set; }
}

public class DeepNestedParent
{
    public DeepNestedChild? Inner { get; set; }
}

public class DeepNestedDestination
{
    public DeepNestedParent? Outer { get; set; }
}

public class DeepSource
{
    public int DeepValue { get; set; }
}

// Null handling test models - Nested source with nullable child
public class NullableNestedSourceChild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class NullableNestedSource
{
    public NullableNestedSourceChild? Child { get; set; }
    public int DirectValue { get; set; }
}

public class NullableNestedFlatDestination
{
    public int ChildId { get; set; }
    public string ChildName { get; set; } = string.Empty;
    public int DirectValue { get; set; }
}

// Null handling test models - Simple nullable properties
public class NullablePropertySource
{
    public string? NullableName { get; set; }
    public int? NullableInt { get; set; }
    public string NonNullableName { get; set; } = string.Empty;
}


public class NullablePropertyDestination
{
    public string? NullableName { get; set; }
    public int? NullableInt { get; set; }
    public string NonNullableName { get; set; } = "default";
}

// Null handling - nullable to non-nullable
public class NullableToNonNullableSource
{
    public string? Name { get; set; }
}

public class NullableToNonNullableDestination
{
    public string Name { get; set; } = "original";
}

// Null handling - nullable int to string
public class NullableIntToStringSource
{
    public int? IntValue { get; set; }
}

public class NullableIntToStringDestination
{
    public string IntValue { get; set; } = "original";
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

    // Nested mapping: flat source to nested destination
    [Mapper]
    [MapProperty("Value1", "Child1.Value")]
    [MapProperty("Value2", "Child2.Value")]
    [MapProperty("Value3", "Child3.Value")]
    public static partial void Map(FlatSource source, NestedDestination destination);

    // Nested mapping: flat source to nested destination with return type
    [Mapper]
    [MapProperty("Value1", "Child1.Value")]
    [MapProperty("Value2", "Child2.Value")]
    [MapProperty("Value3", "Child3.Value")]
    public static partial NestedDestination MapToNew(FlatSource source);

    // Nested mapping: nested source to flat destination (flatten)
    [Mapper]
    [MapProperty("Child.Id", "ChildId")]
    [MapProperty("Child.Name", "ChildName")]
    public static partial void Map(NestedSource source, FlatDestination destination);

    // Deep nested mapping
    [Mapper]
    [MapProperty("DeepValue", "Outer.Inner.Value")]
    public static partial void Map(DeepSource source, DeepNestedDestination destination);

    // Deep nested mapping with return type
    [Mapper]
    [MapProperty("DeepValue", "Outer.Inner.Value")]
    public static partial DeepNestedDestination MapToNew(DeepSource source);

    // Null handling: nested source to flat destination (with nullable source child)
    [Mapper]
    [MapProperty("Child.Id", "ChildId")]
    [MapProperty("Child.Name", "ChildName")]
    public static partial void Map(NullableNestedSource source, NullableNestedFlatDestination destination);

    // Null handling: simple nullable properties
    [Mapper]
    public static partial void Map(NullablePropertySource source, NullablePropertyDestination destination);

    // Null handling: nullable to non-nullable
    [Mapper]
    public static partial void Map(NullableToNonNullableSource source, NullableToNonNullableDestination destination);

    // Null handling: nullable int to non-nullable string
    [Mapper]
    public static partial void Map(NullableIntToStringSource source, NullableIntToStringDestination destination);
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

// Nested mapping tests
public class NestedMappingTests
{
    [Fact]
    public void Map_FlatToNested_MapsToNestedProperties()
    {
        // Arrange
        var source = new FlatSource
        {
            Value1 = 10,
            Value2 = 20,
            Value3 = 30
        };
        var destination = new NestedDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.NotNull(destination.Child1);
        Assert.NotNull(destination.Child2);
        Assert.NotNull(destination.Child3);
        Assert.Equal(10, destination.Child1.Value);
        Assert.Equal(20, destination.Child2.Value);
        Assert.Equal(30, destination.Child3.Value);
    }

    [Fact]
    public void MapToNew_FlatToNested_ReturnsNestedObject()
    {
        // Arrange
        var source = new FlatSource
        {
            Value1 = 100,
            Value2 = 200,
            Value3 = 300
        };

        // Act
        var destination = TestMappers.MapToNew(source);

        // Assert
        Assert.NotNull(destination);
        Assert.NotNull(destination.Child1);
        Assert.NotNull(destination.Child2);
        Assert.NotNull(destination.Child3);
        Assert.Equal(100, destination.Child1.Value);
        Assert.Equal(200, destination.Child2.Value);
        Assert.Equal(300, destination.Child3.Value);
    }

    [Fact]
    public void Map_NestedToFlat_FlattensProperties()
    {
        // Arrange
        var source = new NestedSource
        {
            Child = new NestedSourceChild
            {
                Id = 42,
                Name = "Test"
            },
            DirectValue = 999
        };
        var destination = new FlatDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal(42, destination.ChildId);
        Assert.Equal("Test", destination.ChildName);
        Assert.Equal(999, destination.DirectValue);
    }

    [Fact]
    public void Map_DeepNested_MapsToDeepNestedProperties()
    {
        // Arrange
        var source = new DeepSource
        {
            DeepValue = 12345
        };
        var destination = new DeepNestedDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.NotNull(destination.Outer);
        Assert.NotNull(destination.Outer.Inner);
        Assert.Equal(12345, destination.Outer.Inner.Value);
    }

    [Fact]
    public void MapToNew_DeepNested_ReturnsDeepNestedObject()
    {
        // Arrange
        var source = new DeepSource
        {
            DeepValue = 67890
        };

        // Act
        var destination = TestMappers.MapToNew(source);

        // Assert
        Assert.NotNull(destination);
        Assert.NotNull(destination.Outer);
        Assert.NotNull(destination.Outer.Inner);
        Assert.Equal(67890, destination.Outer.Inner.Value);
    }

    [Fact]
    public void Map_FlatToNested_PreservesExistingNestedObjects()
    {
        // Arrange
        var source = new FlatSource
        {
            Value1 = 10,
            Value2 = 20,
            Value3 = 30
        };
        var existingChild1 = new DestinationChild { Value = 999 };
        var destination = new NestedDestination
        {
            Child1 = existingChild1
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - Child1 should be the same instance, just with updated value
        Assert.Same(existingChild1, destination.Child1);
        Assert.Equal(10, destination.Child1.Value);
        // Child2 and Child3 should be created
        Assert.NotNull(destination.Child2);
        Assert.NotNull(destination.Child3);
    }
}

// Null handling tests
public class NullHandlingTests
{
    [Fact]
    public void Map_NestedSourceWithNullChild_SkipsCopyForNullSource()
    {
        // Arrange
        var source = new NullableNestedSource
        {
            Child = null,  // Child is null
            DirectValue = 100
        };
        var destination = new NullableNestedFlatDestination
        {
            ChildId = 999,       // Original values should be preserved
            ChildName = "Original",
            DirectValue = 0
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - DirectValue should be copied, but nested properties should be skipped
        Assert.Equal(100, destination.DirectValue);
        Assert.Equal(999, destination.ChildId);           // Should preserve original
        Assert.Equal("Original", destination.ChildName);  // Should preserve original
    }

    [Fact]
    public void Map_NestedSourceWithNonNullChild_CopiesNestedProperties()
    {
        // Arrange
        var source = new NullableNestedSource
        {
            Child = new NullableNestedSourceChild
            {
                Id = 42,
                Name = "Test"
            },
            DirectValue = 100
        };
        var destination = new NullableNestedFlatDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert - All properties should be copied
        Assert.Equal(100, destination.DirectValue);
        Assert.Equal(42, destination.ChildId);
        Assert.Equal("Test", destination.ChildName);
    }

    [Fact]
    public void Map_NullableProperties_CopiesNullValues()
    {
        // Arrange
        var source = new NullablePropertySource
        {
            NullableName = null,
            NullableInt = null,
            NonNullableName = "Test"
        };
        var destination = new NullablePropertyDestination
        {
            NullableName = "Original",
            NullableInt = 999
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - Nullable properties should be copied (including null)
        Assert.Null(destination.NullableName);
        Assert.Null(destination.NullableInt);
        Assert.Equal("Test", destination.NonNullableName);
    }

    [Fact]
    public void Map_NullableProperties_CopiesNonNullValues()
    {
        // Arrange
        var source = new NullablePropertySource
        {
            NullableName = "NewName",
            NullableInt = 42,
            NonNullableName = "Test"
        };
        var destination = new NullablePropertyDestination();

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("NewName", destination.NullableName);
        Assert.Equal(42, destination.NullableInt);
        Assert.Equal("Test", destination.NonNullableName);
    }

    [Fact]
    public void Map_NullableToNonNullable_WithNullSource_SetsDefault()
    {
        // Arrange
        var source = new NullableToNonNullableSource
        {
            Name = null
        };
        var destination = new NullableToNonNullableDestination
        {
            Name = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - Expected behavior: Set default! when source is null and target is non-nullable
        // For string, default! is null
        Assert.Null(destination.Name);
    }

    [Fact]
    public void Map_NullableToNonNullable_WithNonNullSource_CopiesValue()
    {
        // Arrange
        var source = new NullableToNonNullableSource
        {
            Name = "NewValue"
        };
        var destination = new NullableToNonNullableDestination
        {
            Name = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("NewValue", destination.Name);
    }

    [Fact]
    public void Map_NullableIntToString_WithNullSource_SetsDefault()
    {
        // Arrange
        var source = new NullableIntToStringSource
        {
            IntValue = null
        };
        var destination = new NullableIntToStringDestination
        {
            IntValue = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert - int? null -> string should be default! (null)
        Assert.Null(destination.IntValue);
    }

    [Fact]
    public void Map_NullableIntToString_WithNonNullSource_ConvertsValue()
    {
        // Arrange
        var source = new NullableIntToStringSource
        {
            IntValue = 42
        };
        var destination = new NullableIntToStringDestination
        {
            IntValue = "Original"
        };

        // Act
        TestMappers.Map(source, destination);

        // Assert
        Assert.Equal("42", destination.IntValue);
    }
}

#endregion
