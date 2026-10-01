using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Abstractions;
using Portfolio.Application.Common;
using Portfolio.Application.Positions;
using Portfolio.Domain.Entities;

namespace Portfolio.UnitTests.Positions;

/// <summary>
/// <see cref="PositionService"/> avec un validateur, un conseiller et une horloge simulés par Moq.
/// </summary>
[TestFixture]
public class PositionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 9, 30, 0, TimeSpan.Zero);
    private static readonly PositionRequest Request = new("Swisscom AG", "Equities", 20m, 520.5m, "CHF");
    private static readonly ValidPosition Valid = new("Swisscom AG", AssetClass.Equities, 20m, 520.5m, "CHF");

    private TestDatabase _database = null!;
    private Mock<ICurrentAdvisor> _advisor = null!;
    private Mock<IPositionValidator> _validator = null!;
    private Mock<TimeProvider> _clock = null!;
    private IPositionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _database = new TestDatabase();
        _advisor = new Mock<ICurrentAdvisor>();
        _advisor.Setup(a => a.Id).Returns(TestDatabase.AliceId);
        _validator = new Mock<IPositionValidator>();
        _validator.Setup(v => v.Validate(It.IsAny<PositionRequest?>(), It.IsAny<IReadOnlyCollection<string>>()))
            .Returns((Valid, new Dictionary<string, string[]>()));
        // TimeProvider est une classe abstraite : Moq en simule les méthodes virtuelles.
        _clock = new Mock<TimeProvider>();
        _clock.Setup(c => c.GetUtcNow()).Returns(Now);

        _service = new PositionService(_database.Context, _advisor.Object, _validator.Object, _clock.Object);
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task Create_saves_the_validated_position_and_its_audit_entry()
    {
        var result = await _service.CreateAsync(_database.Rochat.Id, Request);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(result.Value!.MarketValue, Is.EqualTo(10_410m));

        var audit = await _database.Context.PositionAudit.SingleAsync();
        Assert.That(audit.Action, Is.EqualTo(AuditAction.Created));
        Assert.That(audit.AdvisorId, Is.EqualTo(TestDatabase.AliceId), "l'auteur vient du conseiller injecté");
        Assert.That(audit.OccurredAt, Is.EqualTo(Now), "la date vient de l'horloge injectée");
        Assert.That(audit.PositionId, Is.EqualTo(result.Value.Id));
    }

    [Test]
    public async Task Validator_receives_the_request_and_the_known_currencies()
    {
        await _service.CreateAsync(_database.Rochat.Id, Request);

        _validator.Verify(v => v.Validate(Request, It.Is<IReadOnlyCollection<string>>(c => c.OrderBy(x => x).SequenceEqual(new[] { "CHF", "USD" }))), Times.Once);
    }

    [Test]
    public async Task Validation_errors_are_returned_and_nothing_is_saved()
    {
        var errors = new Dictionary<string, string[]> { ["quantity"] = ["La quantité doit être strictement positive."] };
        _validator.Setup(v => v.Validate(It.IsAny<PositionRequest?>(), It.IsAny<IReadOnlyCollection<string>>()))
            .Returns(((ValidPosition?)null, errors));

        var result = await _service.CreateAsync(_database.Rochat.Id, Request);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.That(result.Errors, Is.EqualTo(errors));
        Assert.That(await _database.Context.Positions.CountAsync(), Is.EqualTo(2));
        Assert.That(await _database.Context.PositionAudit.AnyAsync(), Is.False);
    }

    [Test]
    public async Task Another_advisor_cannot_add_a_position_and_validation_is_not_even_run()
    {
        _advisor.Setup(a => a.Id).Returns(TestDatabase.BrunoId);

        var result = await _service.CreateAsync(_database.Rochat.Id, Request);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
        _validator.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Delete_removes_the_position_and_records_who_deleted_it()
    {
        var positionId = await _database.Context.Positions.Select(p => p.Id).FirstAsync();

        var result = await _service.DeleteAsync(_database.Rochat.Id, positionId);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Ok));
        Assert.That(await _database.Context.Positions.AnyAsync(p => p.Id == positionId), Is.False);
        var audit = await _database.Context.PositionAudit.SingleAsync();
        Assert.That((audit.Action, audit.AdvisorId, audit.After), Is.EqualTo((AuditAction.Deleted, TestDatabase.AliceId, (string?)null)));
        _validator.VerifyNoOtherCalls();
    }
}
