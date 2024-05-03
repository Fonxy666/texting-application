const http = require('http');
const httpProxy = require('http-proxy');

const proxy = httpProxy.createProxyServer({});

const TARGET_URL = 'https://localhost:7045';
const TIMEOUT = 10000; // Timeout in milliseconds
const RETRY_ATTEMPTS = 3; // Number of retry attempts

const server = http.createServer((req, res) => {
  let attempts = 0;

  const tryProxy = () => {
    attempts++;

    proxy.web(req, res, { target: TARGET_URL, timeout: TIMEOUT }, (err) => {
      if (err && err.code === 'ECONNRESET' && attempts < RETRY_ATTEMPTS) {
        // Retry if connection was reset
        console.error(`Connection reset. Retrying (${attempts}/${RETRY_ATTEMPTS})...`);
        tryProxy();
      } else {
        console.error(`Proxy error: ${err ? err.message : 'Unknown error'}`);
        res.writeHead(500, { 'Content-Type': 'text/plain' });
        res.end('Proxy error');
      }
    });
  };

  tryProxy();
});

const PORT = 4200; // Port your Angular app is running on
server.listen(PORT, () => {
  console.log(`Proxy server listening on port ${PORT}`);
});