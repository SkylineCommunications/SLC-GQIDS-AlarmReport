using Skyline.DataMiner.Analytics.GenericInterface;

[GQIMetaData(Name = "Alarm report > Time spans")]
public sealed class TimeSpans : IGQIDataSource
{
    public const string DAY = "DAY";
    public const string WEEK = "WEEK";
    public const string MONTH = "MONTH";

    public const string DAY_LABEL = "Last 24 hours";
    public const string WEEK_LABEL = "Last 7 days";
    public const string MONTH_LABEL = "Last 30 days";

    private readonly GQIColumn<string> _labelColumn;
    private readonly GQIColumn<string> _valueColumn;

    public TimeSpans()
    {
        _labelColumn = new GQIStringColumn("Label");
        _valueColumn = new GQIStringColumn("Value");
    }

    public GQIColumn[] GetColumns()
    {
        return new GQIColumn[]
        {
            _labelColumn,
            _valueColumn,
        };
    }

    public GQIPage GetNextPage(GetNextPageInputArgs args)
    {
        var rows = new[]
        {
            CreateRow(DAY_LABEL, DAY),
            CreateRow(WEEK_LABEL, WEEK),
            CreateRow(MONTH_LABEL, MONTH),
        };
        return new GQIPage(rows);
    }

    private GQIRow CreateRow(string label, string value)
    {
        var cells = new[]
        {
            new GQICell { Value = label },
            new GQICell { Value = value },
        };
        return new GQIRow(cells);
    }
}