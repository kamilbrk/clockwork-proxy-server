const express = require('express');
const https = require('https');
const bodyParser = require('body-parser');
const queryString = require('querystring');
const keys = require('./keys.js');
const app = express();

var mappedKeys = {};

app.use(bodyParser.json());

app.post('/register', function (req, res) {
  if (!req.body.mobile || !req.body.public_key)
    res.sendStatus(400);

  mappedKeys[req.body.mobile] = req.body.public_key;

  res.sendStatus(200);
});

app.post('/message', function (req, res) {
  if (!req.body.message)
    res.sendStatus(400);

  var sendTextOptions = {
    host: 'api.clockworksms.com',
    path: '/http/send.aspx?'
  };

  sendTextOptions.path += queryString.stringify({
    key: encodeURIComponent(keys.clockworkApiKey),
    to: encodeURI(''), // sorry chris
    content: req.body.message.replace('/\s/','+')
  });

  https.get(sendTextOptions, (sendRes) => {
    res.sendStatus(sendRes.statusCode);
  }).on('error', (e) => {
    console.error(e);
    res.sendStatus(500);
  });
});

app.listen(3000, function () {
  console.log('Example app listening on port 3000!');
})

