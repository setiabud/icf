using System.Data.SqlTypes;
using ICFAccounting.Data;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using ICFAccounting.IntegrationTests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SqlException = Microsoft.Data.SqlClient.SqlException;

namespace ICFAccounting.IntegrationTests.Repositories;

[Collection(nameof(SqlServerTestCollection))]
public sealed class SqlServerRepositoryTests(SqlServerModelFixture fixture) : IAsyncLifetime
{
    private readonly string _databaseName = $"ICFAccountingRepositoryTests_{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pledger_mutations_persist_across_contexts(bool insertWithUpdate)
    {
        var pledger = Pledger.Create("Before");
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgerRepository(context);
            if (insertWithUpdate)
            {
                await repository.UpdateAsync(pledger);
            }
            else
            {
                await repository.AddAsync(pledger);
            }
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgerRepository(context);
            var stored = await repository.GetByIdAsync(pledger.Id);
            Assert.NotNull(stored);
            Assert.Equal(pledger.CreatedAt, stored.CreatedAt);
            stored.Update("After", false);
            await repository.AddAsync(stored);
            stored.Update("Final", false);
            await repository.UpdateAsync(stored);
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgerRepository(context);
            var stored = await repository.GetByIdAsync(pledger.Id);
            Assert.NotNull(stored);
            Assert.Equal("Final", stored.PledgerName);
            Assert.False(stored.Active);
            Assert.Equal(pledger.CreatedAt, stored.CreatedAt);
            Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
            await repository.RemoveAsync(pledger.Id);
        }

        await using var readContext = CreateContext();
        var readRepository = new SqlServerPledgerRepository(readContext);
        Assert.Null(await readRepository.GetByIdAsync(pledger.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => readRepository.RemoveAsync(pledger.Id));
    }

