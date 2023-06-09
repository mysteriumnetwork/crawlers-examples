﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Crawler
{
    class Crawler
    {
        private IDictionary<string, byte> visited = new ConcurrentDictionary<string, byte>();
        private IDictionary<string, byte> hosts = new ConcurrentDictionary<string, byte>();
        private ConcurrentQueue<Link> queue;
        private HttpClient client;

        // settings
        private int maxDepth = 0;
        private int maxSites = 1;

        // maxSitesConstraint returns true if we have to skip the given link
        private bool maxSitesConstraint(string e)
        {
            var uri = new Uri(e);
            if (!hosts.ContainsKey(uri.Host))
            {
                if (hosts.Count() < maxSites)
                {
                    hosts[uri.Host] = 0;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        private void scrapData(HtmlDocument doc)
        {
            var cards = doc.DocumentNode.SelectNodes("//div[contains(@class, 'uitk-card uitk-card-roundcorner-all')]");
            if (cards != null) foreach (HtmlNode node in cards)
                {
                    var n1 = node.SelectNodes(".//div[contains(@class, 'uitk-card-content-section')]/div/div/h4[contains(@class, 'uitk-heading')]");
                    if (n1 != null) foreach (var nn1 in n1)
                        {
                            Console.WriteLine(string.Format("label> {0}", nn1.InnerText));
                        }

                    var n2 = node.SelectNodes(".//span/div[contains(@class, 'uitk-text.uitk-type-600')]");
                    if (n2 != null) foreach (var nn in n2)
                        {
                            Console.WriteLine(string.Format("price> {0}", nn.InnerText));
                        }

                    var n3 = node.SelectNodes(".//div[contains(@class, 'uitk-price-lockup')]/section/span[contains(@class, 'uitk-lockup-price')]");
                    if (n3 != null) foreach (var nn in n3)
                        {
                            Console.WriteLine(string.Format("price> {0}", nn.InnerText));
                        }
                }
        }

        private async Task<ISet<string>> collectLinks(string link)
        {
            var newLinks = new HashSet<string>();

            var s = await client.GetStringAsync(link);
            var doc = new HtmlDocument();
            doc.LoadHtml(s);

            scrapData(doc);

            var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (nodes != null)
            {
                foreach (HtmlNode node in nodes)
                {
                    var v = node.GetAttributeValue("href", "");
                    try
                    {
                        var u = new Uri(v);
                        newLinks.Add(v);
                    }
                    catch {}
                }
            }
            return newLinks;
        }

        // Crawl a given site using breadth-first search algorithm
        private async Task TaskHandler(Link j)
        {           
            Console.WriteLine(string.Format("visit> {1} {0}", j.uri, j.depth));
            
            var list = await collectLinks(j.uri);
            foreach (var e in list)
            {
                if (!visited.ContainsKey(e))
                {
                    if (maxSitesConstraint(e))
                    {
                        continue;
                    }
                    if (j.depth + 1 <= maxDepth)
                    {
                        var newJob = new Link(e, j.depth + 1);
                        visited[e] = 0;

                        queue.Enqueue(newJob);
                    }
                }
            }
        }

        // _maxDepth - maximum depth of walk tree
        // _maxSites - maximum number of sites to crawl, including an initail address
        public async Task Start(string u, int _maxDepth, int _maxSites)
        {
            maxDepth = _maxDepth;
            maxSites = _maxSites;

            var httpClientHandler = new HttpClientHandler
            {
                Proxy = new WebProxy
                {
                    Address = new Uri("http://localhost:8080"),
                    BypassProxyOnLocal = false,
                    UseDefaultCredentials = false,
                }
            };
            client = new HttpClient(handler: httpClientHandler, disposeHandler: true);
           
            var maxThreads = 8;
            queue = new ConcurrentQueue<Link>();
            queue.Enqueue(new Link(u, 0));

            var tasks = new List<Task>();
            for (int n = 0; n < maxThreads; n++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (queue.TryDequeue(out Link l))
                    {
                        await RetryHelper.RetryOnExceptionAsync(5, TimeSpan.FromSeconds(5), async () => {
                            await TaskHandler(l);
                        });
                    }
                }));
            }
            await Task.WhenAll(tasks);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            var c = new Crawler();

            try
            {
                var uri = "https://www.expedia.com/Hotel-Search?adults=2&destination=Tbilisi%2C%20Georgia&rooms=1";
                var maxDepth = 0;
                var maxSites = 1;
                // 0 - is a maximum depth of walk tree
                // 1 - maximum number of sites to crawl, including an initail address

                c.Start(uri, maxDepth, maxSites)
                    .Wait();

            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
