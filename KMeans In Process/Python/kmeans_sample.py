# Python/kmeans_sample.py
from typing import Sequence
from sklearn.cluster import k_means

def get_version() -> str:
    """
    Return the fixed version string for this code
    """
    return "2.1.0"

def fit_kmeans(
        points: Sequence[tuple[float, float]],   # ← list/tuple-of-tuples → C# arrays / lists / spans
        n_clusters: int
    ) -> tuple[list[list[float]], float]:        # ← explicit return type
    """
    Fits K-Means on 2-D points and returns (centroids, inertia).
    """
    centroids, _, inertia = k_means(
        points,
        n_clusters=n_clusters,
        n_init="auto",
        random_state=0
    )
    return centroids.tolist(), inertia
