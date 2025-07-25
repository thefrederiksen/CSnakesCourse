def create_sample_dataframe() -> str:
    """
    Creates a sample pandas DataFrame with employee data and demonstrates basic pandas/numpy integration.
   
    """
    try:
        import pandas as pd
        import numpy as np
        
        # Create a simple DataFrame with sample employee data
        # Using realistic but simple data for demonstration purposes
        data = {
            'Name': ['Alice', 'Bob', 'Charlie', 'Diana', 'Eve'],
            'Age': [25, 30, 35, 28, 32],
            'Salary': [50000, 60000, 70000, 55000, 65000],
            'Department': ['IT', 'HR', 'IT', 'Finance', 'IT']
        }
        
        df = pd.DataFrame(data)
        
        # Demonstrate numpy integration with pandas
        # Add normalized salary column using z-score normalization
        df['Salary_Normalized'] = (df['Salary'] - np.mean(df['Salary'])) / np.std(df['Salary'])
        
        # Add categorical age group using numpy conditional logic
        df['Age_Group'] = np.where(df['Age'] < 30, 'Young', 'Senior')
        
        # Return formatted string representation for display
        return df.to_string()
        
    except ImportError as e:
        return (f"Import Error: {str(e)}\n\n"
                f"Required libraries not found. Please ensure pandas and numpy are installed:\n"
                f"   pip install pandas numpy\n\n"
                f"In CSnakes projects, add these to requirements.txt for automatic installation.")
    except Exception as e:
        return (f"Runtime Error: {str(e)}\n\n"
                f"An unexpected error occurred while creating the sample DataFrame.\n"
                f"This might indicate a version compatibility issue or system configuration problem.")