    [Fact]
    public async Task Pledger_queries_preserve_null_query_and_paging_contracts()
    {
        var pledgers = Enumerable.Range(1, 21).Select(index => Pledger.Create($"Pledger {index:D2}")).ToList();
        pledgers[0].Update(pledgers[0].PledgerName, false);
        await using (var context = CreateContext())
        {
            context.Pledgers.AddRange(pledgers);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var repository = new SqlServerPledgerRepository(readContext);
        var unpaged = await repository.GetAllAsync();
        Assert.Equal(21, unpaged.Items.Count);
        Assert.Equal(21, unpaged.TotalCount);
        Assert.Equal(21, unpaged.PageSize);
        Assert.Equal(1, unpaged.Page);
        Assert.Contains(unpaged.Items, pledger => !pledger.Active);

        var firstPage = await repository.GetAllAsync(new PledgerQuery());
        Assert.Equal(20, firstPage.Items.Count);
        Assert.Equal(21, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        var descending = await repository.GetAllAsync(new PledgerQuery(2, 2, sortOrder: SortOrder.Descending));
        Assert.Equal(["Pledger 19", "Pledger 18"], descending.Items.Select(pledger => pledger.PledgerName));
        var beyondEnd = await repository.GetAllAsync(new PledgerQuery(int.MaxValue, 100));
        Assert.Empty(beyondEnd.Items);
        Assert.Equal(21, beyondEnd.TotalCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Category_mutations_persist_across_contexts(bool insertWithUpdate)
    {
        var category = PledgeCategory.Create("Before");
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeCategoryRepository(context);
            if (insertWithUpdate)
            {
                await repository.UpdateAsync(category);
            }
            else
            {
                await repository.AddAsync(category);
            }
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeCategoryRepository(context);
            var stored = await repository.GetByIdAsync(category.Id);
            Assert.NotNull(stored);
            stored.Update("After", false);
            await repository.AddAsync(stored);
            stored.Update("Final", false);
            await repository.UpdateAsync(stored);
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeCategoryRepository(context);
            var stored = Assert.Single(await repository.GetAllAsync());
            Assert.Equal(category.Id, stored.Id);
            Assert.Equal("Final", stored.CategoryName);
            Assert.False(stored.Active);
            Assert.Equal(category.CreatedAt, stored.CreatedAt);
            await repository.RemoveAsync(category.Id);
        }

        await using var readContext = CreateContext();
        var readRepository = new SqlServerPledgeCategoryRepository(readContext);
        Assert.Null(await readRepository.GetByIdAsync(category.Id));
        Assert.Empty(await readRepository.GetAllAsync());
        await Assert.ThrowsAsync<KeyNotFoundException>(() => readRepository.RemoveAsync(category.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Pledge_mutations_persist_across_contexts(bool insertWithUpdate)
    {
        var (pledger, category) = await SeedParentsAsync();
        var pledge = Pledge.Create(2025, 50m, pledger.Id, category.Id);
        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeRepository(context);
            if (insertWithUpdate)
            {
                await repository.UpdateAsync(pledge);
            }
            else
            {
                await repository.AddAsync(pledge);
            }
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeRepository(context);
            var stored = await repository.GetByIdAsync(pledge.Id);
            Assert.NotNull(stored);
            stored.Update(2026, 100m, pledger.Id, category.Id, "After");
            await repository.AddAsync(stored);
            stored.Update(2027, decimal.MaxValue, pledger.Id, category.Id, "Final");
            await repository.UpdateAsync(stored);
        }

        await using (var context = CreateContext())
        {
            var repository = new SqlServerPledgeRepository(context);
            var stored = await repository.GetByIdAsync(pledge.Id);
            Assert.NotNull(stored);
            Assert.Equal(2027, stored.Year);
            Assert.Equal(decimal.MaxValue, stored.Amount);
            Assert.Equal("Final", stored.Comment);
            Assert.Equal(pledge.CreatedAt, stored.CreatedAt);
            await repository.RemoveAsync(pledge.Id);
        }

        await using var readContext = CreateContext();
        var readRepository = new SqlServerPledgeRepository(readContext);
        Assert.Null(await readRepository.GetByIdAsync(pledge.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => readRepository.RemoveAsync(pledge.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Payment_mutations_persist_across_contexts(bool insertWithUpdate)
    {
        var pledge = await SeedPledgeAsync();
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2025, 1, 1));
        await using (var context = CreateContext())
        {
            var repository = CreatePaymentRepository(context);
            if (insertWithUpdate)
            {
                await repository.UpdateAsync(payment);
            }
            else
            {
                await repository.AddAsync(payment);
            }
        }

        await using (var context = CreateContext())
        {
            var repository = CreatePaymentRepository(context);
            var stored = await repository.GetByIdAsync(payment.Id);
            Assert.NotNull(stored);
            stored.Update(pledge.Id, 20m, new DateTime(2026, 10, 26), "After", true);
            await repository.AddAsync(stored);
            stored.Update(pledge.Id, decimal.MaxValue, new DateTime(2026, 10, 27), "Final", false);
            await repository.UpdateAsync(stored);
        }

        await using (var context = CreateContext())
        {
            var repository = CreatePaymentRepository(context);
            var stored = await repository.GetByIdAsync(payment.Id);
            Assert.NotNull(stored);
            Assert.Equal(decimal.MaxValue, stored.Amount);
            Assert.Equal(new DateTime(2026, 10, 27), stored.PaymentDate);
            Assert.Equal("Final", stored.Comment);
            Assert.False(stored.Active);
            Assert.Equal(payment.CreatedAt, stored.CreatedAt);
            Assert.Contains((await repository.GetByPledgeAsync(pledge.Id)).Items, entity => entity.Id == payment.Id);
            await repository.RemoveAsync(payment.Id);
        }

        await using var readContext = CreateContext();
        var readRepository = CreatePaymentRepository(readContext);
        Assert.Null(await readRepository.GetByIdAsync(payment.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => readRepository.RemoveAsync(payment.Id));
    }

    [Theory]
    [InlineData(SortOrder.Ascending)]
    [InlineData(SortOrder.Descending)]
    public async Task Pledger_queries_use_case_insensitive_names_and_sql_guid_ties(SortOrder sortOrder)
    {
        var pledgers = new[] { "alpha", "ALPHA", "beta", "Beta" }
            .Select(name => Pledger.Create(name)).ToList();
        await using (var context = CreateContext())
        {
            context.Pledgers.AddRange(pledgers);
            await context.SaveChangesAsync();
        }

        var expected = sortOrder == SortOrder.Descending
            ? pledgers.OrderByDescending(pledger => pledger.PledgerName, StringComparer.OrdinalIgnoreCase)
            : pledgers.OrderBy(pledger => pledger.PledgerName, StringComparer.OrdinalIgnoreCase);
        var expectedIds = expected.ThenBy(pledger => new SqlGuid(pledger.Id.Value)).Select(pledger => pledger.Id);
        await using var readContext = CreateContext();
        var repository = new SqlServerPledgerRepository(readContext);
        var result = await repository.GetAllAsync(new PledgerQuery(sortOrder: sortOrder));
        Assert.Equal(expectedIds, result.Items.Select(pledger => pledger.Id));
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(false, PledgeSortBy.Year, SortOrder.Ascending)]
    [InlineData(false, PledgeSortBy.Year, SortOrder.Descending)]
    [InlineData(false, PledgeSortBy.Amount, SortOrder.Ascending)]
    [InlineData(false, PledgeSortBy.Amount, SortOrder.Descending)]
    [InlineData(true, PledgeSortBy.Year, SortOrder.Ascending)]
    [InlineData(true, PledgeSortBy.Year, SortOrder.Descending)]
    [InlineData(true, PledgeSortBy.Amount, SortOrder.Ascending)]
    [InlineData(true, PledgeSortBy.Amount, SortOrder.Descending)]
    public async Task Pledge_queries_filter_sort_and_page_in_sql(
        bool byCategory, PledgeSortBy sortBy, SortOrder sortOrder)
    {
        var (pledger, category) = await SeedParentsAsync();
        var (otherPledger, otherCategory) = await SeedParentsAsync();
        var amounts = new[] { 10m, decimal.MaxValue, 2m, 10m, 0.01m };
        var pledges = amounts.Select((amount, index) =>
            Pledge.Create(2025 + index % 2, amount, pledger.Id, category.Id)).ToList();
        pledges.Add(Pledge.Create(2026, 50m, pledger.Id, otherCategory.Id));
        pledges.Add(Pledge.Create(2026, 60m, otherPledger.Id, category.Id));
        pledges.Add(Pledge.Create(2026, 70m, otherPledger.Id, otherCategory.Id));
        await using (var context = CreateContext())
        {
            context.Pledges.AddRange(pledges);
            await context.SaveChangesAsync();
        }

        var matches = pledges.Where(pledge => byCategory
            ? pledge.PledgeCategoryId == category.Id : pledge.PledgerId == pledger.Id);
        var expected = sortBy == PledgeSortBy.Amount
            ? sortOrder == SortOrder.Descending
                ? matches.OrderByDescending(pledge => pledge.Amount) : matches.OrderBy(pledge => pledge.Amount)
            : sortOrder == SortOrder.Descending
                ? matches.OrderByDescending(pledge => pledge.Year) : matches.OrderBy(pledge => pledge.Year);
        var expectedIds = expected.ThenByDescending(pledge => new SqlGuid(pledge.Id.Value))
            .Skip(2).Take(2).Select(pledge => pledge.Id);
        await using var readContext = CreateContext();
        var repository = new SqlServerPledgeRepository(readContext);
        var query = new PledgeQuery(2, 2, sortBy, sortOrder);
        var result = byCategory
            ? await repository.GetByCategoryAsync(category.Id, query)
            : await repository.GetByPledgerAsync(pledger.Id, query);
        Assert.Equal(expectedIds, result.Items.Select(pledge => pledge.Id));
        Assert.Equal(6, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(PledgePaymentSortBy.PaymentDate, SortOrder.Ascending)]
    [InlineData(PledgePaymentSortBy.PaymentDate, SortOrder.Descending)]
    [InlineData(PledgePaymentSortBy.Amount, SortOrder.Ascending)]
    [InlineData(PledgePaymentSortBy.Amount, SortOrder.Descending)]
    public async Task Payment_queries_filter_sort_and_page_in_sql(
        PledgePaymentSortBy sortBy, SortOrder sortOrder)
    {
        var pledge = await SeedPledgeAsync();
        var otherPledge = await SeedPledgeAsync();
        var amounts = new[] { 10m, decimal.MaxValue, 2m, 10m, 0.01m };
        var payments = amounts.Select((amount, index) => PledgePayment.Create(
            pledge.Id, amount, new DateTime(2026, 10, 26).AddDays(index % 2))).ToList();
        payments[0].Update(pledge.Id, payments[0].Amount, payments[0].PaymentDate, null, false);
        await using (var context = CreateContext())
        {
            context.PledgePayments.AddRange(payments);
            context.PledgePayments.Add(PledgePayment.Create(otherPledge.Id, 999m, new DateTime(2026, 1, 1)));
            await context.SaveChangesAsync();
        }

        var expected = sortBy == PledgePaymentSortBy.Amount
            ? sortOrder == SortOrder.Descending
                ? payments.OrderByDescending(payment => payment.Amount) : payments.OrderBy(payment => payment.Amount)
            : sortOrder == SortOrder.Descending
                ? payments.OrderByDescending(payment => payment.PaymentDate) : payments.OrderBy(payment => payment.PaymentDate);
        var expectedIds = expected.ThenByDescending(payment => new SqlGuid(payment.Id.Value))
            .Skip(2).Take(2).Select(payment => payment.Id);
        await using var readContext = CreateContext();
        var repository = CreatePaymentRepository(readContext);
        var result = await repository.GetByPledgeAsync(pledge.Id, new PledgePaymentQuery(2, 2, sortBy, sortOrder));
        Assert.Equal(expectedIds, result.Items.Select(payment => payment.Id));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Default_and_out_of_range_queries_preserve_totals_and_page_sizes()
    {
        var (pledger, category) = await SeedParentsAsync();
        var pledges = Enumerable.Range(0, 21)
            .Select(index => Pledge.Create(2020 + index, index + 1m, pledger.Id, category.Id)).ToList();
        var payments = Enumerable.Range(0, 21)
            .Select(index => PledgePayment.Create(pledges[0].Id, index + 1m, new DateTime(2026, 1, 1).AddDays(index)))
            .ToList();
        await using (var context = CreateContext())
        {
            context.Pledges.AddRange(pledges);
            context.PledgePayments.AddRange(payments);
            await context.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var pledgeRepository = new SqlServerPledgeRepository(readContext);
        var byPledger = await pledgeRepository.GetByPledgerAsync(pledger.Id);
        var byCategory = await pledgeRepository.GetByCategoryAsync(category.Id);
        Assert.Equal(20, byPledger.Items.Count);
        Assert.Equal(21, byPledger.TotalCount);
        Assert.Equal(2, byPledger.TotalPages);
        Assert.Equal(2040, byPledger.Items[0].Year);
        Assert.Equal(byPledger.Items.Select(pledge => pledge.Id), byCategory.Items.Select(pledge => pledge.Id));
        var beyondPledger = await pledgeRepository.GetByPledgerAsync(pledger.Id, new PledgeQuery(int.MaxValue, 100));
        var beyondCategory = await pledgeRepository.GetByCategoryAsync(category.Id, new PledgeQuery(int.MaxValue, 100));
        Assert.Empty(beyondPledger.Items);
        Assert.Empty(beyondCategory.Items);
        Assert.Equal(21, beyondPledger.TotalCount);
        Assert.Equal(21, beyondCategory.TotalCount);

        var paymentRepository = CreatePaymentRepository(readContext);
        var defaultPayments = await paymentRepository.GetByPledgeAsync(pledges[0].Id);
        Assert.Equal(20, defaultPayments.Items.Count);
        Assert.Equal(21, defaultPayments.TotalCount);
        Assert.Equal(2, defaultPayments.TotalPages);
        Assert.Equal(new DateTime(2026, 1, 21), defaultPayments.Items[0].PaymentDate);
        var beyondPayments = await paymentRepository.GetByPledgeAsync(pledges[0].Id, new PledgePaymentQuery(int.MaxValue, 100));
        Assert.Empty(beyondPayments.Items);
        Assert.Equal(21, beyondPayments.TotalCount);
    }

    [Fact]
    public async Task Empty_queries_and_unknown_id_reads_preserve_existing_outcomes()
    {
        await using var context = CreateContext();
        var pledgers = new SqlServerPledgerRepository(context);
        var categories = new SqlServerPledgeCategoryRepository(context);
        var pledges = new SqlServerPledgeRepository(context);
        var payments = CreatePaymentRepository(context);
        Assert.Null(await pledgers.GetByIdAsync(PledgerId.New()));
        Assert.Null(await categories.GetByIdAsync(PledgeCategoryId.New()));
        Assert.Null(await pledges.GetByIdAsync(PledgeId.New()));
        Assert.Null(await payments.GetByIdAsync(PledgePaymentId.New()));
        Assert.Empty(await categories.GetAllAsync());
        var unpagedPledgers = await pledgers.GetAllAsync();
        Assert.Empty(unpagedPledgers.Items);
        Assert.Equal(1, unpagedPledgers.PageSize);
        var pagedPledgers = await pledgers.GetAllAsync(new PledgerQuery());
        Assert.Empty(pagedPledgers.Items);
        Assert.Equal(0, pagedPledgers.TotalCount);
        var byPledger = await pledges.GetByPledgerAsync(PledgerId.New());
        var byCategory = await pledges.GetByCategoryAsync(PledgeCategoryId.New());
        var byPledge = await payments.GetByPledgeAsync(PledgeId.New());
        Assert.Empty(byPledger.Items);
        Assert.Empty(byCategory.Items);
        Assert.Empty(byPledge.Items);
        Assert.Equal(0, byPledger.TotalCount);
        Assert.Equal(0, byCategory.TotalCount);
        Assert.Equal(0, byPledge.TotalCount);
    }

    [Fact]
    public async Task Canceled_calls_do_not_read_or_mutate_the_context()
    {
        await using var context = CreateContext();
        var pledgers = new SqlServerPledgerRepository(context);
        var categories = new SqlServerPledgeCategoryRepository(context);
        var pledges = new SqlServerPledgeRepository(context);
        var payments = CreatePaymentRepository(context);
        var pledger = Pledger.Create("Canceled");
        var category = PledgeCategory.Create("Canceled");
        var pledge = Pledge.Create(2026, 100m, pledger.Id, category.Id);
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2026, 10, 27));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<CancellationToken, Task>[] operations =
        [
            token => pledgers.AddAsync(pledger, token),
            token => pledgers.UpdateAsync(pledger, token),
            token => pledgers.GetByIdAsync(pledger.Id, token),
            token => pledgers.GetAllAsync(cancellationToken: token),
            token => pledgers.GetAllAsync(new PledgerQuery(), token),
            token => pledgers.RemoveAsync(pledger.Id, token),
            token => categories.AddAsync(category, token),
            token => categories.UpdateAsync(category, token),
            token => categories.GetByIdAsync(category.Id, token),
            token => categories.GetAllAsync(token),
            token => categories.RemoveAsync(category.Id, token),
            token => pledges.AddAsync(pledge, token),
            token => pledges.UpdateAsync(pledge, token),
            token => pledges.GetByIdAsync(pledge.Id, token),
            token => pledges.GetByPledgerAsync(pledger.Id, cancellationToken: token),
            token => pledges.GetByCategoryAsync(category.Id, cancellationToken: token),
            token => pledges.RemoveAsync(pledge.Id, token),
            token => payments.AddAsync(payment, token),
            token => payments.UpdateAsync(payment, token),
            token => payments.GetByIdAsync(payment.Id, token),
            token => payments.GetByPledgeAsync(pledge.Id, cancellationToken: token),
            token => payments.RemoveAsync(payment.Id, token)
        ];
        foreach (var operation in operations)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation(cancellation.Token));
        }

        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Empty(await context.Pledgers.ToListAsync());
        Assert.Empty(await context.PledgeCategories.ToListAsync());
        Assert.Empty(await context.Pledges.ToListAsync());
        Assert.Empty(await context.PledgePayments.ToListAsync());
    }

    [Theory]
    [InlineData("Pledger")]
    [InlineData("Category")]
    [InlineData("Pledge")]
    public async Task Referenced_deletes_fail_without_deleting_related_records(string aggregate)
    {
        var pledge = await SeedPledgeAsync();
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2026, 10, 27));
        await using (var context = CreateContext())
        {
            await CreatePaymentRepository(context).AddAsync(payment);
        }

        await using (var context = CreateContext())
        {
            var exception = await Assert.ThrowsAsync<RelationshipConflictException>(() => aggregate switch
            {
                "Pledger" => new SqlServerPledgerRepository(context).RemoveAsync(pledge.PledgerId),
                "Category" => new SqlServerPledgeCategoryRepository(context).RemoveAsync(pledge.PledgeCategoryId),
                _ => new SqlServerPledgeRepository(context).RemoveAsync(pledge.Id)
            });
            var persistenceFailure = Assert.IsType<DbUpdateException>(exception.InnerException);
            Assert.Equal(547, Assert.IsType<SqlException>(persistenceFailure.InnerException).Number);
        }

        await using var readContext = CreateContext();
        Assert.NotNull(await new SqlServerPledgerRepository(readContext).GetByIdAsync(pledge.PledgerId));
        Assert.NotNull(await new SqlServerPledgeCategoryRepository(readContext).GetByIdAsync(pledge.PledgeCategoryId));
        Assert.NotNull(await new SqlServerPledgeRepository(readContext).GetByIdAsync(pledge.Id));
        Assert.NotNull(await CreatePaymentRepository(readContext).GetByIdAsync(payment.Id));
        await CreatePaymentRepository(readContext).RemoveAsync(payment.Id);
        await new SqlServerPledgeRepository(readContext).RemoveAsync(pledge.Id);
        await new SqlServerPledgerRepository(readContext).RemoveAsync(pledge.PledgerId);
        await new SqlServerPledgeCategoryRepository(readContext).RemoveAsync(pledge.PledgeCategoryId);
    }

    [Theory]
    [InlineData("Pledger", false)]
    [InlineData("Pledger", true)]
    [InlineData("Category", false)]
    [InlineData("Category", true)]
    [InlineData("Pledge", false)]
    [InlineData("Pledge", true)]
    public async Task Invalid_reference_writes_fail_without_corrupting_saved_records(
        string missingReference, bool update)
    {
        var pledge = await SeedPledgeAsync();
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2026, 10, 27));
        await using (var context = CreateContext())
        {
            await CreatePaymentRepository(context).AddAsync(payment);
        }

        var originalPledgerId = pledge.PledgerId;
        var originalCategoryId = pledge.PledgeCategoryId;
        await using (var context = CreateContext())
        {
            Task WriteInvalidReferenceAsync()
            {
                if (missingReference == "Pledge")
                {
                    var invalidPayment = update ? payment
                        : PledgePayment.Create(pledge.Id, 20m, new DateTime(2026, 10, 27));
                    invalidPayment.Update(PledgeId.New(), 20m, invalidPayment.PaymentDate, null, true);
                    var repository = CreatePaymentRepository(context);
                    return update ? repository.UpdateAsync(invalidPayment) : repository.AddAsync(invalidPayment);
                }

                var invalidPledge = update ? pledge : Pledge.Create(2026, 50m, originalPledgerId, originalCategoryId);
                invalidPledge.Update(2026, 50m,
                    missingReference == "Pledger" ? PledgerId.New() : originalPledgerId,
                    missingReference == "Category" ? PledgeCategoryId.New() : originalCategoryId);
                var pledgeRepository = new SqlServerPledgeRepository(context);
                return update ? pledgeRepository.UpdateAsync(invalidPledge) : pledgeRepository.AddAsync(invalidPledge);
            }

            var exception = await Assert.ThrowsAsync<RelationshipConflictException>(WriteInvalidReferenceAsync);
            var persistenceFailure = Assert.IsType<DbUpdateException>(exception.InnerException);
            Assert.Equal(547, Assert.IsType<SqlException>(persistenceFailure.InnerException).Number);
        }

        await using var readContext = CreateContext();
        var storedPledge = await new SqlServerPledgeRepository(readContext).GetByIdAsync(pledge.Id);
        var storedPayment = await CreatePaymentRepository(readContext).GetByIdAsync(payment.Id);
        Assert.NotNull(storedPledge);
        Assert.NotNull(storedPayment);
        Assert.Equal(originalPledgerId, storedPledge.PledgerId);
        Assert.Equal(originalCategoryId, storedPledge.PledgeCategoryId);
        Assert.Equal(pledge.Id, storedPayment.PledgeId);
        Assert.Equal(100m, storedPledge.Amount);
        Assert.Equal(10m, storedPayment.Amount);
        Assert.Equal(1, await readContext.Pledges.CountAsync());
        Assert.Equal(1, await readContext.PledgePayments.CountAsync());
    }

    [Fact]
    public async Task Tracked_mutations_and_relationship_changes_persist_without_duplicate_tracking()
    {
        var (otherPledger, otherCategory) = await SeedParentsAsync();
        var otherPledge = await SeedPledgeAsync();
        var pledger = Pledger.Create("Tracked pledger");
        var category = PledgeCategory.Create("Tracked category");
        var pledge = Pledge.Create(2025, 50m, pledger.Id, category.Id);
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2026, 1, 1));
        await using (var context = CreateContext())
        {
            context.AddRange(pledger, category, pledge, payment);
            await context.SaveChangesAsync();
            pledger.Update("Updated tracked pledger", false);
            await new SqlServerPledgerRepository(context).UpdateAsync(pledger);
            category.Update("Updated tracked category", false);
            await new SqlServerPledgeCategoryRepository(context).AddAsync(category);
            pledge.Update(2026, 100m, otherPledger.Id, otherCategory.Id);
            await new SqlServerPledgeRepository(context).UpdateAsync(pledge);
            payment.Update(otherPledge.Id, 20m, new DateTime(2026, 10, 27), null, false);
            await CreatePaymentRepository(context).AddAsync(payment);
        }

        await using var readContext = CreateContext();
        var storedPledger = await new SqlServerPledgerRepository(readContext).GetByIdAsync(pledger.Id);
        var storedCategory = await new SqlServerPledgeCategoryRepository(readContext).GetByIdAsync(category.Id);
        var storedPledge = await new SqlServerPledgeRepository(readContext).GetByIdAsync(pledge.Id);
        var storedPayment = await CreatePaymentRepository(readContext).GetByIdAsync(payment.Id);
        Assert.NotNull(storedPledger);
        Assert.NotNull(storedCategory);
        Assert.NotNull(storedPledge);
        Assert.NotNull(storedPayment);
        Assert.Equal("Updated tracked pledger", storedPledger.PledgerName);
        Assert.False(storedPledger.Active);
        Assert.Equal("Updated tracked category", storedCategory.CategoryName);
        Assert.False(storedCategory.Active);
        Assert.Equal(otherPledger.Id, storedPledge.PledgerId);
        Assert.Equal(otherCategory.Id, storedPledge.PledgeCategoryId);
        Assert.Equal(otherPledge.Id, storedPayment.PledgeId);
        Assert.False(storedPayment.Active);
        Assert.Empty(readContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Null_aggregate_mutations_fail_before_tracking_changes()
    {
        await using var context = CreateContext();
        var pledgers = new SqlServerPledgerRepository(context);
        var categories = new SqlServerPledgeCategoryRepository(context);
        var pledges = new SqlServerPledgeRepository(context);
        var payments = CreatePaymentRepository(context);
        await Assert.ThrowsAsync<ArgumentNullException>(() => pledgers.AddAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => pledgers.UpdateAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => categories.AddAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => categories.UpdateAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => pledges.AddAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => pledges.UpdateAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => payments.AddAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => payments.UpdateAsync(null!));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Materialized_lists_remain_unchanged_after_later_writes(bool paginated)
    {
        await using var context = CreateContext();
        var pledgers = new SqlServerPledgerRepository(context);
        var categories = new SqlServerPledgeCategoryRepository(context);
        var originalPledger = Pledger.Create("Original pledger");
        var originalCategory = PledgeCategory.Create("Original category");
        await pledgers.AddAsync(originalPledger);
        await categories.AddAsync(originalCategory);
        var pledgerSnapshot = await pledgers.GetAllAsync(paginated ? new PledgerQuery() : null);
        var categorySnapshot = await categories.GetAllAsync();

        var remainingPledger = Pledger.Create("Remaining pledger");
        var remainingCategory = PledgeCategory.Create("Remaining category");
        await pledgers.AddAsync(remainingPledger);
        await categories.AddAsync(remainingCategory);
        await pledgers.RemoveAsync(originalPledger.Id);
        await categories.RemoveAsync(originalCategory.Id);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => pledgers.RemoveAsync(PledgerId.New()));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => categories.RemoveAsync(PledgeCategoryId.New()));

        Assert.Equal(originalPledger.Id, Assert.Single(pledgerSnapshot.Items).Id);
        Assert.Equal(1, pledgerSnapshot.TotalCount);
        Assert.Equal(originalCategory.Id, Assert.Single(categorySnapshot).Id);
        Assert.Equal(remainingPledger.Id, Assert.Single((await pledgers.GetAllAsync()).Items).Id);
        Assert.Equal(remainingCategory.Id, Assert.Single(await categories.GetAllAsync()).Id);
    }

    [Fact]
    public async Task Canceled_mutations_preserve_existing_records()
    {
        var pledge = await SeedPledgeAsync();
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2026, 10, 27));
        await using (var context = CreateContext())
        {
            await CreatePaymentRepository(context).AddAsync(payment);
        }

        await using (var context = CreateContext())
        {
            var pledgers = new SqlServerPledgerRepository(context);
            var categories = new SqlServerPledgeCategoryRepository(context);
            var pledges = new SqlServerPledgeRepository(context);
            var payments = CreatePaymentRepository(context);
            var pledger = await pledgers.GetByIdAsync(pledge.PledgerId);
            var category = await categories.GetByIdAsync(pledge.PledgeCategoryId);
            Assert.NotNull(pledger);
            Assert.NotNull(category);
            pledger.Update("Canceled name", false);
            category.Update("Canceled category", false);
            pledge.Update(2027, 200m, pledge.PledgerId, pledge.PledgeCategoryId);
            payment.Update(pledge.Id, 20m, payment.PaymentDate, null, false);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Func<CancellationToken, Task>[] operations =
            [
                token => pledgers.UpdateAsync(pledger, token),
                token => pledgers.RemoveAsync(pledger.Id, token),
                token => categories.UpdateAsync(category, token),
                token => categories.RemoveAsync(category.Id, token),
                token => pledges.UpdateAsync(pledge, token),
                token => pledges.RemoveAsync(pledge.Id, token),
                token => payments.UpdateAsync(payment, token),
                token => payments.RemoveAsync(payment.Id, token)
            ];
            foreach (var operation in operations)
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation(cancellation.Token));
            }

