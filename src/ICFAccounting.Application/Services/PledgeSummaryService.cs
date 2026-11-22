using System.Globalization;
using System.Net;
using System.Text;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Builds HTML pledge summaries from accounting repositories.
/// </summary>
public sealed class PledgeSummaryService(
    IPledgerRepository pledgerRepository,
    IPledgeRepository pledgeRepository,
    IPledgePaymentRepository pledgePaymentRepository,
    IPledgeCategoryRepository pledgeCategoryRepository,
    TimeProvider timeProvider
) : IPledgeSummaryService
{
    private const int PAGE_SIZE = 100;
    private readonly IPledgerRepository _pledgerRepository =
        pledgerRepository ?? throw new ArgumentNullException(nameof(pledgerRepository));
    private readonly IPledgeRepository _pledgeRepository =
        pledgeRepository ?? throw new ArgumentNullException(nameof(pledgeRepository));
    private readonly IPledgePaymentRepository _pledgePaymentRepository =
        pledgePaymentRepository ?? throw new ArgumentNullException(nameof(pledgePaymentRepository));
    private readonly IPledgeCategoryRepository _pledgeCategoryRepository =
        pledgeCategoryRepository ?? throw new ArgumentNullException(nameof(pledgeCategoryRepository));
    private readonly TimeProvider _timeProvider =
        timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <inheritdoc />
    public async Task<IReadOnlyList<PledgerPledgeSummary>> CreateAsync(
        CancellationToken cancellationToken = default
    )
    {
        var now = _timeProvider.GetUtcNow();
        var currentYear = now.Year;
        var includePriorYear = now.Month == 1;
        var pledgers = await _pledgerRepository.GetAllAsync(cancellationToken: cancellationToken);
        var categories = await _pledgeCategoryRepository.GetAllAsync(cancellationToken);
        var categoriesById = categories.ToDictionary(
            category => category.Id,
            category => category.CategoryName
        );
        var summaries = new List<PledgerPledgeSummary>();

        foreach (var pledger in pledgers.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!pledger.Active || !pledger.EnableNotification || string.IsNullOrWhiteSpace(pledger.Email))
            {
                continue;
            }

            var html = await CreateHtmlAsync(
                pledger.PledgerName,
                pledger.Id,
                currentYear,
                includePriorYear ? currentYear - 1 : null,
                now.UtcDateTime,
                categoriesById,
                cancellationToken
            );
            summaries.Add(new PledgerPledgeSummary(pledger.PledgerName, pledger.Email, html));
        }

        return summaries;
    }

    private async Task<string> CreateHtmlAsync(
        string pledgerName,
        Domain.Pledgers.PledgerId pledgerId,
        int currentYear,
        int? priorYear,
        DateTime currentDate,
        IReadOnlyDictionary<Domain.PledgeCategories.PledgeCategoryId, string> categoriesById,
        CancellationToken cancellationToken
    )
    {
        var html = new StringBuilder();
        html.Append("<html><body>");
        html.Append("<h1>Pledge Summary</h1>");
        html.Append($"<p><strong>{WebUtility.HtmlEncode(pledgerName)}</strong></p>");
        await AppendYearAsync(
            html,
            "Current Year",
            currentYear,
            pledgerId,
            categoriesById,
            cancellationToken,
            currentDate
        );

        if (priorYear.HasValue)
        {
            await AppendYearAsync(
                html,
                "Prior Year",
                priorYear.Value,
                pledgerId,
                categoriesById,
                cancellationToken
            );
        }

        html.Append("</body></html>");
        return html.ToString();
    }

    private async Task AppendYearAsync(
        StringBuilder html,
        string heading,
        int year,
        Domain.Pledgers.PledgerId pledgerId,
        IReadOnlyDictionary<Domain.PledgeCategories.PledgeCategoryId, string> categoriesById,
        CancellationToken cancellationToken,
        DateTime? throughDate = null
    )
    {
        html.Append($"<h2>{WebUtility.HtmlEncode(heading)} - {year}</h2>");
        var pledges = await GetPledgesAsync(pledgerId, year, cancellationToken);
        if (pledges.Count == 0)
        {
            html.Append("<p>No pledges found.</p>");
            return;
        }

        foreach (var pledge in pledges)
        {
            cancellationToken.ThrowIfCancellationRequested();
            categoriesById.TryGetValue(pledge.PledgeCategoryId, out var categoryName);
            categoryName = string.IsNullOrWhiteSpace(categoryName) ? "Uncategorized" : categoryName;

            html.Append("<section style=\"margin-bottom: 1.5em\">");
            html.Append($"<h3>{WebUtility.HtmlEncode(categoryName)}</h3>");
            html.Append(
                $"<p>Pledge amount: <strong>{pledge.Amount.ToString("C", CultureInfo.CurrentCulture)}</strong></p>"
            );

            var payments = await GetPaymentsAsync(pledge.Id, year, cancellationToken, throughDate);
            html.Append("<table border=\"1\" cellpadding=\"4\" cellspacing=\"0\">");
            html.Append("<thead><tr><th>Date</th><th>Amount</th></tr></thead><tbody>");
            decimal total = 0m;
            foreach (var payment in payments)
            {
                total += payment.Amount;
                html.Append(
                    $"<tr><td>{payment.PaymentDate.ToString("d", CultureInfo.CurrentCulture)}</td>"
                    + $"<td>{payment.Amount.ToString("C", CultureInfo.CurrentCulture)}</td></tr>"
                );
            }
            html.Append("</tbody></table>");
            html.Append(
                $"<p>YTD total: <strong>{total.ToString("C", CultureInfo.CurrentCulture)}</strong></p>"
            );
            html.Append("</section>");
        }
    }

    private async Task<IReadOnlyList<Pledge>> GetPledgesAsync(
        Domain.Pledgers.PledgerId pledgerId,
        int year,
        CancellationToken cancellationToken
    )
    {
        var pledges = new List<Pledge>();
        var page = 1;
        while (true)
        {
            var result = await _pledgeRepository.GetByPledgerAsync(
                pledgerId,
                new PledgeQuery(page, PAGE_SIZE, PledgeSortBy.Year),
                cancellationToken
            );
            pledges.AddRange(result.Items.Where(pledge => pledge.Year == year));
            if (page >= result.TotalPages)
            {
                return pledges;
            }
            page++;
        }
    }

    private async Task<IReadOnlyList<PledgePayment>> GetPaymentsAsync(
        PledgeId pledgeId,
        int year,
        CancellationToken cancellationToken,
        DateTime? throughDate = null
    )
    {
        var payments = new List<PledgePayment>();
        var page = 1;
        while (true)
        {
            var result = await _pledgePaymentRepository.GetByPledgeAsync(
                pledgeId,
                new PledgePaymentQuery(page, PAGE_SIZE),
                cancellationToken
            );
            payments.AddRange(
                result.Items.Where(payment =>
                    payment.Active
                    && payment.PaymentDate.Year == year
                    && (!throughDate.HasValue || payment.PaymentDate <= throughDate.Value)
                )
            );
            if (page >= result.TotalPages)
            {
                return payments;
            }
            page++;
        }
    }
}
