using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Net.Messages;
using System.Collections.Generic;
using System.Linq;

[GQIMetaData(Name = "Alarm report > Events")]
public sealed class Events : IGQIDataSource, IGQIOnInit, IGQIInputArguments
{
    private readonly GQIColumn<string> _nameColumn;
    private readonly GQIColumn<int> _timeoutColumn;
    private readonly GQIColumn<int> _warningColumn;
    private readonly GQIColumn<int> _minorColumn;
    private readonly GQIColumn<int> _majorColumn;
    private readonly GQIColumn<int> _criticalColumn;

    private GQIDMS _dms;

    private int _viewFilter;
    private string _timeSpan;

    public Events()
    {
        _nameColumn = new GQIStringColumn("Name");
        _timeoutColumn = new GQIIntColumn("Timeout");
        _warningColumn = new GQIIntColumn("Warning");
        _minorColumn = new GQIIntColumn("Minor");
        _majorColumn = new GQIIntColumn("Major");
        _criticalColumn = new GQIIntColumn("Critical");
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

    private GQIRow CreateRow(ReportAlarmCountDataResponseMessage response)
    {
        var name = GetName(response);
        var cells = new[]
        {
            new GQICell { Value = name },
            new GQICell { Value = response.AmountTimeout },
            new GQICell { Value = response.AmountWarning },
            new GQICell { Value = response.AmountMinor },
            new GQICell { Value = response.AmountMajor },
            new GQICell { Value = response.AmountCritical },
        };
        return new GQIRow(cells);
    }

    private IEnumerable<ReportAlarmCountDataResponseMessage> GetAlarmCounts(string timeSpan)
    {
        var viewFilter = new ReportFilterInfo(ReportFilterType.View)
        {
            ViewID = _viewFilter,
        };
        var request = new GetReportAlarmCountDataMessage
        {
            Timespan = timeSpan,
            SortMethod = ReportTopSortType.Total,
            MaxAmount = 5,
            Options = ReportOptionFlags.IncludeDerivedElements | ReportOptionFlags.IncludeServices,
            Filter = viewFilter,
        };
        var responses = _dms.SendMessages(request);
        return responses.OfType<ReportAlarmCountDataResponseMessage>();
    }

    private string GetName(ReportAlarmCountDataResponseMessage response)
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