            Assert.Empty(context.ChangeTracker.Entries());
        }

        await using var readContext = CreateContext();
        var storedPledger = await new SqlServerPledgerRepository(readContext).GetByIdAsync(pledge.PledgerId);
        var storedCategory = await new SqlServerPledgeCategoryRepository(readContext).GetByIdAsync(pledge.PledgeCategoryId);
        var storedPledge = await new SqlServerPledgeRepository(readContext).GetByIdAsync(pledge.Id);
        var storedPayment = await CreatePaymentRepository(readContext).GetByIdAsync(payment.Id);
        Assert.NotNull(storedPledger);
        Assert.NotNull(storedCategory);
        Assert.NotNull(storedPledge);
        Assert.NotNull(storedPayment);
        Assert.Equal("Test pledger", storedPledger.PledgerName);
        Assert.Equal("Test category", storedCategory.CategoryName);
        Assert.True(storedPledger.Active);
        Assert.True(storedCategory.Active);
        Assert.Equal(2026, storedPledge.Year);
        Assert.Equal(100m, storedPledge.Amount);
        Assert.Equal(10m, storedPayment.Amount);
        Assert.True(storedPayment.Active);
    }

    private async Task<(Pledger Pledger, PledgeCategory Category)> SeedParentsAsync()
    {
        var pledger = Pledger.Create("Test pledger");
        var category = PledgeCategory.Create("Test category");
        await using var context = CreateContext();
        context.AddRange(pledger, category);
        await context.SaveChangesAsync();
        return (pledger, category);
    }

    private async Task<Pledge> SeedPledgeAsync()
    {
        var (pledger, category) = await SeedParentsAsync();
        var pledge = Pledge.Create(2026, 100m, pledger.Id, category.Id);
        await using var context = CreateContext();
        context.Pledges.Add(pledge);
        await context.SaveChangesAsync();
        return pledge;
    }

    private static SqlServerPledgePaymentRepository CreatePaymentRepository(ICFAccountingDbContext context)
    {
        return new SqlServerPledgePaymentRepository(context, NullLogger<SqlServerPledgePaymentRepository>.Instance);
    }

    private ICFAccountingDbContext CreateContext()
    {
        return fixture.CreateContext(_databaseName);
    }
}