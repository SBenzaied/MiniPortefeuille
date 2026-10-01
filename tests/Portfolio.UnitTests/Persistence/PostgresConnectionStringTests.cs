using Npgsql;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.UnitTests.Persistence;

[TestFixture]
public class PostgresConnectionStringTests
{
    [Test]
    public void Render_style_url_is_converted_to_an_npgsql_connection_string()
    {
        var normalized = PostgresConnectionString.Normalize("postgresql://portfolio:s3cr%40t@dpg-abc123-a:5433/portfolio_db");

        var builder = new NpgsqlConnectionStringBuilder(normalized);
        Assert.That(builder.Host, Is.EqualTo("dpg-abc123-a"));
        Assert.That(builder.Port, Is.EqualTo(5433));
        Assert.That(builder.Database, Is.EqualTo("portfolio_db"));
        Assert.That(builder.Username, Is.EqualTo("portfolio"));
        Assert.That(builder.Password, Is.EqualTo("s3cr@t"), "les caractères encodés dans l'URL sont décodés");
        Assert.That(builder.SslMode, Is.EqualTo(SslMode.Prefer));
    }

    [Test]
    public void Missing_port_defaults_to_5432_and_sslmode_query_is_honoured()
    {
        var builder = new NpgsqlConnectionStringBuilder(
            PostgresConnectionString.Normalize("postgres://u:p@db.example.com/portfolio?sslmode=require"));

        Assert.That(builder.Port, Is.EqualTo(5432));
        Assert.That(builder.SslMode, Is.EqualTo(SslMode.Require));
    }

    [Test]
    public void Classic_connection_string_is_left_untouched()
    {
        const string classic = "Host=db;Database=portfolio;Username=portfolio;Password=x";

        Assert.That(PostgresConnectionString.Normalize(classic), Is.EqualTo(classic));
    }
}
