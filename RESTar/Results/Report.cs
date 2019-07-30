using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful REPORT requests
    /// </summary>
    public class Report : Content
    {
        /// <summary>
        /// The body of the report
        /// </summary>
        public ReportBody ReportBody { get; }

        internal Report(IRequest request, long count) : base(request) => ReportBody = new ReportBody(count);

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Report)};{Request.Resource};{ReportBody.Count}";
    }

    /// <summary>
    /// Describes the body of a report
    /// </summary>
    public class ReportBody
    {
        /// <summary>
        /// The number of entities counted
        /// </summary>
        public long Count { get; }

        internal ReportBody(long count) => Count = count;
    }
}