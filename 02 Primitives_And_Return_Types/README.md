# CSnakes Course - Lab 02: Primitives and Return Types

## Overview

This lab demonstrates how CSnakes maps Python types to C# types. Understanding this mapping is essential for writing interoperable code.

## Official Documentation

**CSnakes Type System Reference:**
https://tonybaloney.github.io/CSnakes/v1/user-guide/type-system/

---

## Type Mapping Matrix

### Primitive Types

| Python Type | C# Type | Notes |
|-------------|---------|-------|
| `int` | `long` | Python int is unlimited precision, C# long is 64-bit |
| `float` | `double` | Python float is 64-bit, maps to C# double |
| `str` | `string` | Unicode strings map directly |
| `bool` | `bool` | Direct mapping |
| `bytes` | `byte[]` | Binary data |
| `None` (return) | `void` | Functions returning None become void methods |

### Collection Types

| Python Type | C# Type | Notes |
|-------------|---------|-------|
| `list[T]` | `IReadOnlyList<T>` | Immutable in C# |
| `dict[K, V]` | `IReadOnlyDictionary<K, V>` | Immutable in C# |
| `tuple[T1, T2, ...]` | `(T1, T2, ...)` | C# value tuples (up to 17 items) |
| `set[T]` | `IReadOnlySet<T>` | Immutable in C# |

### Optional and Union Types

| Python Type | C# Type | Notes |
|-------------|---------|-------|
| `Optional[T]` | `T?` | Nullable value/reference type |
| `T \| None` | `T?` | Same as Optional[T] |
| `Union[T1, T2]` | Overloads | CSnakes generates method overloads |

### Special Types

| Python Type | C# Type | Notes |
|-------------|---------|-------|
| `datetime` | `PyObject` | No direct mapping - use string conversion |
| `Generator[...]` | `IGeneratorIterator<...>` | For Python generators |
| `Coroutine[...]` | `Task<T>` | For async Python functions |
| Classes | `PyObject` | Custom classes return as PyObject |

---

## Examples in This Lab

### primitives.py Functions

| Function | Python Signature | C# Signature |
|----------|-----------------|--------------|
| `add_numbers` | `(a: int, b: float) -> float` | `double AddNumbers(long a, double b)` |
| `concatenate_strings` | `(s1: str, s2: str) -> str` | `string ConcatenateStrings(string s1, string s2)` |
| `is_positive` | `(number: float) -> bool` | `bool IsPositive(double number)` |
| `multiply_integers` | `(x: int, y: int) -> int` | `long MultiplyIntegers(long x, long y)` |
| `get_optional_message` | `(include: bool) -> Optional[str]` | `string? GetOptionalMessage(bool include)` |
| `process_nullable_input` | `(value: Optional[int]) -> str` | `string ProcessNullableInput(long? value)` |
| `reverse_bytes` | `(data: bytes) -> bytes` | `byte[] ReverseBytes(byte[] data)` |
| `write_to_file` | `(...) -> None` | `void WriteToFile(...)` |
| `get_current_time` | `() -> datetime` | `PyObject GetCurrentTime()` |

---

## Key Concepts

### 1. Python int maps to C# long (not int)

Python integers have unlimited precision. CSnakes maps them to `long` (64-bit) to handle larger values.

```python
def multiply_integers(x: int, y: int) -> int:
    return x * y
```

Generated C#:
```csharp
public long MultiplyIntegers(long x, long y)
```

### 2. Optional types become nullable

```python
def get_optional_message(include: bool) -> Optional[str]:
    return "Hello" if include else None
```

Generated C#:
```csharp
public string? GetOptionalMessage(bool include)
```

### 3. None return type becomes void

```python
def write_to_file(filename: str, text: str) -> None:
    pass
```

Generated C#:
```csharp
public void WriteToFile(string filename, string text)
```

### 4. datetime requires manual conversion

Python `datetime` maps to `PyObject` because there's no direct equivalent. Use string conversion:

```python
def get_current_time_as_text() -> str:
    return datetime.now().isoformat()
```

Or access PyObject attributes in C#:
```csharp
PyObject pyDt = pythonEnv.Primitives().GetCurrentTime();
string isoFormat = pyDt.GetAttr("isoformat").Call().As<string>();
DateTime dt = DateTime.Parse(isoFormat);
```

### 5. Default parameters are preserved

```python
def divide_with_default(dividend: int, divisor: int = 2) -> float:
    return dividend / divisor
```

Generated C#:
```csharp
public double DivideWithDefault(long dividend, long divisor = 2)
```

---

## Common Pitfalls

### 1. int vs long confusion

C# `int` is 32-bit, but CSnakes uses `long` (64-bit) for Python `int`.

```csharp
// This works
long result = pythonEnv.Primitives().MultiplyIntegers(7, 8);

// This requires cast
int smallResult = (int)pythonEnv.Primitives().MultiplyIntegers(7, 8);
```

### 2. Nullable value types

When Python returns `Optional[int]`, C# gets `long?` (nullable long):

```csharp
long? value = pythonEnv.Primitives().ProcessSomething();
if (value.HasValue)
{
    Console.WriteLine($"Value: {value.Value}");
}
```

### 3. Collections are read-only

Python `list` becomes `IReadOnlyList<T>` - you cannot modify it in C#.

```csharp
IReadOnlyList<long> numbers = pythonEnv.GetNumbers();
// numbers.Add(5); // ERROR - not allowed
```

---

## Running This Lab

1. Open `CSnakes Course.sln` in Visual Studio
2. Set `02. Primitives_And_Return_Types` as startup project
3. Press F5 to run
4. Observe type conversions in action

---

## References

- [CSnakes Type System](https://tonybaloney.github.io/CSnakes/v1/user-guide/type-system/)
- [CSnakes Documentation](https://tonybaloney.github.io/CSnakes/)
- [CSnakes GitHub](https://github.com/tonybaloney/CSnakes)
