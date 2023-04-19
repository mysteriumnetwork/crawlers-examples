using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HtmlAgilityPack;

namespace Crawler
{
    class Link
    {
        public string uri;
        public int depth;

        public Link(string uri, int depth)
        {
            this.uri = uri;
            this.depth = depth;
        }
    }

    class Crawler
    {
        private IDictionary<string, byte> visited = new ConcurrentDictionary<string, byte>();
        private IDictionary<string, byte> hosts = new ConcurrentDictionary<string, byte>();
        private int maxDepth = 0;
        private int maxSites = 1;

        private WaitCallback callback;
        private CountdownEvent jobsEvent;
        private Crawler _this;

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
                    if (n1 != null) foreach (HtmlNode nn1 in n1)
                        {
                            Console.WriteLine(string.Format("label> {0}", nn1.InnerText));
                        }

                    var n2 = node.SelectNodes(".//span/div[contains(@class, 'uitk-text.uitk-type-600')]");
                    if (n2 != null) foreach (HtmlNode nn in n2)
                        {
                            Console.WriteLine(string.Format("price> {0}", nn.InnerText));
                        }

                    var n3 = node.SelectNodes(".//div[contains(@class, 'uitk-price-lockup')]/section/span[contains(@class, 'uitk-lockup-price')]");
                    if (n3 != null) foreach (HtmlNode nn in n3)
                        {
                            Console.WriteLine(string.Format("price> {0}", nn.InnerText));
                        }
                }
        }

        private ISet<string> collectLinks(string link)
        {
            var newLinks = new HashSet<string>();

            var hw = new HtmlWeb();
            var doc = hw.Load(link);

            _this.scrapData(doc);

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
                    catch (System.UriFormatException) { }
                }
            }
            return newLinks;
        }

        // Crawl a given site using breadth-first search algorithm
        private void TaskHandler(Object ob)
        {
            Link j = (Link)ob;

            Console.WriteLine(string.Format("visit> {1} {0}", j.uri, j.depth));
            var list = this.collectLinks(j.uri);
            foreach (var e in list)
            {
                if (!visited.ContainsKey(e))
                {
                    if (this.maxSitesConstraint(e))
                    {
                        continue;
                    }
                    if (j.depth + 1 <= maxDepth)
                    {
                        var newJob = new Link(e, j.depth + 1);
                        visited[e] = 0;
                        ThreadPool.QueueUserWorkItem(callback, newJob);
                        jobsEvent.AddCount(1);
                    }
                }
            }
            jobsEvent.Signal();
        }

        public void crawl(string u, int maxDepth, int maxSites)
        {
            this.maxDepth = maxDepth;
            this.maxSites = maxSites;

            _this = this;
            callback = new WaitCallback(TaskHandler);
            jobsEvent = new CountdownEvent(1);

            ThreadPool.SetMaxThreads(6, 300);
            ThreadPool.SetMinThreads(6, 300);
            ThreadPool.QueueUserWorkItem(callback, new Link(u, 0));
            jobsEvent.Wait();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var c = new Crawler();
            c.crawl("https://www.expedia.com/Hotel-Search?adults=2&destination=Tbilisi%2C%20Georgia&rooms=1", 0, 1);
        }
    }
}
