using ICFAccounting.Api.Models;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.Api.Extensions;

public static class EndpointExtensions
{
    public static void MapEndpoints(this WebApplication app)
    {
        app.MapPledgerEndpoints();
        app.MapPledgeCategoryEndpoints();
        app.MapPledgeEndpoints();
        app.MapPledgePaymentEndpoints();
    }
}