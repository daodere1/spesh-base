// Synthetic, loopback-only Sonarr fixture. Never uses a real API key or media library.
const http = require('node:http');
const fs = require('node:fs');
const port = Number(process.argv[2]);
const logPath = process.argv[3];
const requests = [];
const fixture = [{
  id: 10001, title: 'Isolated episode', airDateUtc: '2026-11-16T02:30:00Z',
  airDate: '2026-11-15', seasonNumber: 1, episodeNumber: 1,
  series: { tvdbId: 999999999, title: 'Isolated series', path: '/not-a-jellyfin-library' },
}];
http.createServer((request, response) => {
  if (request.headers['x-api-key'] !== 'spesh-isolated-sonarr-key') {
    response.writeHead(403); response.end('{}'); return;
  }
  requests.push(request.url);
  fs.writeFileSync(logPath, JSON.stringify(requests));
  response.writeHead(200, { 'Content-Type': 'application/json' });
  response.end(JSON.stringify(fixture));
}).listen(port, '127.0.0.1');
