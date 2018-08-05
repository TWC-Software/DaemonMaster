/////////////////////////////////////////////////////////////////////////////////////////
//  Parts of the code are from:     Copyright (c) 2012 GitHub, Inc. (Squirrel)
//  and running under the MIT licences! 
//
//  Permission is hereby granted, free of charge, to any person obtaining a
//  copy of this software and associated documentation files (the "Software"),
//  to deal in the Software without restriction, including without limitation
//  the rights to use, copy, modify, merge, publish, distribute, sublicense,
//  and/or sell copies of the Software, and to permit persons to whom the
//  Software is furnished to do so, subject to the following conditions:
/////////////////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DaemonMasterUpdater.GitHub
{
    public sealed class GitHubApi
    {
        public class GitHubRelease
        {
            [JsonProperty(PropertyName = "prerelease")]
            public bool Prerelease;

            [JsonProperty(PropertyName = "published_at")]
            public DateTime PublishedAt;

            [JsonProperty(PropertyName = "name")]
            public string VersionName;

            [JsonProperty(PropertyName = "tag_name")]
            public Version Version;

            [JsonProperty(PropertyName = "html_url")]
            public string VersionUrl;

            [JsonProperty(PropertyName = "assets")]
            public List<GitHubAssets> Assets;
        }

        public class GitHubAssets
        {
            [JsonProperty(PropertyName = "browser_download_url")]
            public string FileUrl;

            [JsonProperty(PropertyName = "size")]
            public string FileSize;
        }


        public static async Task<GitHubRelease> GitHubGetLastReleaseAsync(string repoUrl, string accessToken = null, bool acceptPrerelease = false)
        {
            var repoUri = new Uri(repoUrl);
            var userAgent = new ProductInfoHeaderValue(Updater.AppName + "_Updater", Assembly.GetExecutingAssembly().GetName().Version.ToString());

            if (repoUri.Segments.Length != 3)
            {
                throw new UriFormatException("Repo URL must be to the root URL of the repo e.g. https://github.com/myuser/myrepo");
            }

            //Building the GitHub API URL
            var repoApiUrl = new StringBuilder("repos");
            repoApiUrl.Append(repoUri.AbsolutePath);
            repoApiUrl.Append("/releases");

            //Adding access token when present
            if (!String.IsNullOrWhiteSpace(accessToken))
                repoApiUrl.Append("?access_token=").Append(accessToken);

            using (var httpClient = new HttpClient())
            {
                //Disable SSLv3
                ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Ssl3;

                httpClient.BaseAddress = new Uri("https://api.github.com/");
                httpClient.DefaultRequestHeaders.UserAgent.Clear();
                httpClient.DefaultRequestHeaders.UserAgent.Add(userAgent);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var result = await httpClient.GetAsync(repoApiUrl.ToString());

                //Make an exception if the result is not valid
                result.EnsureSuccessStatusCode();

                var releases = JsonConvert.DeserializeObject<List<GitHubRelease>>(await result.Content.ReadAsStringAsync());
                var lastReleases = releases
                    .Where(x => acceptPrerelease || !x.Prerelease) //Exclude or include prerelease
                    .OrderByDescending(x => x.PublishedAt) //Order by publishing date
                    .First(); //Get only the first one back

                return lastReleases;
            }
        }
    }
}
