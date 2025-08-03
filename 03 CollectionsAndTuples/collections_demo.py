from typing import List, Tuple, Dict, Optional

def process_employees(employees: List[Tuple[str, int]]) -> Dict[str, str]:
    """
    Takes a list of (name, age) tuples and returns employee categories
    Demonstrates: List[Tuple] -> Dict marshaling
    """
    categories = {}
    for name, age in employees:
        if age < 25:
            categories[name] = "junior"
        elif age < 40:
            categories[name] = "mid-level"
        else:
            categories[name] = "senior"
    return categories

def calculate_team_stats(team_scores: Dict[str, List[float]]) -> Dict[str, Tuple[float, int]]:
    """
    Takes team names mapped to score lists, returns (average, count) for each team
    Demonstrates: Nested collections and tuple returns
    """
    stats = {}
    for team_name, scores in team_scores.items():
        if scores:  # Handle empty lists
            average = sum(scores) / len(scores)
            stats[team_name] = (average, len(scores))
        else:
            stats[team_name] = (0.0, 0)
    return stats

def merge_department_data(
    employees: List[Tuple[str, int]], 
    salaries: Dict[str, float]
) -> List[Tuple[str, int, float]]:
    """
    Combines employee list with salary dict, returns enriched data
    Demonstrates: Multiple input types, complex output type
    """
    result = []
    for name, age in employees:
        salary = salaries.get(name, 0.0)  # Default to 0 if not found
        result.append((name, age, salary))
    return result

def handle_optional_data(data: List[Tuple[str, Optional[int]]]) -> Dict[str, str]:
    """
    Demonstrates handling None values in collections
    Shows how Python Optional[T] maps to C# nullable types
    """
    result = {}
    for name, age in data:
        if age is None:
            result[name] = "age_unknown"
        else:
            result[name] = f"age_{age}"
    return result

def process_nested_structures(
    departments: Dict[str, List[Tuple[str, int, float]]]
) -> Dict[str, Dict[str, float]]:
    """
    Complex nested structure processing
    Demonstrates: Deep nesting and multiple collection types
    """
    summary = {}
    for dept_name, employees in departments.items():
        dept_stats = {
            "total_employees": float(len(employees)),
            "avg_age": sum(emp[1] for emp in employees) / len(employees) if employees else 0.0,
            "total_salary": sum(emp[2] for emp in employees)
        }
        summary[dept_name] = dept_stats
    return summary