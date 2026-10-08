using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Devi.Updates;

public static class UpdateClient
{
	public static HttpClient CreateFeedClient(string userAgentProduct, string version)
	{
		if (string.IsNullOrWhiteSpace(userAgentProduct) || userAgentProduct.Contains(' ', StringComparison.Ordinal))
		{
			throw new ArgumentException("The user-agent product must be a single token such as DEVI-Validate.", nameof(userAgentProduct));
		}

		if (string.IsNullOrWhiteSpace(version))
		{
			throw new ArgumentException("The version is required.", nameof(version));
		}

		HttpClient httpClient = new HttpClient(new SocketsHttpHandler
		{
			AllowAutoRedirect = false,
			AutomaticDecompression = DecompressionMethods.All
		});
		httpClient.Timeout = Timeout.InfiniteTimeSpan;
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgentProduct + "/" + version);
		return httpClient;
	}

	public static async Task<UpdateCheckResult> CheckAsync(HttpClient http, Uri feedUrl, string productId, Version current, ECDsa publicKey, CancellationToken cancellationToken, bool requireHttps = true, IReadOnlyCollection<string>? allowedHosts = null)
	{
		EnsureUrl(feedUrl, requireHttps, allowedHosts);
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(20.0));
		string json;
		try
		{
			json = await ReadFeedAsync(http, feedUrl, timeout.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			throw new UpdateException("The update check timed out. This copy still works offline.", transport: true);
		}
		catch (HttpRequestException ex)
		{
			throw new UpdateException("The update check could not reach the download host. This copy still works offline. " + ex.Message, transport: true);
		}

		return UpdateFeed.Compare(UpdateFeed.FindProduct(UpdateFeed.ParseAndVerify(json, publicKey), productId), current);
	}

	public static async Task<UpdateCheckResult> CheckConfiguredAsync(HttpClient http, string productId, Version current, ECDsa publicKey, CancellationToken cancellationToken, bool requireHttps = true, IReadOnlyCollection<string>? allowedHosts = null)
	{
		UpdateException? lastTransport = null;
		foreach (Uri feedUrl in UpdateTrust.FeedUrisFor(productId))
		{
			try
			{
				return await CheckAsync(http, feedUrl, productId, current, publicKey, cancellationToken, requireHttps, allowedHosts).ConfigureAwait(false);
			}
			catch (UpdateException ex) when (ex.IsTransport)
			{
				lastTransport = ex;
			}
		}

		throw lastTransport ?? new UpdateException("The update check could not reach the download host. This copy still works offline.", transport: true);
	}

	public static async Task<string> DownloadVerifiedAsync(HttpClient http, UpdateFile file, string directory, IProgress<long>? progress, CancellationToken cancellationToken, bool requireHttps = true, IReadOnlyCollection<string>? allowedHosts = null)
	{
		if (!Uri.TryCreate(file.Url, UriKind.Absolute, out Uri? result))
		{
			throw new UpdateException("The download address is not a valid URL.");
		}

		EnsureUrl(result, requireHttps, allowedHosts);
		Directory.CreateDirectory(directory);
		string destination = Path.Combine(directory, file.Name);
		string temporary = destination + ".partial";
		if (File.Exists(temporary))
		{
			File.Delete(temporary);
		}

		try
		{
			using HttpResponseMessage response = await http.GetAsync(result, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
			if (response.StatusCode != HttpStatusCode.OK)
			{
				throw new UpdateException("The download returned " + (int)response.StatusCode + ".");
			}

			long? contentLength = response.Content.Headers.ContentLength;
			if (contentLength.HasValue && contentLength.GetValueOrDefault() > UpdateTrust.MaxDownloadBytes)
			{
				throw new UpdateException("The download is larger than this copy will accept.");
			}

			await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			await using FileStream target = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None);
			using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
			byte[] buffer = new byte[131072];
			long total = 0L;
			while (true)
			{
				int n = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
				if (n == 0)
				{
					break;
				}

				total += n;
				if (total > UpdateTrust.MaxDownloadBytes)
				{
					throw new UpdateException("The download is larger than this copy will accept.");
				}

				hash.AppendData(buffer.AsSpan(0, n));
				await target.WriteAsync(buffer.AsMemory(0, n), cancellationToken).ConfigureAwait(false);
				progress?.Report(total);
			}

			await target.FlushAsync(cancellationToken).ConfigureAwait(false);
			if (!string.Equals(Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(), file.Sha256, StringComparison.Ordinal))
			{
				throw new UpdateException("The downloaded file does not match the published SHA-256. It was not kept.");
			}
		}
		catch
		{
			if (File.Exists(temporary))
			{
				File.Delete(temporary);
			}

			throw;
		}

		if (File.Exists(destination))
		{
			File.Delete(destination);
		}

		File.Move(temporary, destination);
		return destination;
	}

	public static void EnsureUrl(Uri uri, bool requireHttps, IReadOnlyCollection<string>? allowedHosts)
	{
		if (!uri.IsAbsoluteUri)
		{
			throw new UpdateException("The update address is not absolute.");
		}

		if (requireHttps)
		{
			if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
			{
				throw new UpdateException("Update addresses must use HTTPS.");
			}
		}
		else if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
		{
			throw new UpdateException("The update address is not an HTTP address.");
		}

		IReadOnlyCollection<string> hosts = allowedHosts ?? UpdateTrust.AllowedHosts;
		bool allowed = false;
		foreach (string host in hosts)
		{
			if (string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase))
			{
				allowed = true;
				break;
			}
		}

		if (!allowed)
		{
			throw new UpdateException("The update address is not on the DEVI download host.");
		}

		if (requireHttps && uri.Port != 443)
		{
			throw new UpdateException("Update addresses must use port 443.");
		}
	}

	private static async Task<string> ReadFeedAsync(HttpClient http, Uri feedUrl, CancellationToken cancellationToken)
	{
		using HttpResponseMessage response = await http.GetAsync(feedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
		if (response.StatusCode != HttpStatusCode.OK)
		{
			throw new UpdateException("The update check returned " + (int)response.StatusCode + ". This copy still works offline.", transport: true);
		}

		long? contentLength = response.Content.Headers.ContentLength;
		if (contentLength.HasValue && contentLength.GetValueOrDefault() > UpdateTrust.MaxFeedBytes)
		{
			throw new UpdateException("The update feed is too large. This copy still works offline.", transport: true);
		}

		await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
		using MemoryStream buffer = new MemoryStream();
		byte[] chunk = new byte[8192];
		while (true)
		{
			int n = await source.ReadAsync(chunk, cancellationToken).ConfigureAwait(false);
			if (n == 0)
			{
				break;
			}

			if (buffer.Length + n > UpdateTrust.MaxFeedBytes)
			{
				throw new UpdateException("The update feed is too large. This copy still works offline.", transport: true);
			}

			buffer.Write(chunk, 0, n);
		}

		return Encoding.UTF8.GetString(buffer.ToArray());
	}
}
