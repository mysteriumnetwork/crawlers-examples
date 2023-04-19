const request = require('request');
const cheerio = require('cheerio');
const axios = require('axios');
const url = require('url');
const Queue = require('async-parallel-queue');


/* private */
var visited = {}; // set
var jobs;         // queue
var hosts = {};   // set


var numberOfKeys = function (o) { return Object.keys(o).length }

// maxSitesConstraint returns true if we have to skip the given link
function maxSitesConstraint(e) {
    var uri = url.parse(e);
    if (!(uri.host in hosts)) {
        if (numberOfKeys(hosts) < maxSites) {
            hosts[uri.host] = 1
        } else {
            return true
        }
    }
    return false
}

function scrapData($) {
    const cards = $("div.uitk-card.uitk-card-roundcorner-all")
    cards.each((index, value) => {
        const _l1 = $(value).find("div.uitk-card-content-section > div > div > h4.uitk-heading")
        _l1.each((index, value) => {
            console.log("label>", $(value).text())
        })
        const _l2 = $(value).find("span > div.uitk-text.uitk-type-600")
        _l2.each((index, value) => {
            console.log("price>", $(value).text())
        })
        const _l3 = $(value).find("div.uitk-price-lockup > section > span.uitk-lockup-price")
        _l3.each((index, value) => {
            console.log("price>", $(value).text())
        })
    })
}

async function collectUrls(uri) {
    let newLinks = {}

    let response = await axios(uri).catch((err) => console.log(err))
    if (response.status !== 200) {
        console.log("Error occurred while fetching data")
        return newLinks
    }
    const $ = cheerio.load(response.data)
    console.log("load ok>")

    scrapData($)

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

const maxDepth = 0;
const maxSites = 1;

async function main() {
    await crawl('https://www.expedia.com/Hotel-Search?adults=2&destination=Tbilisi%2C%20Georgia&rooms=1')
}

main()
    .then(v => console.log(v))
    .catch(err => console.error(err));