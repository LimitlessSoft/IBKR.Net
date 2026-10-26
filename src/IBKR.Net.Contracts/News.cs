namespace IBKR.Net;

/// <summary>A news provider the account can read over the API (reqNewsProviders).</summary>
public sealed record NewsProvider(string Code, string Name);

/// <summary>
/// One headline, from history (reqHistoricalNews) or live (news ticks). <see cref="ExtraData"/> is
/// IB's metadata, e.g. <c>A:800015:L:en</c> (language, sometimes sentiment <c>K:-0.97</c>), without
/// the braces history wraps it in.
/// </summary>
public sealed record NewsHeadline(
	DateTimeOffset Time,
	string ProviderCode,
	string ArticleId,
	string Headline,
	string ExtraData
);

/// <summary>A page of past headlines, newest first; <see cref="HasMore"/> when IB has older ones.</summary>
public sealed record HistoricalNews(IReadOnlyList<NewsHeadline> Headlines, bool HasMore);

/// <summary>How an article's <see cref="NewsArticle.Text"/> is encoded.</summary>
public enum NewsArticleType
{
	/// <summary>Text or HTML.</summary>
	Text = 0,

	/// <summary>A binary document (e.g. PDF), base64-encoded.</summary>
	Binary = 1,
}

/// <summary>A full article (reqNewsArticle).</summary>
public sealed record NewsArticle(NewsArticleType Type, string Text);
