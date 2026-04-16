const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] :
  env.ASPNETCORE_HTTP_PORT ? `http://localhost:${env.ASPNETCORE_HTTP_PORT}` :
  'http://localhost:5039';

const PROXY_CONFIG = [
  {
    context: [
      "/countries",
      "/weatherforecast",
    ],
    target,
    secure: false,
    changeOrigin: true,
  }
]

module.exports = PROXY_CONFIG;
