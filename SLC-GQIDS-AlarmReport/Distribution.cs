using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Messages;
using System;

[GQIMetaData(Name = "Alarm report > Distribution")]
public sealed class Distribution : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
    private readonly GQIColumn<string> _labelColumn;
    private readonly GQIColumn<double> _valueColumn;
    private readonly GQIColumn<double> _averageColumn;

    private GQIDMS _dms;
    private int _viewFilter;
    private string _timeSpan;

    public Distribution()
    {
        _labelColumn = new GQIStringColumn("Label");
        _valueColumn = new GQIDoubleColumn("Value");
        _averageColumn = new GQIDoubleColumn("Average");
    }

    public OnInitOutputArgs OnInit(OnInitInputArgs args)
    {
        _dms = args.DMS;
        return default;
    }

    public GQIArgument[] GetInputArguments()
    {
        return new GQIArgument[]
        {
            Report.Instance.ViewFilterArgument,
            Report.Instance.TimeSpanArgument,
        };
    }

    public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
    {
        _viewFilter = Report.Instance.GetViewFilter(args);
        _timeSpan = Report.Instance.GetTimeSpan(args);
        return default;
    }

    public GQIColumn[] GetColumns()
    {
        return new GQIColumn[]
        {
            _labelColumn,
            _valueColumn,
            _averageColumn,
        };
    }

    public GQIPage GetNextPage(GetNextPageInputArgs args)
    {
        var rows = GetRows(_timeSpan);
        return new GQIPage(rows);
    }

    private GQIRow CreateRow(string label, double value, double? average = null)
    {
        var cells = new[]
        {
            new GQICell { Value = label },
            new GQICell { Value = value },
            new GQICell { Value = average },
        };
        return new GQIRow(cells);
    }

    private GQIRow[] GetRows(string timeSpan)
    {
        switch (timeSpan)
        {
            case TimeSpans.DAY:
                return GetLast24Hours();
            case TimeSpans.WEEK:
                return GetLast7Days();
            case TimeSpans.MONTH:
                return GetLast30Days();
            default:
                return Array.Empty<GQIRow>();
        }
    }

    private GQIRow[] GetLast24Hours()
    {
        var valueRequest = GetData(ReportHistoryType.Last24Hours, ReportTimeslotType.Hour, ReportAverageType.NoAverage);
        var averageRequest = GetData(ReportHistoryType.LastWeek, ReportTimeslotType.Hour, ReportAverageType.Day);

        var labels = valueRequest.Labels;
        var values = valueRequest.DoubleValues;
        var averages = averageRequest.DoubleValues;

        var rows = new GQIRow[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            rows[i] = CreateRow(labels[i], values[i], averages[i]);
        }
        return rows;
    }

    private GQIRow[] GetLast7Days()
    {
        var valueRequest = GetData(ReportHistoryType.LastWeek, ReportTimeslotType.DayOfWeek, ReportAverageType.NoAverage);
        var averageRequest = GetData(ReportHistoryType.LastMonth, ReportTimeslotType.DayOfWeek, ReportAverageType.Week);

        var labels = valueRequest.Labels;
        var values = valueRequest.DoubleValues;
        var averages = averageRequest.DoubleValues;

        var rows = new GQIRow[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            var weekDay = LabelToWeekDay(labels[i]);
            rows[i] = CreateRow(weekDay, values[i], averages[i]);
        }
        return rows;
    }

    private string LabelToWeekDay(string label)
    {
        switch (label)
        {
            case "1": return "Monday";
            case "2": return "Tuesday";
            case "3": return "Wednesday";
            case "4": return "Thursday";
            case "5": return "Friday";
            case "6": return "Saturday";
            case "7": return "Sunday";
            default: return label;
        }
    }

    private GQIRow[] GetLast30Days()
    {
        var valueRequest = GetData(ReportHistoryType.LastMonth, ReportTimeslotType.Day, ReportAverageType.NoAverage);

        var labels = valueRequest.Labels;
        var values = valueRequest.DoubleValues;

        var rows = new GQIRow[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            rows[i] = CreateRow(labels[i], values[i]);
        }
        return rows;
    }

    private ReportAlarmDistributionDataResponseMessage GetData(ReportHistoryType timeSpan, ReportTimeslotType timeSlot, ReportAverageType average)
    {
        var viewFilter = new ReportFilterInfo(ReportFilterType.View)
        {
            ViewID = _viewFilter,
        };
        var request = new GetReportAlarmDistributionDataMessage
        {
            Span = timeSpan,
            TimeslotSize = timeSlot,
            Average = average,
            IncludedSeverities = ReportIncludedSeverities.All,
            Options = ReportOptionFlags.IncludeDerivedElements | ReportOptionFlags.IncludeServices,
            Filter = viewFilter,
        };
        return _dms.SendMessage(request) as ReportAlarmDistributionDataResponseMessage;
    }
}