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
}
