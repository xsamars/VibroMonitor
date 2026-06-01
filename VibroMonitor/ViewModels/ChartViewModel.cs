using VibroMonitor.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using VibroMonitor.Data;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Defaults;
using System.Linq;
using System;

namespace VibroMonitor.ViewModels;

public partial class PointChartViewModel : ObservableObject
{
    public EquipmentPoint Point { get; }

    public IEnumerable<ISeries> Series { get; private set; } = Array.Empty<ISeries>();

    // Labeler for X axis - converts OADate back to localized DateTime string
    public Func<double, string> XLabeler { get; private set; } = val => DateTime.FromOADate(val).ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");

    public enum TimeRange { Hour, Day, Week, Month, All }

    private readonly AppDbContext _db;

    private TimeRange _selectedRange = TimeRange.Day;
    public TimeRange SelectedRange
    {
        get => _selectedRange;
        set
        {
            if (_selectedRange == value) return;
            _selectedRange = value;
            OnPropertyChanged(nameof(SelectedRange));
            LoadRange();
        }
    }

    public PointChartViewModel(EquipmentPoint point, AppDbContext db)
    {
        Point = point;
        _db = db;

        // default selection
        SelectedRange = TimeRange.Day;
        // initial load
        LoadRange();
    }

    private void LoadRange()
    {
        try
        {
            DateTime? from = null;
            var now = DateTime.UtcNow;
            switch (SelectedRange)
            {
                case TimeRange.Hour: from = now.AddHours(-1); break;
                case TimeRange.Day: from = now.AddDays(-1); break;
                case TimeRange.Week: from = now.AddDays(-7); break;
                case TimeRange.Month: from = now.AddMonths(-1); break;
                case TimeRange.All: from = null; break;
            }

            var query = _db.PointHistory.Where(h => h.EquipmentPointId == Point.Id);
            if (from.HasValue)
                query = query.Where(h => h.Time >= from.Value);

            var history = query.OrderBy(h => h.Time).ToList();

            var values = history.Select(h => new ObservablePoint(h.Time.ToOADate(), h.Value)).ToArray();

            Series = new ISeries[] { new LineSeries<ObservablePoint> { Values = values, Name = Point.Name } };
            OnPropertyChanged(nameof(Series));
            // also update labeler to show only date for wide ranges
            XLabeler = val => DateTime.FromOADate(val).ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");
            OnPropertyChanged(nameof(XLabeler));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load point history for point {Point.Id}: {ex.Message}");
            Series = Array.Empty<ISeries>();
            OnPropertyChanged(nameof(Series));
        }
    }
}
