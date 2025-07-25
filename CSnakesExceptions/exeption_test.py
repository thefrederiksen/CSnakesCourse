def test_divide_by_zero():
    try:
        result = 10 / 0
    except ZeroDivisionError as e:
        print(f"Caught exception in Python: {e}")
        raise  # Re-raise the same exception

def umtimate_question() -> str:
    return 42  # This returns an int, despite the type hint of str