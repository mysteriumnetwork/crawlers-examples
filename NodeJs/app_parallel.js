const request = require('request');
const cheerio = require('cheerio');
const axios = require('axios');
const url = require('url');
const Queue = require('async-parallel-queue');


/* private */
var visited = {}; // set
var jobs;         // queue
var hosts = {};   // set
const maxDepth = 1;
const maxSites = 5;

var numberOfKeys = function (o) { return Object.keys(o).length }

// maxSitesConstraint returns true if we have to skip the given link
function maxSitesConstraint(e) {
    var uri = url.parse(e);
    if (!(uri.host in hosts)) {
        if (numberOfKeys(hosts) < maxSites) {
            hosts[uri.Host] = 1
        } else {
            return true
        }
    }
    return false
}

async function collectUrls(uri) {
    let newLinks = {}

    let response = await axios(uri).catch((err) => console.log(err))
    if (response.status !== 200) {
        console.log("Error occurred while fetching data")
        return newLinks
    }
    const $ = cheerio.load(response.data)
    const links = $("a")
    links.each((index, value) => {
        const l = $(value).attr("href")
        if (l === undefined) {
            return
        }

        let l_ = url.parse(l)
        if (l_.host != null) {
            newLinks[l] = 1
        }
    })
    return newLinks
}

async function taskHandler(job) {
    console.log("visit> %s %s", job.depth, job.url)

    var list = await collectUrls(job.url)
    for (let e in list) {
        if (!(e in visited)) {
            if (maxSitesConstraint(e)) {
                continue
            }
            if (job.depth + 1 <= maxDepth) {
                let newJob = { url: e, depth: job.depth + 1 }
                visited[e] = newJob
                jobs.add(async () => taskHandler(newJob));
            }
        }
    }
}

// Crawl a given site using breadth-first search algorithm
async function crawl(root) {
    jobs = new Queue({ concurrency: 10 });
    jobs.add(async () => taskHandler({ url: root, depth: 0 }));
    await jobs.waitIdle();
}

async function main() {
    await crawl('https://google.com')
}

main()
    .then(v => console.log(v))
    .catch(err => console.error(err));
