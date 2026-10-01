using Microsoft.AspNetCore.Identity;
using Portfolio.Application.Auth;
using Portfolio.Domain.Entities;

namespace Portfolio.UnitTests.Auth;

/// <summary><see cref="AuthService"/> avec un hacheur de mots de passe simulé par Moq.</summary>
[TestFixture]
public class AuthServiceTests
{
    private TestDatabase _database = null!;
    private Mock<IPasswordHasher<Advisor>> _hasher = null!;
    private IAuthService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _database = new TestDatabase();
        _hasher = new Mock<IPasswordHasher<Advisor>>();
        _service = new AuthService(_database.Context, _hasher.Object);
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    private void HashCheckReturns(PasswordVerificationResult result) =>
        _hasher.Setup(h => h.VerifyHashedPassword(It.IsAny<Advisor>(), It.IsAny<string>(), It.IsAny<string>())).Returns(result);

    [Test]
    public async Task Valid_password_authenticates_the_advisor()
    {
        HashCheckReturns(PasswordVerificationResult.Success);

        var advisor = await _service.AuthenticateAsync(" AMartin ", "secret");

        Assert.That(advisor, Is.EqualTo(new AuthenticatedAdvisor(TestDatabase.AliceId, "amartin", "Alice Martin")));
        _hasher.Verify(h => h.VerifyHashedPassword(It.Is<Advisor>(a => a.UserName == "amartin"), "hash-alice", "secret"), Times.Once);
    }

    [Test]
    public async Task Wrong_password_is_rejected()
    {
        HashCheckReturns(PasswordVerificationResult.Failed);

        Assert.That(await _service.AuthenticateAsync("amartin", "faux"), Is.Null);
    }

    [Test]
    public async Task Unknown_user_still_costs_a_hash_verification()
    {
        // Le temps de réponse ne doit pas révéler qu'un identifiant n'existe pas.
        HashCheckReturns(PasswordVerificationResult.Failed);

        Assert.That(await _service.AuthenticateAsync("inconnu", "secret"), Is.Null);
        _hasher.Verify(h => h.VerifyHashedPassword(It.IsAny<Advisor>(), It.IsAny<string>(), "secret"), Times.Once);
    }

    [Test]
    public async Task Outdated_hash_is_upgraded_after_a_successful_login()
    {
        HashCheckReturns(PasswordVerificationResult.SuccessRehashNeeded);
        _hasher.Setup(h => h.HashPassword(It.IsAny<Advisor>(), "secret")).Returns("nouveau-hash");

        await _service.AuthenticateAsync("amartin", "secret");

        _hasher.Verify(h => h.HashPassword(It.IsAny<Advisor>(), "secret"), Times.Once);
        _database.Context.ChangeTracker.Clear();
        Assert.That(_database.Context.Advisors.Single(a => a.Id == TestDatabase.AliceId).PasswordHash, Is.EqualTo("nouveau-hash"));
    }

    [TestCase(null, "secret")]
    [TestCase("amartin", "")]
    public async Task Missing_credentials_are_rejected_without_calling_the_hasher(string? userName, string? password)
    {
        Assert.That(await _service.AuthenticateAsync(userName, password), Is.Null);
        _hasher.VerifyNoOtherCalls();
    }
}
