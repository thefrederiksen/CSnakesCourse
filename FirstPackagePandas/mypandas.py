import pandas as pd
import numpy as np
from datetime import datetime, timedelta

def create_sales_data(n_records: int, random_seed=None):
    """
    Create sample sales and product data for pivot table practice.
    
    Parameters:
    n_records (int): Number of records to generate
    random_seed (int): Random seed for reproducibility (default: 42)
    
    Returns:
    pd.DataFrame: Sales data with columns for pivoting
    """
    # Set random seed for reproducibility
    if random_seed is None:
        random_seed = 42
    np.random.seed(random_seed)
    
    # Define categories
    products = ['Laptop', 'Desktop', 'Monitor', 'Keyboard', 'Mouse', 'Printer', 'Tablet', 'Phone']
    regions = ['North', 'South', 'East', 'West', 'Central']
    salespeople = ['Alice Johnson', 'Bob Smith', 'Carol Davis', 'David Wilson', 'Emma Brown', 'Frank Miller']
    quarters = ['Q1', 'Q2', 'Q3', 'Q4']
    
    # Generate random data
    data = {
        'Date': pd.date_range(start='2023-01-01', end='2023-12-31', periods=n_records),
        'Product': np.random.choice(products, n_records),
        'Region': np.random.choice(regions, n_records),
        'Salesperson': np.random.choice(salespeople, n_records),
        'Quarter': np.random.choice(quarters, n_records),
        'Units_Sold': np.random.randint(1, 50, n_records),
        'Unit_Price': np.random.uniform(50, 2000, n_records).round(2),
        'Discount': np.random.uniform(0, 0.3, n_records).round(3),
        'Customer_Satisfaction': np.random.uniform(3.0, 5.0, n_records).round(1)
    }
    
    # Create DataFrame
    df = pd.DataFrame(data)
    
    # Calculate additional columns
    df['Revenue'] = (df['Units_Sold'] * df['Unit_Price'] * (1 - df['Discount'])).round(2)
    df['Month'] = df['Date'].dt.month_name()
    df['Year'] = df['Date'].dt.year
    
    return df

def pivot_revenue_by_product_region(df):
    """
    Create a pivot table showing Revenue by Product and Region.
    
    Parameters:
    df (pd.DataFrame): Sales data to pivot
    
    Returns:
    pd.DataFrame: Pivot table with Products as rows, Regions as columns, Revenue as values
    """
    return df.pivot_table(values='Revenue', index='Product', columns='Region', 
                         aggfunc='sum', fill_value=0)

def create_pivot_table(df, values, index, columns, aggfunc=None, fill_value=None):
    """
    Create a pivot table with specified parameters.
    
    Parameters:
    df (pd.DataFrame): Source dataframe to pivot
    values (str or list): Column(s) to aggregate
    index (str or list): Column(s) to use as row index
    columns (str or list): Column(s) to use as column headers
    aggfunc (str or function): Aggregation function (default: 'sum')
    fill_value (scalar): Value to replace missing values (default: 0)
    
    Returns:
    pd.DataFrame: Pivot table with specified configuration
    """
    if aggfunc is None:
        aggfunc = 'sum'
    if fill_value is None:
        fill_value = 0
    return df.pivot_table(values=values, index=index, columns=columns, 
                         aggfunc=aggfunc, fill_value=fill_value)

# def main():
#     """
#     Main method to create sales data and generate revenue pivot table.
    
#     Returns:
#     tuple: (original_dataframe, pivot_table)
#     """
#     # Create the dataset
#     df = create_sales_data(n_records=200)
    
#     # Create the pivot table using the generic pivot method
#     pivot_table = create_pivot_table(df, values='Revenue', index='Product', 
#                                    columns='Region', aggfunc='sum', fill_value=0)
    
#     print("Revenue by Product and Region:")
#     print(pivot_table)
    
#     return df, pivot_table

# # Execute the main method when script is run
# if __name__ == "__main__":
#     sales_df, revenue_pivot = main()