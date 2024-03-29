using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Messages;
using System.Collections.Generic;
using System.Linq;

[GQIMetaData(Name = "Alarm report > States")]
public sealed class States : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
    private readonly GQIColumn<string> _nameColumn;
    private readonly GQIColumn<double> _timeoutColumn;
    private readonly GQIColumn<double> _warningColumn;
    private readonly GQIColumn<double> _minorColumn;
    private readonly GQIColumn<double> _majorColumn;
    private readonly GQIColumn<double> _criticalColumn;

    private GQIDMS _dms;
    private int _viewFilter;
    private string _timeSpan;

    public States()
    {
        _nameColumn = new GQIStringColumn("Name");
        _timeoutColumn = new GQIDoubleColumn("Timeout");
        _warningColumn = new GQIDoubleColumn("Warning");
        _minorColumn = new GQIDoubleColumn("Minor");
        _majorColumn = new GQIDoubleColumn("Major");
        _criticalColumn = new GQIDoubleColumn("Critical");
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
            _nameColumn,
            _timeoutColumn,
            _warningColumn,
            _minorColumn,
            _majorColumn,
            _criticalColumn,
        };
    }

    public GQIPage GetNextPage(GetNextPageInputArgs args)
    {
        var rows = GetRows(_timeSpan);
        return new GQIPage(rows);
    }

    private GQIRow[] GetRows(string timeSpan)
    {
        var data = GetAlarmCounts(timeSpan);

        var rows = new List<GQIRow>();
        foreach (var response in data)
        {
            var row = CreateRow(response);
            rows.Add(row);
        }
        return rows.ToArray();
    }

    private GQIRow CreateRow(ReportStateDataResponseMessage response)
    {
        var name = GetName(response);
        var cells = new[]
        {
            new GQICell { Value = name },
            new GQICell { Value = response.PercentageTimeout },
            new GQICell { Value = response.PercentageWarning },
            new GQICell { Value = response.PercentageMinor },
            new GQICell { Value = response.PercentageMajor },
            new GQICell { Value = response.PercentageCritical },
        };
        return new GQIRow(cells);
    }

    private IEnumerable<ReportStateDataResponseMessage> GetAlarmCounts(string timeSpan)
    {
        var viewFilter = new ReportFilterInfo(ReportFilterType.View)
        {
            ViewID = _viewFilter,
        };
        var request = new GetReportStateDataMessage
        {
            Timespan = timeSpan,
            SortMethod = ReportTopSortType.Total,
            MaxAmount = 5,
            Options = ReportOptionFlags.IncludeDerivedElements | ReportOptionFlags.IncludeServices,
            Filter = viewFilter,
        };
        var responses = _dms.SendMessages(request);
        return responses.OfType<ReportStateDataResponseMessage>();
    }

    private string GetName(ReportStateDataResponseMessage response)
    {
        if (response.IsService)
            return GetServiceName(response.DataMinerID, response.ServiceID);
        else
            return GetElementName(response.DataMinerID, response.ElementID);
    }

    private string GetElementName(int dmaID, int elementID)
    {
        var request = GetLiteElementInfo.ByID(dmaID, elementID);
        var element = _dms.SendMessage(request) as LiteElementInfoEvent;
        return element.Name;
    }

    private string GetServiceName(int dmaID, int serviceID)
    {
        var request = GetLiteServiceInfo.ByID(dmaID, serviceID);
        var service = _dms.SendMessage(request) as LiteServiceInfoEvent;
        return service.Name;
    }
}