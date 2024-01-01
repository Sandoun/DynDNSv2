using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DynDNSv2;

internal class Updater : BackgroundService
{

    private string dynDnsUrl;

    private string username;

    private string password;

    private string[] updateDomains;

    private IPAddress lastPublicIp;

    private CancellationToken cToken;

    private ILogger logger;

    public Updater(ILogger<Updater> logger)
    {

        this.logger = logger;
        lastPublicIp = IPAddress.Parse("127.0.0.1");
    
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        cToken = stoppingToken;

        var _dynDNSUrl = Environment.GetEnvironmentVariable("UPDATE_URL");
        var _username = Environment.GetEnvironmentVariable("USERNAME");
        var _password = Environment.GetEnvironmentVariable("PASSWORD");

        if (string.IsNullOrEmpty(_dynDNSUrl))
            throw new Exception("The UPDATE_URL env variable was not set");

        if (string.IsNullOrEmpty(_username))
            throw new Exception("The USERNAME env variable was not set");

        if (string.IsNullOrEmpty(_password))
            throw new Exception("The PASSWORD env variable was not set");

        dynDnsUrl = _dynDNSUrl;
        username = _username;
        password = _password;

        var updateLst = new List<string>();

        int i = 1;
        while(true)
        {
            var found = Environment.GetEnvironmentVariable($"UPDATE_URL_{i}");

            if(!string.IsNullOrEmpty(found))
            {
                updateLst.Add(found);
            } else
            {
                break;
            }

            i++;

        }

        if(updateLst.Count <= 0)
            throw new Exception("No UPDATE_URL_[N] env variable found, pleace specify one with UPDATE_URL_1, UPDATE_URL_2 ...");

        updateDomains = updateLst.ToArray();

        await RunUpdaterAsync();

    }

    private async Task RunUpdaterAsync()
    {

        await RunIpTester();

    }

    private async Task RunIpTester()
    {

        while(!cToken.IsCancellationRequested) {

            var ipChangeResult = await CheckPublicIpAsync();

            if (ipChangeResult.WasChanged)
            {
                logger.LogInformation("Public IP change detected");
                await RunDnsUpdateAsync(ipChangeResult.UpdatedIP!);
            }

            await Task.Delay(5000);

        }

    }

    private async Task<IPTestResult> CheckPublicIpAsync()
    {

        List<string> services = new List<string>() {
            "https://ipv4.icanhazip.com",
            "https://api.ipify.org",
            "https://ipinfo.io/ip",
            "https://checkip.amazonaws.com",
            "https://wtfismyip.com/text",
            "http://icanhazip.com"
        };

        using (var client = new HttpClient())
        {

            foreach (var service in services)
            {

                try
                {

                    var str = await client.GetStringAsync(service);
                    var match = Regex.Match(str, @"(?:[0-9]{1,3}\.){3}[0-9]{1,3}");

                    if (match.Success)
                    {
                        str = match.Value;
                    }

                    if (!IPAddress.TryParse(str, out var ip)) continue;

                    if (ip.ToString() != lastPublicIp.ToString())
                    {

                        lastPublicIp = ip;
                        return new IPTestResult
                        {
                            WasChanged = true,
                            UpdatedIP = ip,
                        };

                    }

                    return new IPTestResult
                    {
                        WasChanged = false,
                    };

                }
                catch { }

            }

        }

        throw new InvalidOperationException("The public ip could not be resolved");

    }

    private async Task RunDnsUpdateAsync(IPAddress publicIp)
    {

        using (var client = new HttpClient())
        {

            client.DefaultRequestHeaders.Add("User-Agent", "Server");

            var byteArray = new UTF8Encoding().GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var hostNames = string.Join(",", updateDomains);

            var reqURL = $"{dynDnsUrl}?hostname={hostNames}&myip={publicIp}";

            var result = await client.GetAsync(reqURL, cToken);

            if (result.IsSuccessStatusCode)
            {

                string? responseStr = result.Content != null ? await result.Content.ReadAsStringAsync() : null;

                if (responseStr == null)
                {

                    logger.LogError("No response body");
                    return;

                }

                responseStr = responseStr.Substring(0, responseStr.Length - 1);

                var updateMsgs = responseStr.Split("\n");

                foreach (var msg in updateMsgs)
                {

                    var sb = new StringBuilder("DynDNS response from ");

                    sb.Append($"[{reqURL}]: ");
                    sb.Append($"'{msg}', ");
                    sb.Append($"Code: {(int)result.StatusCode}");

                    if (msg.Contains("good") || msg.Contains("nochg"))
                    {

                        logger.LogInformation($"Updated DNS: {msg}");

                    }
                    else
                    {

                        logger.LogError($"DynDNS error response: {responseStr}");

                    }

                }

            }
            else
            {

                logger.LogError($"HTTP Error code: {result.StatusCode}");

            }

        }

    }

}
