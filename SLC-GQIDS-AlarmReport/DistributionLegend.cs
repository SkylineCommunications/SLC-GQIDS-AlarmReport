using Skyline.DataMiner.Analytics.GenericInterface;
using System;

[GQIMetaData(Name = "Alarm report > Distribution legend")]
public sealed class DistributionLegend : IGQIDataSource, IGQIInputArguments
{
    public const string WEEKLY_AVG_LABEL = "7 day average";
    public const string MONTHLY_AVG_LABEL = "30 day average";

    public const string VALUE_TYPE = "VALUE";
    public const string AVERAGE_TYPE = "AVERAGE";

    private readonly GQIColumn<string> _labelColumn;
    private readonly GQIColumn<bool> _isAverageColumn;

    private string _timeSpan;

    public DistributionLegend()
    {
        _labelColumn = new GQIStringColumn("Label");
        _isAverageColumn = new GQIBooleanColumn("Is average");
    }

    public GQIArgument[] GetInputArguments()
    {
        return new[] { Report.Instance.TimeSpanArgument };
    }

    public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
    {
        _timeSpan = Report.Instance.GetTimeSpan(args);
        return default;
    }

    public GQIColumn[] GetColumns()
    {
        return new GQIColumn[]
        {
            _labelColumn,
            _isAverageColumn,
        };
    }

    public GQIPage GetNextPage(GetNextPageInputArgs args)
    {
        var rows = CreateRows(_timeSpan);
        return new GQIPage(rows);
    }

    private static GQIRow[] CreateRows(string timeSpan)
    {
        switch (timeSpan)
        {
            case TimeSpans.DAY:
                return new[]
                {
                    CreateRow(TimeSpans.DAY_LABEL, false),
                    CreateRow(WEEKLY_AVG_LABEL, true),
                };
            case TimeSpans.WEEK:
                return new[]
                {
                    CreateRow(TimeSpans.WEEK_LABEL, false),
                    CreateRow(MONTHLY_AVG_LABEL, true),
                };
            case TimeSpans.MONTH:
                return new[]
                {
                    CreateRow(TimeSpans.MONTH_LABEL, false),
                };
            default:
                return Array.Empty<GQIRow>();
        }
    }

    private static GQIRow CreateRow(string label, bool isAverage)
    {
        var cells = new[]
        {
            new GQICell { Value = label },
            new GQICell { Value = isAverage },
        };
        return new GQIRow(cells);
    }
}
