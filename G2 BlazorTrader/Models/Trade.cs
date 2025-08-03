using System;

namespace BlazorTrader.Models;

/// <summary>
/// Represents a stock trade
/// </summary>
public class Trade
{
    /// <summary>
    /// Unique identifier for the trade
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Stock symbol (e.g., AAPL, MSFT)
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of shares traded
    /// </summary>
    public int Quantity { get; set; }
    
    /// <summary>
    /// Price per share
    /// </summary>
    public double Price { get; set; }
    
    /// <summary>
    /// Total value of the trade (Quantity * Price)
    /// </summary>
    public double TotalValue => Quantity * Price;
    
    /// <summary>
    /// Date and time when the trade was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Whether this is a buy (true) or sell (false) trade
    /// </summary>
    public bool IsBuy { get; set; } = true;
    
    /// <summary>
    /// Whether this is a Sunday trade
    /// </summary>
    public bool IsSundayTrade { get; set; }
    
    /// <summary>
    /// Optional notes about the trade
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Latest close price for the stock symbol
    /// </summary>
    public double? LatestPrice { get; set; }
}