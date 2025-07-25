from typing import List, Dict, Any      
import pandas as pd

def revenue_by_region_category(
    rows: List[Dict[str, object]]
) -> List[Dict[str, object]]:
    """
    Build a Region × Category pivot table of total Revenue.

    Returns a list[dict] so C# can drop it straight back into a DataTable.
    """
    df = pd.DataFrame(rows)

    pivot = (
        df.pivot_table(
            values="Revenue",
            index="Region",
            columns="Category",
            aggfunc="sum",
            fill_value=0,      # missing combos become 0 instead of NaN
        )
        .reset_index()         # turn the index back into a column
    )

    return pivot.to_dict(orient="records")
