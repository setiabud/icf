using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ICFAccounting.IntegrationTests.Data;

[Collection(nameof(SqlServerTestCollection))]
public sealed class ICFAccountingDbContextTests(SqlServerModelFixture fixture)
{
    [Fact]
    public async Task Database_migration_is_applied_once_and_matches_the_model()
    {
        await using var context = fixture.CreateContext();
        var migration = Assert.Single(context.Database.GetMigrations());
        Assert.EndsWith("_InitialCreate", migration);
        Assert.Equal(migration, Assert.Single(await context.Database.GetAppliedMigrationsAsync()));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());

        await context.Database.MigrateAsync();

        Assert.Equal(migration, Assert.Single(await context.Database.GetAppliedMigrationsAsync()));
    }

    [Fact]
    public async Task Database_migration_creates_the_expected_sql_schema()
    {
        await using var context = fixture.CreateContext();
        var tables = await context.Database.SqlQueryRaw<string>(
            "SELECT [name] AS [Value] FROM sys.tables WHERE [schema_id] = SCHEMA_ID(N'dbo') AND [name] <> N'__EFMigrationsHistory'")
            .OrderBy(tableName => tableName).ToListAsync();
        Assert.Equal(["PledgeCategories", "PledgePayments", "Pledgers", "Pledges"], tables);

        var collation = await context.Database.SqlQueryRaw<string>(
            "SELECT CAST(DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS nvarchar(128)) AS [Value]")
            .SingleAsync();
        Assert.Equal("Latin1_General_100_CI_AS_SC", collation);

        var amountColumns = await context.Database.SqlQueryRaw<int>(
            """
            SELECT COUNT(*) AS [Value] FROM sys.columns AS columns
            INNER JOIN sys.tables AS tables ON columns.object_id = tables.object_id
            WHERE tables.schema_id = SCHEMA_ID(N'dbo')
                AND tables.name IN (N'Pledges', N'PledgePayments')
                AND columns.name = N'Amount' AND columns.precision = 38 AND columns.scale = 2
            """).SingleAsync();
        Assert.Equal(2, amountColumns);

        var restrictedRelationships = await context.Database.SqlQueryRaw<int>(
            """
            SELECT COUNT(*) AS [Value] FROM sys.foreign_keys
            WHERE schema_id = SCHEMA_ID(N'dbo') AND delete_referential_action = 0
            """).SingleAsync();
        Assert.Equal(3, restrictedRelationships);
    }

    [Fact]
    public async Task Database_migration_can_roll_back_and_reapply_in_an_isolated_database()
    {
        await using var context = fixture.CreateContext($"ICFAccountingRollbackTests_{Guid.NewGuid():N}");
        try
        {
            await context.Database.MigrateAsync();
            var pledger = Pledger.Create("Rollback test pledger");
            var category = PledgeCategory.Create("Rollback test category");
            var pledge = Pledge.Create(2026, 100m, pledger.Id, category.Id);
            var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2026, 10, 27));
            context.AddRange(pledger, category, pledge, payment);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            await context.GetService<IMigrator>().MigrateAsync(Migration.InitialDatabase);

            Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
            var remainingTables = await context.Database.SqlQueryRaw<string>(
                "SELECT [name] AS [Value] FROM sys.tables WHERE [schema_id] = SCHEMA_ID(N'dbo') AND [name] <> N'__EFMigrationsHistory'")
                .ToListAsync();
            Assert.Empty(remainingTables);

            await context.Database.MigrateAsync();

            Assert.Single(await context.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await context.Pledgers.ToListAsync());
            Assert.Empty(await context.PledgeCategories.ToListAsync());
            Assert.Empty(await context.Pledges.ToListAsync());
            Assert.Empty(await context.PledgePayments.ToListAsync());
        }
        finally
        {
            await context.Database.EnsureDeletedAsync();
        }
    }

    [Theory]
    [InlineData(typeof(Pledger), "Pledgers", 5)]
    [InlineData(typeof(PledgeCategory), "PledgeCategories", 5)]
    [InlineData(typeof(Pledge), "Pledges", 8)]
    [InlineData(typeof(PledgePayment), "PledgePayments", 8)]
    public void Model_maps_all_properties_and_client_generated_identity(
        Type entityType, string tableName, int propertyCount)
    {
        using var context = fixture.CreateContext();
        var entity = context.Model.FindEntityType(entityType)!;
        Assert.Equal(tableName, entity.GetTableName());
        Assert.Equal(propertyCount, entity.GetProperties().Count());
        Assert.DoesNotContain(entity.GetProperties(), property => property.IsShadowProperty());

        var identity = Assert.Single(entity.FindPrimaryKey()!.Properties);
        Assert.Equal("Id", identity.Name);
        Assert.Equal(ValueGenerated.Never, identity.ValueGenerated);
        Assert.Equal(typeof(Guid), identity.GetTypeMapping().Converter!.ProviderClrType);
        Assert.Equal("uniqueidentifier", identity.GetColumnType());
        Assert.Equal("datetime2(7)", entity.FindProperty("CreatedAt")!.GetColumnType());
        Assert.Equal("datetime2(7)", entity.FindProperty("UpdatedAt")!.GetColumnType());
    }

    [Fact]
    public void Model_maps_only_required_non_cascading_foreign_keys()
    {
        using var context = fixture.CreateContext();
        var foreignKeys = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys()).ToList();
        Assert.Equal(3, foreignKeys.Count);
        Assert.All(foreignKeys, foreignKey =>
        {
            Assert.True(foreignKey.IsRequired);
            Assert.Equal(DeleteBehavior.NoAction, foreignKey.DeleteBehavior);
            Assert.Null(foreignKey.DependentToPrincipal);
            Assert.Null(foreignKey.PrincipalToDependent);
            Assert.Equal(typeof(Guid), Assert.Single(foreignKey.Properties)
                .GetTypeMapping().Converter!.ProviderClrType);
        });
    }

    [Fact]
    public void Model_preserves_unbounded_strings_and_explicit_sql_collation()
    {
        using var context = fixture.CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        Assert.Equal("Latin1_General_100_CI_AS_SC", model.GetCollation());
        var strings = model.GetEntityTypes().SelectMany(entity => entity.GetProperties())
            .Where(property => property.ClrType == typeof(string)).ToList();
        Assert.Equal(4, strings.Count);
        Assert.All(strings, property =>
        {
            Assert.Equal("nvarchar(max)", property.GetColumnType());
            Assert.Null(property.GetMaxLength());
            Assert.Equal(property.Name == "Comment", property.IsNullable);
        });
    }

    [Theory]
    [InlineData(typeof(Pledge))]
    [InlineData(typeof(PledgePayment))]
    public void Model_maps_amounts_without_reducing_domain_precision(Type entityType)
    {
        using var context = fixture.CreateContext();
        var amount = context.Model.FindEntityType(entityType)!.FindProperty("Amount")!;
        Assert.Equal(38, amount.GetPrecision());
        Assert.Equal(2, amount.GetScale());
        Assert.Equal("decimal(38,2)", amount.GetColumnType());
    }

    [Fact]
    public async Task SaveChanges_round_trips_all_aggregates_across_contexts()
    {
        var pledger = Pledger.Create(new string('P', 500));
        var category = PledgeCategory.Create(new string('C', 500));
        var pledge = Pledge.Create(2026, decimal.MaxValue, pledger.Id, category.Id, new string('A', 5000));
        var paymentDate = new DateTime(2026, 10, 27, 12, 30, 0, DateTimeKind.Unspecified).AddTicks(1234);
        var payment = PledgePayment.Create(pledge.Id, 12345678901234567890123456.78m, paymentDate);

        await using (var context = fixture.CreateContext())
        {
            context.AddRange(pledger, category, pledge, payment);
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var storedPledger = await readContext.Pledgers.SingleAsync(entity => entity.Id == pledger.Id);
        var storedCategory = await readContext.PledgeCategories.SingleAsync(entity => entity.Id == category.Id);
        var storedPledge = await readContext.Pledges.SingleAsync(entity => entity.Id == pledge.Id);
        var storedPayment = await readContext.PledgePayments.SingleAsync(entity => entity.Id == payment.Id);

        Assert.Equal(pledger.PledgerName, storedPledger.PledgerName);
        Assert.Equal(category.CategoryName, storedCategory.CategoryName);
        Assert.Equal(pledge.PledgerId, storedPledge.PledgerId);
        Assert.Equal(pledge.PledgeCategoryId, storedPledge.PledgeCategoryId);
        Assert.Equal(pledge.Year, storedPledge.Year);
        Assert.Equal(pledge.Amount, storedPledge.Amount);
        Assert.Equal(pledge.Comment, storedPledge.Comment);
        Assert.Equal(payment.PledgeId, storedPayment.PledgeId);
        Assert.Equal(payment.Amount, storedPayment.Amount);
        Assert.Equal(paymentDate, storedPayment.PaymentDate);
        Assert.Null(storedPayment.Comment);
        Assert.True(storedPledger.Active);
        Assert.True(storedCategory.Active);
        Assert.True(storedPayment.Active);

        AssertUtcTimestamp(pledger.CreatedAt, storedPledger.CreatedAt);
        AssertUtcTimestamp(pledger.UpdatedAt, storedPledger.UpdatedAt);
        AssertUtcTimestamp(category.CreatedAt, storedCategory.CreatedAt);
        AssertUtcTimestamp(category.UpdatedAt, storedCategory.UpdatedAt);
        AssertUtcTimestamp(pledge.CreatedAt, storedPledge.CreatedAt);
        AssertUtcTimestamp(pledge.UpdatedAt, storedPledge.UpdatedAt);
        AssertUtcTimestamp(payment.CreatedAt, storedPayment.CreatedAt);
        AssertUtcTimestamp(payment.UpdatedAt, storedPayment.UpdatedAt);
    }

    [Fact]
    public async Task SaveChanges_persists_updates_and_keeps_inactive_entities_visible()
    {
        var pledger = Pledger.Create("Before");
        var category = PledgeCategory.Create("Before");
        var pledge = Pledge.Create(2025, 50m, pledger.Id, category.Id);
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2025, 1, 1));
        await using (var context = fixture.CreateContext())
        {
            context.AddRange(pledger, category, pledge, payment);
            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            var storedPledger = await context.Pledgers.SingleAsync(entity => entity.Id == pledger.Id);
            var storedCategory = await context.PledgeCategories.SingleAsync(entity => entity.Id == category.Id);
            var storedPledge = await context.Pledges.SingleAsync(entity => entity.Id == pledge.Id);
            var storedPayment = await context.PledgePayments.SingleAsync(entity => entity.Id == payment.Id);
            storedPledger.Update("After", false);
            storedCategory.Update("After", false);
            storedPledge.Update(2026, 100m, pledger.Id, category.Id, "Updated pledge");
            storedPayment.Update(pledge.Id, 20m, new DateTime(2026, 10, 27), "Updated payment", false);
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var updatedPledger = await readContext.Pledgers.SingleAsync(entity => entity.Id == pledger.Id);
        var updatedCategory = await readContext.PledgeCategories.SingleAsync(entity => entity.Id == category.Id);
        var updatedPledge = await readContext.Pledges.SingleAsync(entity => entity.Id == pledge.Id);
        var updatedPayment = await readContext.PledgePayments.SingleAsync(entity => entity.Id == payment.Id);
        Assert.Equal("After", updatedPledger.PledgerName);
        Assert.False(updatedPledger.Active);
        Assert.Equal("After", updatedCategory.CategoryName);
        Assert.False(updatedCategory.Active);
        Assert.Equal(2026, updatedPledge.Year);
        Assert.Equal(100m, updatedPledge.Amount);
        Assert.Equal("Updated pledge", updatedPledge.Comment);
        Assert.Equal(20m, updatedPayment.Amount);
        Assert.Equal(new DateTime(2026, 10, 27), updatedPayment.PaymentDate);
        Assert.Equal("Updated payment", updatedPayment.Comment);
        Assert.False(updatedPayment.Active);
        AssertUtcTimestamp(pledge.CreatedAt, updatedPledge.CreatedAt);
        Assert.True(updatedPledge.UpdatedAt >= updatedPledge.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, updatedPledge.UpdatedAt.Kind);
    }

    [Theory]
    [InlineData("Pledger")]
    [InlineData("PledgeCategory")]
    [InlineData("Pledge")]
    public async Task SaveChanges_rejects_deleting_referenced_aggregates(string aggregate)
    {
        var pledger = Pledger.Create("Referenced pledger");
        var category = PledgeCategory.Create("Referenced category");
        var pledge = Pledge.Create(2026, 100m, pledger.Id, category.Id);
        var payment = PledgePayment.Create(pledge.Id, 10m, new DateTime(2026, 10, 27));
        await using (var context = fixture.CreateContext())
        {
            context.AddRange(pledger, category, pledge, payment);
            await context.SaveChangesAsync();
        }

        await using var deleteContext = fixture.CreateContext();
        object referenced = aggregate switch
        {
            "Pledger" => await deleteContext.Pledgers.SingleAsync(entity => entity.Id == pledger.Id),
            "PledgeCategory" => await deleteContext.PledgeCategories.SingleAsync(entity => entity.Id == category.Id),
            _ => await deleteContext.Pledges.SingleAsync(entity => entity.Id == pledge.Id)
        };
        deleteContext.Remove(referenced);
        var exception = await Assert.ThrowsAsync<RelationshipConflictException>(() => deleteContext.SaveChangesAsync());
        var persistenceFailure = Assert.IsType<DbUpdateException>(exception.InnerException);
        Assert.Equal(547, Assert.IsType<SqlException>(persistenceFailure.InnerException).Number);

        await using var readContext = fixture.CreateContext();
        Assert.True(await readContext.Pledgers.AnyAsync(entity => entity.Id == pledger.Id));
        Assert.True(await readContext.PledgeCategories.AnyAsync(entity => entity.Id == category.Id));
        Assert.True(await readContext.Pledges.AnyAsync(entity => entity.Id == pledge.Id));
        Assert.True(await readContext.PledgePayments.AnyAsync(entity => entity.Id == payment.Id));
    }

    [Theory]
    [InlineData("Pledger", false)]
    [InlineData("Pledger", true)]
    [InlineData("PledgeCategory", false)]
    [InlineData("PledgeCategory", true)]
    [InlineData("Pledge", false)]
    [InlineData("Pledge", true)]
    public async Task SaveChanges_rejects_missing_foreign_key_references(string missingReference, bool synchronous)
    {
        var pledger = Pledger.Create("Existing pledger");
        var category = PledgeCategory.Create("Existing category");
        await using var context = fixture.CreateContext();
        context.AddRange(pledger, category);
        await context.SaveChangesAsync();

        if (missingReference == "Pledge")
        {
            context.PledgePayments.Add(PledgePayment.Create(PledgeId.New(), 10m, new DateTime(2026, 10, 27)));
        }
        else
        {
            context.Pledges.Add(Pledge.Create(2026, 100m,
                missingReference == "Pledger" ? PledgerId.New() : pledger.Id,
                missingReference == "PledgeCategory" ? PledgeCategoryId.New() : category.Id));
        }

        var exception = synchronous
            ? Assert.Throws<RelationshipConflictException>(() => context.SaveChanges())
            : await Assert.ThrowsAsync<RelationshipConflictException>(() => context.SaveChangesAsync());
        var persistenceFailure = Assert.IsType<DbUpdateException>(exception.InnerException);
        Assert.Equal(547, Assert.IsType<SqlException>(persistenceFailure.InnerException).Number);
    }

    [Fact]
    public async Task Amounts_support_numeric_filtering_ordering_and_scalar_projection()
    {
        decimal[] amounts =
        [
            0.01m, 2m, 10m, 792281625142643375935439503.35m,
            7922816251426433759354395033.5m, decimal.MaxValue
        ];
        var pledger = Pledger.Create("Amount query pledger");
        var category = PledgeCategory.Create("Amount query category");
        var pledges = amounts.Select(amount => Pledge.Create(2026, amount, pledger.Id, category.Id)).ToList();
        var payments = amounts.Select(amount => PledgePayment.Create(
            pledges[0].Id, amount, new DateTime(2026, 10, 27))).ToList();
        await using (var context = fixture.CreateContext())
        {
            context.AddRange(pledger, category);
            context.Pledges.AddRange(pledges);
            context.PledgePayments.AddRange(payments);
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var minimum = amounts[0];
        var maximum = amounts[^1];
        var paymentPledgeId = pledges[0].Id;
        var storedPledgeAmounts = await readContext.Pledges
            .Where(pledge => pledge.PledgerId == pledger.Id && pledge.Amount >= minimum)
            .OrderBy(pledge => pledge.Amount).Select(pledge => pledge.Amount).ToListAsync();
        var storedPaymentAmounts = await readContext.PledgePayments
            .Where(payment => payment.PledgeId == paymentPledgeId && payment.Amount >= minimum)
            .OrderBy(payment => payment.Amount).Select(payment => payment.Amount).ToListAsync();
        Assert.Equal(amounts, storedPledgeAmounts);
        Assert.Equal(amounts, storedPaymentAmounts);
        Assert.True(await readContext.Pledges.AnyAsync(
            pledge => pledge.PledgerId == pledger.Id && pledge.Amount == maximum));
        Assert.True(await readContext.PledgePayments.AnyAsync(
            payment => payment.PledgeId == paymentPledgeId && payment.Amount == maximum));
    }

    [Fact]
    public async Task Amount_materialization_rejects_values_that_would_require_rounding()
    {
        var pledger = Pledger.Create("Decimal boundary pledger");
        var category = PledgeCategory.Create("Decimal boundary category");
        var pledge = Pledge.Create(2026, 100m, pledger.Id, category.Id);
        await using (var context = fixture.CreateContext())
        {
            context.AddRange(pledger, category, pledge);
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE [dbo].[Pledges] SET [Amount] = CAST('7922816251426433759354395033.51' AS decimal(38,2)) WHERE [Id] = {pledge.Id.Value}");
        }

        await using var readContext = fixture.CreateContext();
        await Assert.ThrowsAsync<OverflowException>(() => readContext.Pledges
            .Where(entity => entity.Id == pledge.Id).Select(entity => entity.Amount).SingleAsync());
    }

    private static void AssertUtcTimestamp(DateTime expected, DateTime actual)
    {
        Assert.Equal(expected, actual);
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
    }
}