using Newtonsoft.Json;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace AngryLevelLoader.Managers.ServerManager
{
	public abstract class AngryResponse
	{
		public string message { get; set; }
		public int status { get; set; }
	}

	public abstract class AngryResult<Resp, Stat> where Resp : AngryResponse where Stat : Enum
	{
		public bool networkError = false;
		public bool httpError = false;

		public bool completed = false;
		public bool completedSuccessfully
		{
			get
			{
				return completed && !networkError && !httpError;
			}
		}

		public string message;
		public Stat status;
		public Resp response;
	}

	public static class AngryRequest
	{
		public const string CONTENT_TYPE_JSON = "application/json";

		/// <summary>
		/// Generic method to make a request to the angry api which requires user token as one of the parameters. New token is generated if the token was not requested yet or the API returned an INVALID_TOKEN status code.
		/// </summary>
		/// <typeparam name="Resp">Response JSON type</typeparam>
		/// <typeparam name="Stat">Status code enum</typeparam>
		/// <param name="url">URL to be made web request to</param>
		/// <param name="invalidTokenStatus">Status code for invalid token</param>
		/// <param name="tokenRequested">Set to true by the method if it is called recursively after requesting a new token</param>
		/// <returns>Result object passed as the parameter</returns>
		public static async Task<AngryResult<Resp, Stat>> MakeRequestWithToken<Resp, Stat>(string url, AngryResult<Resp, Stat> result, Stat invalidTokenStatus, CancellationToken cancellationToken = default, string method = "GET", string body = null, string contentType = null, bool tokenRequested = false) where Resp : AngryResponse where Stat : Enum
		{
			bool invalidToken = string.IsNullOrEmpty(AngryUser.token);

			if (!invalidToken || tokenRequested)
			{
				string urlWithToken = (url.EndsWith("?") ? url + $"steamId={AngryUser.steamId}&token={AngryUser.token}" : url + $"&steamId={AngryUser.steamId}&token={AngryUser.token}");
				UnityWebRequest req = new UnityWebRequest(urlWithToken, method);
				req.downloadHandler = new DownloadHandlerBuffer();
				if (body != null)
				{
					req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
				}
				if (contentType != null)
				{
					req.SetRequestHeader("Content-Type", contentType);
					if (req.uploadHandler != null)
						req.uploadHandler.contentType = contentType;
				}
				cancellationToken.Register(() =>
				{
					if (req != null && !req.isDone)
						req.Abort();
				});

				await req.SendWebRequest();

				if (req.isNetworkError)
				{
					result.networkError = true;
					result.completed = true;
					return result;
				}
				if (req.isHttpError)
				{
					result.httpError = true;
					result.completed = true;
					return result;
				}

				Resp response = JsonConvert.DeserializeObject<Resp>(req.downloadHandler.text);
				result.response = response;
				result.message = response.message;
				result.status = (Stat)Enum.ToObject(typeof(Stat), response.status);
				if (EqualityComparer<Stat>.Default.Equals(result.status, invalidTokenStatus))
				{
					invalidToken = true;
				}
				else
				{
					result.completed = true;
					return result;
				}
			}

			if (invalidToken)
			{
				if (tokenRequested)
				{
					result.completed = true;
					return result;
				}

				await AngryUser.GenerateToken();
				return await MakeRequestWithToken<Resp, Stat>(url, result, invalidTokenStatus, cancellationToken, method, body, contentType, true);
			}

			return result;
		}

		/// <summary>
		/// Generic method to make a request to the angry api which requires no token.
		/// </summary>
		/// <typeparam name="Resp">Response JSON type</typeparam>
		/// <typeparam name="Stat">Status code enum</typeparam>
		/// <param name="url">URL to be made web request to</param>
		/// <returns>Result object passed as the parameter</returns>
		public static async Task<AngryResult<Resp, Stat>> MakeRequest<Resp, Stat>(string url, AngryResult<Resp, Stat> result, CancellationToken cancellationToken = default, string method = "GET", string body = null, string contentType = null) where Resp : AngryResponse where Stat : Enum
		{
			UnityWebRequest req = new UnityWebRequest(url, method);
			req.downloadHandler = new DownloadHandlerBuffer();
			if (body != null)
			{
				req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
			}
			if (contentType != null)
			{
				req.SetRequestHeader("Content-Type", contentType);
				if (req.uploadHandler != null)
					req.uploadHandler.contentType = contentType;
			}
			cancellationToken.Register(() =>
			{
				if (req != null && !req.isDone)
					req.Abort();
			});
			await req.SendWebRequest();

			if (req.isNetworkError)
			{
				result.networkError = true;
				result.completed = true;
				return result;
			}
			if (req.isHttpError)
			{
				result.httpError = true;
				result.completed = true;
				return result;
			}

			Resp response = JsonConvert.DeserializeObject<Resp>(req.downloadHandler.text);
			result.response = response;
			result.message = response.message;
			result.status = (Stat)Enum.ToObject(typeof(Stat), response.status);
			result.completed = true;
			return result;
		}

		public static async Task<AngryResult<Resp, Stat>> MakeRequestWithAdminToken<Resp, Stat>(string url, AngryResult<Resp, Stat> result, Stat invalidTokenStatus, Stat missingKeyStatus, CancellationToken cancellationToken = default, string method = "GET", string body = null, string contentType = null, bool tokenRequested = false) where Resp : AngryResponse where Stat : Enum
		{
			if (string.IsNullOrEmpty(CryptographyUtils.AdminPrivateKey))
			{
				result.status = missingKeyStatus;
				result.completed = true;
				return result;
			}

			bool invalidToken = string.IsNullOrEmpty(AngryUser.token);

			if (!invalidToken || tokenRequested)
			{
				string encryptedToken = Convert.ToBase64String(CryptographyUtils.Encrypt(AngryUser.token, CryptographyUtils.AdminPrivateKey));
				string urlWithToken = (url.EndsWith("?") ? url + $"steamId={AngryUser.steamId}&token={encryptedToken}" : url + $"&steamId={AngryUser.steamId}&token={encryptedToken}");
				UnityWebRequest req = new UnityWebRequest(urlWithToken, method);
				req.downloadHandler = new DownloadHandlerBuffer();
				if (body != null)
				{
					req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
				}
				if (contentType != null)
				{
					req.SetRequestHeader("Content-Type", contentType);
					if (req.uploadHandler != null)
						req.uploadHandler.contentType = contentType;
				}
				cancellationToken.Register(() =>
				{
					if (req != null && !req.isDone)
						req.Abort();
				});
				await req.SendWebRequest();

				if (req.isNetworkError)
				{
					result.networkError = true;
					result.completed = true;
					return result;
				}
				if (req.isHttpError)
				{
					result.httpError = true;
					result.completed = true;
					return result;
				}

				Resp response = JsonConvert.DeserializeObject<Resp>(req.downloadHandler.text);
				result.response = response;
				result.message = response.message;
				result.status = (Stat)Enum.ToObject(typeof(Stat), response.status);
				if (EqualityComparer<Stat>.Default.Equals(result.status, invalidTokenStatus))
				{
					invalidToken = true;
				}
				else
				{
					result.completed = true;
					return result;
				}
			}

			if (invalidToken)
			{
				if (tokenRequested)
				{
					result.completed = true;
					return result;
				}

				await AngryUser.GenerateToken();
				return await MakeRequestWithToken<Resp, Stat>(url, result, invalidTokenStatus, cancellationToken, method, body, contentType, true);
			}

			return result;
		}
	}
}
