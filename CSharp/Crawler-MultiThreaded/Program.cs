using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HtmlAgilityPack;

namespace Crawler
{
    class Link
    {
        public string uri;
        public int    depth;

        public Link (string uri, int depth)
        {
            this.uri = uri;
            this.depth = depth;
        }

        public override bool Equals(object ob)
        {
            if (ob is Link)
            {
                Link c = (Link)ob;
                return depth == c.depth && uri == c.uri;
            }
            return false;            
        }

        public override int GetHashCode() {
            return uri.GetHashCode() ^ depth.GetHashCode();
        }
    }

    class Crawler
    {
        private Dictionary<string, Link> visited = new Dictionary<string, Link>();
        private Queue<Link> jobs = new Queue<Link>();
        private ISet<string> hosts = new HashSet<string>();
        const int maxDepth = 2;
        const int maxSites = 10;

        private WaitCallback callback;
        private CountdownEvent jobsEvent;

        // maxSitesConstraint returns true if we have to skip the given link
        private bool maxSitesConstraint(string e)
        {
            var uri = new Uri(e);           
            if (!hosts.Contains(uri.Host))
            {
                if (hosts.Count() < maxSites)
                {
                    hosts.Add(uri.Host);
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public ISet<string> collectLinks(string link)
        {
            var newLinks = new HashSet<string>();

            var hw = new HtmlWeb();
            var doc = hw.Load(link);
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
                    catch (System.UriFormatException) {}
                }
            }
            return newLinks;
        }
        
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
                        visited[e] = newJob;
                        ThreadPool.QueueUserWorkItem(callback, newJob);
                        jobsEvent.AddCount(1);
                    }
                }
            }
            jobsEvent.Signal();
        }

        public void crawl(string u)
        {
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
            c.crawl("http://google.com");
        }
    }
}
