const request = require('request');
const cheerio = require('cheerio');
const fs = require('fs');
const axios = require('axios');
const url = require('url');

/* private */
var visited = {}; // set
var jobs = [];    // queue
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

// Crawl a given site using breadth-first search algorithm
async function crawl(root) {
    jobs.push({ url: root, depth: 0 })

    while (jobs.length > 0) {
        var j = jobs.pop()

        console.log("visit> %s %s", j.depth, j.url)
        var list = await collectUrls(j.url)
        for (let e in list) {
            if (!(e in visited)) {
                if (maxSitesConstraint(e)) {
                    continue
                }
                if (j.depth + 1 <= maxDepth) {
                    let newJob = { url: e, depth: j.depth + 1 }
                    visited[e] = newJob
                    jobs.push(newJob)
                }
            }
        }
    }
}

async function main() {
    await crawl('https://google.com')
}

main()
    .then(v => console.log(v))
    .catch(err => console.error(err));
